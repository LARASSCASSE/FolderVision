using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FolderVision.Core
{
    public class ProgressTracker
    {
        private readonly object _lockObject = new object();
        private readonly ConcurrentDictionary<int, ThreadProgress> _threadProgresses;
        private readonly List<DateTime> _progressHistory;
        private long _totalThreads;
        private long _completedThreads;
        private DateTime _startTime;
        private DateTime _lastUpdateTime;
        private bool _isInitialized;

        public ProgressTracker()
        {
            _threadProgresses = new ConcurrentDictionary<int, ThreadProgress>();
            _progressHistory = new List<DateTime>();
        }

        public void Initialize(long totalThreads)
        {
            lock (_lockObject)
            {
                _totalThreads = Math.Max(1, totalThreads);
                _completedThreads = 0;
                _startTime = DateTime.Now;
                _lastUpdateTime = _startTime;
                _isInitialized = true;
                _threadProgresses.Clear();
                _progressHistory.Clear();

                // Initialize thread progress tracking
                for (int i = 0; i < totalThreads; i++)
                {
                    _threadProgresses.TryAdd(i, new ThreadProgress
                    {
                        ThreadId = i,
                        PercentComplete = 0,
                        Status = ThreadProgressStatus.Pending,
                        StartTime = _startTime
                    });
                }

                RaiseProgressChanged();
            }
        }

        public void UpdateThreadProgress(int threadId, int percentComplete, string currentPath = "")
        {
            if (!_isInitialized) return;

            if (_threadProgresses.TryGetValue(threadId, out var threadProgress))
            {
                lock (_lockObject)
                {
                    threadProgress.PercentComplete = Math.Min(100, Math.Max(0, percentComplete));
                    threadProgress.CurrentPath = currentPath ?? string.Empty;
                    threadProgress.LastUpdateTime = DateTime.Now;
                    threadProgress.Status = ThreadProgressStatus.Running;

                    _lastUpdateTime = DateTime.Now;
                    _progressHistory.Add(_lastUpdateTime);

                    // Keep only recent history for rate calculation (last 10 updates)
                    if (_progressHistory.Count > 10)
                    {
                        _progressHistory.RemoveAt(0);
                    }

                    RaiseProgressChanged();
                }
            }
        }

        public void CompleteThread(int threadId)
        {
            if (!_isInitialized) return;

            if (_threadProgresses.TryGetValue(threadId, out var threadProgress))
            {
                lock (_lockObject)
                {
                    if (threadProgress.Status != ThreadProgressStatus.Completed)
                    {
                        threadProgress.PercentComplete = 100;
                        threadProgress.Status = ThreadProgressStatus.Completed;
                        threadProgress.EndTime = DateTime.Now;
                        _completedThreads++;
                        _lastUpdateTime = DateTime.Now;

                        RaiseProgressChanged();
                    }
                }
            }
        }

        public void FailThread(int threadId, string error)
        {
            if (!_isInitialized) return;

            if (_threadProgresses.TryGetValue(threadId, out var threadProgress))
            {
                lock (_lockObject)
                {
                    if (threadProgress.Status != ThreadProgressStatus.Failed &&
                        threadProgress.Status != ThreadProgressStatus.Completed)
                    {
                        threadProgress.Status = ThreadProgressStatus.Failed;
                        threadProgress.Error = error;
                        threadProgress.EndTime = DateTime.Now;
                        _completedThreads++;
                        _lastUpdateTime = DateTime.Now;

                        RaiseProgressChanged();
                    }
                }
            }
        }

        public void UpdateProgress(long processedItems)
        {
            // This method is kept for backward compatibility
            lock (_lockObject)
            {
                _lastUpdateTime = DateTime.Now;
                RaiseProgressChanged();
            }
        }

        public void IncrementProgress(long increment = 1)
        {
            // This method is kept for backward compatibility
            lock (_lockObject)
            {
                _lastUpdateTime = DateTime.Now;
                RaiseProgressChanged();
            }
        }

        private void RaiseProgressChanged()
        {
            var args = new ProgressChangedEventArgs
            {
                PercentComplete = OverallPercentComplete,
                ProcessedItems = _completedThreads,
                TotalItems = _totalThreads,
                ElapsedTime = ElapsedTime,
                EstimatedTimeRemaining = EstimatedTimeRemaining,
                ActiveThreads = GetActiveThreadCount(),
                CompletedThreads = _completedThreads,
                FailedThreads = GetFailedThreadCount(),
                ThreadDetails = GetThreadProgressSummary()
            };

            ProgressChanged?.Invoke(this, args);
        }

        private int GetActiveThreadCount()
        {
            return _threadProgresses.Values.Count(tp => tp.Status == ThreadProgressStatus.Running);
        }

        private int GetFailedThreadCount()
        {
            return _threadProgresses.Values.Count(tp => tp.Status == ThreadProgressStatus.Failed);
        }

        private List<ThreadProgressSummary> GetThreadProgressSummary()
        {
            return _threadProgresses.Values.Select(tp => new ThreadProgressSummary
            {
                ThreadId = tp.ThreadId,
                PercentComplete = tp.PercentComplete,
                Status = tp.Status,
                CurrentPath = tp.CurrentPath,
                ElapsedTime = tp.ElapsedTime,
                Error = tp.Error
            }).ToList();
        }

        public double OverallPercentComplete
        {
            get
            {
                if (!_isInitialized || _totalThreads == 0) return 0;

                lock (_lockObject)
                {
                    var totalProgress = _threadProgresses.Values.Sum(tp => tp.PercentComplete);
                    return totalProgress / _totalThreads;
                }
            }
        }

        public long ProcessedItems => _completedThreads;
        public long TotalItems => _totalThreads;
        public TimeSpan ElapsedTime => _isInitialized ? DateTime.Now - _startTime : TimeSpan.Zero;

        public TimeSpan EstimatedTimeRemaining
        {
            get
            {
                if (!_isInitialized || _totalThreads == 0) return TimeSpan.Zero;

                lock (_lockObject)
                {
                    var overallPercent = OverallPercentComplete;
                    if (overallPercent <= 0) return TimeSpan.MaxValue;
                    if (overallPercent >= 100) return TimeSpan.Zero;

                    var elapsed = ElapsedTime;
                    var estimatedTotal = elapsed.TotalSeconds * (100.0 / overallPercent);
                    var remaining = estimatedTotal - elapsed.TotalSeconds;

                    return TimeSpan.FromSeconds(Math.Max(0, remaining));
                }
            }
        }

        public double ProcessingRate
        {
            get
            {
                lock (_lockObject)
                {
                    if (_progressHistory.Count < 2) return 0;

                    var timeSpan = _progressHistory.Last() - _progressHistory.First();
                    if (timeSpan.TotalSeconds <= 0) return 0;

                    return _progressHistory.Count / timeSpan.TotalSeconds;
                }
            }
        }

        public IReadOnlyList<ThreadProgress> GetThreadProgresses()
        {
            return _threadProgresses.Values.ToList();
        }

        public ThreadProgress? GetThreadProgress(int threadId)
        {
            return _threadProgresses.TryGetValue(threadId, out var progress) ? progress : null;
        }

        public bool IsCompleted => _isInitialized && _completedThreads >= _totalThreads;
        public bool HasFailures => GetFailedThreadCount() > 0;

        public event EventHandler<ProgressChangedEventArgs>? ProgressChanged;
    }

    public class ThreadProgress
    {
        public int ThreadId { get; set; }
        public int PercentComplete { get; set; }
        public ThreadProgressStatus Status { get; set; }
        public string CurrentPath { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Error { get; set; }

        public TimeSpan ElapsedTime => (EndTime ?? DateTime.Now) - StartTime;
        public TimeSpan TimeSinceLastUpdate => DateTime.Now - LastUpdateTime;
    }

    public class ThreadProgressSummary
    {
        public int ThreadId { get; set; }
        public int PercentComplete { get; set; }
        public ThreadProgressStatus Status { get; set; }
        public string CurrentPath { get; set; } = string.Empty;
        public TimeSpan ElapsedTime { get; set; }
        public string? Error { get; set; }
    }

    public enum ThreadProgressStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public class ProgressChangedEventArgs : EventArgs
    {
        public double PercentComplete { get; set; }
        public long ProcessedItems { get; set; }
        public long TotalItems { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public int ActiveThreads { get; set; }
        public long CompletedThreads { get; set; }
        public int FailedThreads { get; set; }
        public List<ThreadProgressSummary> ThreadDetails { get; set; } = new List<ThreadProgressSummary>();
    }
}