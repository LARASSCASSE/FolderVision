using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FolderVision.Models;

namespace FolderVision.Core
{
    public class ThreadManager : IDisposable
    {
        private readonly int _maxConcurrency;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<Task> _taskQueue;
        private readonly ConcurrentDictionary<int, ThreadInfo> _threadInfos;
        private readonly object _lockObject = new object();
        private CancellationTokenSource? _cancellationTokenSource;
        private int _activeTasks;
        private bool _disposed;

        public ThreadManager(int maxConcurrency = 0)
        {
            _maxConcurrency = Math.Max(1, maxConcurrency == 0 ? Environment.ProcessorCount : maxConcurrency);
            _semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
            _taskQueue = new ConcurrentQueue<Task>();
            _threadInfos = new ConcurrentDictionary<int, ThreadInfo>();
        }

        public async Task<ScanResult> ScanMultiplePathsAsync(List<string> paths, ScanSettings settings, ProgressTracker progressTracker)
        {
            if (paths == null || !paths.Any())
                throw new ArgumentException("Paths list cannot be null or empty", nameof(paths));

            // Validate paths
            var validPaths = paths.Where(Directory.Exists).ToList();
            if (!validPaths.Any())
                throw new ArgumentException("No valid paths found", nameof(paths));

            _cancellationTokenSource = new CancellationTokenSource();
            var aggregatedResult = new ScanResult
            {
                ScanStartTime = DateTime.Now
            };

            progressTracker.Initialize(validPaths.Count);

            try
            {
                // Create tasks for each path
                var scanTasks = new List<Task<ThreadScanResult>>();

                for (int i = 0; i < validPaths.Count; i++)
                {
                    var path = validPaths[i];
                    var threadId = i;

                    var threadInfo = new ThreadInfo
                    {
                        ThreadId = threadId,
                        Path = path,
                        Status = ThreadStatus.Queued,
                        StartTime = DateTime.Now
                    };

                    _threadInfos.TryAdd(threadId, threadInfo);

                    var task = ScanPathAsync(path, threadId, settings, progressTracker, _cancellationTokenSource.Token);
                    scanTasks.Add(task);
                }

                // Execute all tasks with limited concurrency
                var results = await ExecuteWithLimitedConcurrencyAsync(scanTasks, _maxConcurrency);

                // Aggregate results
                foreach (var result in results.Where(r => r?.ScanResult != null))
                {
                    if (result.ScanResult?.RootFolders.Any() == true)
                    {
                        foreach (var rootFolder in result.ScanResult.RootFolders)
                        {
                            aggregatedResult.AddRootFolder(rootFolder);
                        }
                    }

                    if (result.ScanResult?.ScannedPaths != null)
                    {
                        foreach (var scannedPath in result.ScanResult.ScannedPaths)
                        {
                            aggregatedResult.AddScannedPath(scannedPath);
                        }
                    }
                }

                aggregatedResult.SetScanDuration(DateTime.Now);
                return aggregatedResult;
            }
            catch (OperationCanceledException)
            {
                aggregatedResult.SetScanDuration(DateTime.Now);
                return aggregatedResult;
            }
            catch (Exception ex)
            {
                aggregatedResult.SetScanDuration(DateTime.Now);
                throw new InvalidOperationException($"Multi-path scan failed: {ex.Message}", ex);
            }
        }

        private async Task<ThreadScanResult> ScanPathAsync(string path, int threadId, ScanSettings settings, ProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var threadInfo = _threadInfos[threadId];
            var result = new ThreadScanResult { ThreadId = threadId };

            try
            {
                threadInfo.Status = ThreadStatus.Running;
                threadInfo.StartTime = DateTime.Now;

                var scanEngine = new ScanEngine();

                // Subscribe to progress events
                scanEngine.ProgressChanged += (sender, e) =>
                {
                    threadInfo.CurrentPath = e.CurrentPath;
                    threadInfo.ProgressPercentage = e.PercentComplete;
                    threadInfo.ProcessedItems = e.ProcessedFiles;
                    threadInfo.TotalItems = e.TotalFiles;

                    progressTracker.UpdateThreadProgress(threadId, e.PercentComplete, e.CurrentPath);
                };

                var scanResult = await scanEngine.ScanFolderAsync(path, settings);

                result.ScanResult = scanResult;
                result.IsSuccess = true;
                result.Errors = scanEngine.GetErrors().ToList();

                threadInfo.Status = ThreadStatus.Completed;
                threadInfo.EndTime = DateTime.Now;
                threadInfo.ProgressPercentage = 100;

                progressTracker.CompleteThread(threadId);

                return result;
            }
            catch (OperationCanceledException)
            {
                threadInfo.Status = ThreadStatus.Cancelled;
                threadInfo.EndTime = DateTime.Now;
                result.IsSuccess = false;
                result.Error = "Operation was cancelled";
                return result;
            }
            catch (Exception ex)
            {
                threadInfo.Status = ThreadStatus.Failed;
                threadInfo.EndTime = DateTime.Now;
                threadInfo.Error = ex.Message;

                result.IsSuccess = false;
                result.Error = ex.Message;
                result.Exception = ex;

                progressTracker.FailThread(threadId, ex.Message);

                return result;
            }
        }

        private async Task<List<T>> ExecuteWithLimitedConcurrencyAsync<T>(List<Task<T>> tasks, int maxConcurrency)
        {
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var results = new List<T>();
            var executingTasks = new List<Task<T>>();

            foreach (var task in tasks)
            {
                var wrappedTask = ExecuteTaskWithSemaphoreAsync(task, semaphore);
                executingTasks.Add(wrappedTask);
            }

            var completedTasks = await Task.WhenAll(executingTasks);
            results.AddRange(completedTasks);

            return results;
        }

        private async Task<T> ExecuteTaskWithSemaphoreAsync<T>(Task<T> task, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                Interlocked.Increment(ref _activeTasks);
                return await task;
            }
            finally
            {
                Interlocked.Decrement(ref _activeTasks);
                semaphore.Release();
            }
        }

        public async Task ExecuteAsync(Func<Task> taskFunc)
        {
            await _semaphore.WaitAsync();
            try
            {
                Interlocked.Increment(ref _activeTasks);
                await taskFunc();
            }
            finally
            {
                Interlocked.Decrement(ref _activeTasks);
                _semaphore.Release();
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> taskFunc)
        {
            await _semaphore.WaitAsync();
            try
            {
                Interlocked.Increment(ref _activeTasks);
                return await taskFunc();
            }
            finally
            {
                Interlocked.Decrement(ref _activeTasks);
                _semaphore.Release();
            }
        }

        public void CancelAll()
        {
            _cancellationTokenSource?.Cancel();
        }

        public IReadOnlyDictionary<int, ThreadInfo> GetThreadInfos()
        {
            return _threadInfos.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public ThreadStatus GetThreadStatus(int threadId)
        {
            return _threadInfos.TryGetValue(threadId, out var info) ? info.Status : ThreadStatus.Unknown;
        }

        public int ActiveTasks => _activeTasks;
        public int QueuedTasks => _taskQueue.Count;
        public int MaxConcurrency => _maxConcurrency;

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _semaphore?.Dispose();
                _disposed = true;
            }
        }
    }

    public class ThreadInfo
    {
        public int ThreadId { get; set; }
        public string Path { get; set; } = string.Empty;
        public ThreadStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string CurrentPath { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public long ProcessedItems { get; set; }
        public long TotalItems { get; set; }
        public string? Error { get; set; }

        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
        public TimeSpan ElapsedTime => (EndTime ?? DateTime.Now) - StartTime;
    }

    public class ThreadScanResult
    {
        public int ThreadId { get; set; }
        public ScanResult? ScanResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Error { get; set; }
        public Exception? Exception { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public enum ThreadStatus
    {
        Unknown,
        Queued,
        Running,
        Completed,
        Failed,
        Cancelled
    }
}