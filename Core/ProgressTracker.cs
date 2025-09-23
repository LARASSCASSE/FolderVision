using System;

namespace FolderVision.Core
{
    public class ProgressTracker
    {
        private long _totalItems;
        private long _processedItems;
        private DateTime _startTime;

        public ProgressTracker()
        {
        }

        public void Initialize(long totalItems)
        {
            throw new NotImplementedException();
        }

        public void UpdateProgress(long processedItems)
        {
            throw new NotImplementedException();
        }

        public void IncrementProgress(long increment = 1)
        {
            throw new NotImplementedException();
        }

        public double PercentComplete => _totalItems > 0 ? (_processedItems * 100.0) / _totalItems : 0;
        public long ProcessedItems => _processedItems;
        public long TotalItems => _totalItems;
        public TimeSpan ElapsedTime => DateTime.Now - _startTime;
        public TimeSpan EstimatedTimeRemaining => TimeSpan.Zero;

        public event EventHandler<ProgressChangedEventArgs>? ProgressChanged;
    }

    public class ProgressChangedEventArgs : EventArgs
    {
        public double PercentComplete { get; set; }
        public long ProcessedItems { get; set; }
        public long TotalItems { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }
}