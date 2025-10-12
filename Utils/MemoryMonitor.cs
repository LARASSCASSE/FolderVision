using System;

namespace FolderVision.Utils
{
    public class MemoryMonitor
    {
        private readonly long _maxMemoryMB;
        private readonly bool _enableOptimization;
        private long _lastMemoryCheck;
        private DateTime _lastGCTime = DateTime.MinValue;
        private const int MinGCIntervalMs = 5000; // Minimum 5 seconds between forced GCs

        public MemoryMonitor(long maxMemoryMB, bool enableOptimization = true)
        {
            _maxMemoryMB = maxMemoryMB;
            _enableOptimization = enableOptimization;
            _lastMemoryCheck = GC.GetTotalMemory(false) / (1024 * 1024);
        }

        public long GetCurrentMemoryUsageMB()
        {
            // Use cached value for frequent checks, only force GC for critical measurements
            var currentMemory = GC.GetTotalMemory(false) / (1024 * 1024);
            _lastMemoryCheck = currentMemory;
            return currentMemory;
        }

        public long GetAccurateMemoryUsageMB()
        {
            // Only use this for critical decisions, not frequent monitoring
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var accurateMemory = GC.GetTotalMemory(false) / (1024 * 1024);
            _lastMemoryCheck = accurateMemory;
            return accurateMemory;
        }

        public bool IsMemoryLimitExceeded()
        {
            if (!_enableOptimization)
                return false;

            // Quick check first
            var currentMemory = GetCurrentMemoryUsageMB();

            // If significantly over limit, do accurate measurement
            if (currentMemory > _maxMemoryMB * 1.1) // 10% buffer before accurate check
            {
                return GetAccurateMemoryUsageMB() > _maxMemoryMB;
            }

            return currentMemory > _maxMemoryMB;
        }

        public void ForceCleanupIfNeeded()
        {
            if (!_enableOptimization)
                return;

            var now = DateTime.Now;

            // Rate-limit forced GCs to prevent performance impact
            if ((now - _lastGCTime).TotalMilliseconds < MinGCIntervalMs)
                return;

            if (IsMemoryLimitExceeded())
            {
                // Use less aggressive GC first
                GC.Collect(1, GCCollectionMode.Optimized);

                // Only do full GC if still over limit after gentle collection
                if (GetCurrentMemoryUsageMB() > _maxMemoryMB)
                {
                    GC.Collect(2, GCCollectionMode.Forced);
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                _lastGCTime = now;
            }
        }

        public bool ShouldTriggerCleanup(int processedCount, int cleanupInterval = 1000)
        {
            if (!_enableOptimization)
                return false;

            // Only suggest cleanup at intervals and if memory usage is growing
            return (processedCount % cleanupInterval == 0) &&
                   (GetCurrentMemoryUsageMB() > _maxMemoryMB * 0.8); // 80% threshold
        }

        public double GetMemoryUsagePercentage()
        {
            return (double)GetCurrentMemoryUsageMB() / _maxMemoryMB * 100;
        }


        public string GetMemoryInfo()
        {
            var current = GetCurrentMemoryUsageMB();
            var percentage = GetMemoryUsagePercentage();
            return $"Memory: {current}/{_maxMemoryMB} MB ({percentage:F1}%)";
        }
    }
}