using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using FolderVision.Models;
using FolderVision.Utils;

namespace FolderVision.Core
{
    public class ScanEngine
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly object _lockObject = new object();
        private long _processedDirectories;
        private long _estimatedDirectories;
        private readonly List<string> _errors;
        private MemoryMonitor? _memoryMonitor;
        private bool _useProgressiveEstimation;

        public ScanEngine()
        {
            _errors = new List<string>();
            _useProgressiveEstimation = true; // Enable progressive estimation by default
        }

        public async Task<ScanResult> ScanFolderAsync(string folderPath, ScanSettings settings)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));

            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

            var scanResult = new ScanResult
            {
                ScanStartTime = DateTime.Now
            };

            _cancellationTokenSource = new CancellationTokenSource(settings.GlobalTimeout);
            _processedDirectories = 0;
            _estimatedDirectories = 1; // Start with 1 for the root directory
            lock (_lockObject)
            {
                _errors.Clear();
            }

            // Initialize memory monitor
            _memoryMonitor = new MemoryMonitor(settings.MaxMemoryUsageMB, settings.EnableMemoryOptimization);

            try
            {
                // Single pass: scan with progressive estimation
                var rootFolder = await ScanDirectoryAsync(folderPath, settings, _cancellationTokenSource.Token);

                if (rootFolder != null)
                {
                    scanResult.AddRootFolder(rootFolder);
                    scanResult.AddScannedPath(folderPath);
                }

                // Finalize progress reporting
                FinalizeProgress();

                scanResult.SetScanDuration(DateTime.Now);
                ScanCompleted?.Invoke(this, scanResult);
                return scanResult;
            }
            catch (OperationCanceledException)
            {
                scanResult.SetScanDuration(DateTime.Now);
                return scanResult;
            }
            catch (Exception ex)
            {
                _errors.Add($"Scan failed: {ex.Message}");
                scanResult.SetScanDuration(DateTime.Now);
                return scanResult;
            }
        }

        public async Task<FolderInfo?> ScanDirectoryAsync(string path, ScanSettings settings, CancellationToken cancellationToken = default, int currentDepth = 0)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            // Check depth limit to prevent stack overflow
            if (currentDepth >= settings.MaxDepth)
            {
                LogError($"Maximum depth reached ({settings.MaxDepth}) at: {path}");
                return null;
            }

            // Get timeout based on path type (network vs local)
            var timeout = TimeoutHelper.GetTimeoutForPath(path, settings);

            try
            {
                return await TimeoutHelper.ExecuteWithTimeout(async (timeoutToken) =>
                {
                    // Check if we should skip this folder
                    if (settings.ShouldSkipFolder(path))
                        return null;

                    var folderInfo = new FolderInfo(path);

                    // Get directory info
                    var directoryInfo = new DirectoryInfo(path);
                    folderInfo.LastModified = directoryInfo.LastWriteTime;

                    // Get subdirectories for progressive estimation
                    var subdirectories = await GetSubdirectoriesAsync(directoryInfo, timeoutToken);

                    // Report progress with discovered subdirectory count
                    ReportProgress(path, subdirectories.Length);

                    // Scan files in current directory
                    await ScanFilesAsync(folderInfo, directoryInfo, settings, timeoutToken);

                    // Scan subdirectories using already-retrieved list
                    await ScanSubdirectoriesAsync(folderInfo, subdirectories, settings, timeoutToken, currentDepth);

                    folderInfo.UpdateCounts();
                    return folderInfo;
                }, timeout, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                LogError($"Access denied: {path}");
                return null;
            }
            catch (PathTooLongException)
            {
                LogError($"Path too long: {path}");
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                LogError($"Directory not found: {path}");
                return null;
            }
            catch (IOException ex)
            {
                LogError($"IO error in {path}: {ex.Message}");
                return null;
            }
            catch (SecurityException)
            {
                LogError($"Security error accessing: {path}");
                return null;
            }
            catch (TimeoutException)
            {
                LogError($"Timeout ({timeout.TotalSeconds}s) in {path}");
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Unexpected error in {path}: {ex.Message}");
                return null;
            }
        }

        private async Task ScanFilesAsync(FolderInfo folderInfo, DirectoryInfo directoryInfo, ScanSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var files = await Task.Run(() =>
                {
                    try
                    {
                        return directoryInfo.GetFiles();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        LogError($"Access denied to files in: {directoryInfo.FullName}");
                        return Array.Empty<FileInfo>();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error getting files in {directoryInfo.FullName}: {ex.Message}");
                        return Array.Empty<FileInfo>();
                    }
                }, cancellationToken);

                var fileCount = 0;
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        // Skip hidden files if settings specify
                        if (settings.SkipHiddenFolders && (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        fileCount++;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error processing file {file.FullName}: {ex.Message}");
                    }
                }

                folderInfo.SetFileCount(fileCount);
            }
            catch (Exception ex)
            {
                LogError($"Error scanning files in {directoryInfo.FullName}: {ex.Message}");
                folderInfo.SetFileCount(0);
            }
        }

        private async Task<DirectoryInfo[]> GetSubdirectoriesAsync(DirectoryInfo directoryInfo, CancellationToken cancellationToken)
        {
            try
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        return directoryInfo.GetDirectories();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        LogError($"Access denied to subdirectories in: {directoryInfo.FullName}");
                        return Array.Empty<DirectoryInfo>();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error getting subdirectories in {directoryInfo.FullName}: {ex.Message}");
                        return Array.Empty<DirectoryInfo>();
                    }
                }, cancellationToken);
            }
            catch (Exception)
            {
                return Array.Empty<DirectoryInfo>();
            }
        }

        private async Task ScanSubdirectoriesAsync(FolderInfo folderInfo, DirectoryInfo[] subdirectories, ScanSettings settings, CancellationToken cancellationToken, int currentDepth)
        {
            try
            {

                // Optimize for large directory structures
                if (subdirectories.Length > 100)
                {
                    // Process in batches for very large directories
                    await ProcessSubdirectoriesInBatches(folderInfo, subdirectories, settings, cancellationToken, currentDepth);
                }
                else
                {
                    // Standard parallel processing for smaller directories
                    var semaphore = new SemaphoreSlim(settings.MaxThreads, settings.MaxThreads);
                    var tasks = new List<Task>();

                    foreach (var subdir in subdirectories)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        tasks.Add(ProcessSubdirectoryAsync(folderInfo, subdir, settings, semaphore, cancellationToken, currentDepth));
                    }

                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error scanning subdirectories: {ex.Message}");
            }
        }

        private async Task ProcessSubdirectoriesInBatches(FolderInfo folderInfo, DirectoryInfo[] subdirectories, ScanSettings settings, CancellationToken cancellationToken, int currentDepth)
        {
            const int batchSize = 50; // Process 50 directories at a time
            var semaphore = new SemaphoreSlim(Math.Min(settings.MaxThreads, 8), Math.Min(settings.MaxThreads, 8)); // Limit concurrency for large batches

            for (int i = 0; i < subdirectories.Length; i += batchSize)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var batch = subdirectories.Skip(i).Take(batchSize);
                var batchTasks = new List<Task>();

                foreach (var subdir in batch)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    batchTasks.Add(ProcessSubdirectoryAsync(folderInfo, subdir, settings, semaphore, cancellationToken, currentDepth));
                }

                await Task.WhenAll(batchTasks);

                // Allow GC to collect completed tasks and reduce memory pressure
                if (i % (batchSize * 4) == 0) // Every 4 batches
                {
                    _memoryMonitor?.ForceCleanupIfNeeded();
                }
            }
        }

        private async Task ProcessSubdirectoryAsync(FolderInfo parentFolder, DirectoryInfo subdir, ScanSettings settings, SemaphoreSlim semaphore, CancellationToken cancellationToken, int currentDepth)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var subFolderInfo = await ScanDirectoryAsync(subdir.FullName, settings, cancellationToken, currentDepth + 1);
                if (subFolderInfo != null)
                {
                    lock (_lockObject)
                    {
                        parentFolder.AddSubFolder(subFolderInfo);
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void FinalizeProgress()
        {
            // Set progress to 100% when scan is complete
            var args = new ProgressEventArgs
            {
                PercentComplete = 100,
                CurrentPath = "Scan completed",
                ProcessedFiles = _processedDirectories,
                TotalFiles = _estimatedDirectories
            };

            ProgressChanged?.Invoke(this, args);
        }

        private void ReportProgress(string currentPath, int subdirectoriesCount = 0)
        {
            lock (_lockObject)
            {
                _processedDirectories++;

                // Progressive estimation: update total based on discoveries
                if (_useProgressiveEstimation && subdirectoriesCount > 0)
                {
                    // Add newly discovered subdirectories to our estimate
                    _estimatedDirectories += subdirectoriesCount;
                }

                // Check memory usage and cleanup if needed
                if (_memoryMonitor != null)
                {
                    // Only trigger cleanup when actually needed, not on fixed intervals
                    if (_memoryMonitor.ShouldTriggerCleanup((int)_processedDirectories, 500))
                    {
                        _memoryMonitor.ForceCleanupIfNeeded();
                    }

                    if (_memoryMonitor.IsMemoryLimitExceeded())
                    {
                        LogError($"Memory limit exceeded ({_memoryMonitor.GetMemoryInfo()}) at: {currentPath}");
                        _cancellationTokenSource?.Cancel();
                    }
                }

                // Calculate progress with progressive estimation
                var percentComplete = _estimatedDirectories > 0
                    ? (int)((_processedDirectories * 100) / _estimatedDirectories)
                    : 0;

                // Cap at 95% until scan is complete to show ongoing work
                percentComplete = Math.Min(percentComplete, 95);

                var args = new ProgressEventArgs
                {
                    PercentComplete = percentComplete,
                    CurrentPath = currentPath,
                    ProcessedFiles = _processedDirectories,
                    TotalFiles = _estimatedDirectories
                };

                ProgressChanged?.Invoke(this, args);
            }
        }

        private void LogError(string message)
        {
            lock (_lockObject)
            {
                _errors.Add(message);
            }
        }

        public void CancelScan()
        {
            _cancellationTokenSource?.Cancel();
        }

        public IReadOnlyList<string> GetErrors()
        {
            lock (_lockObject)
            {
                return _errors.ToList();
            }
        }

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        public event EventHandler<ScanResult>? ScanCompleted;
        public event EventHandler<string>? ErrorOccurred;
    }

    public class ProgressEventArgs : EventArgs
    {
        public int PercentComplete { get; set; }
        public string CurrentPath { get; set; } = string.Empty;
        public long ProcessedFiles { get; set; }
        public long TotalFiles { get; set; }
    }
}