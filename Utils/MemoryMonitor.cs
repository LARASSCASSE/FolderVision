using System;

namespace FolderVision.Utils
{
    public class MemoryMonitor
    {
        private readonly long _maxMemoryMB;
        private readonly bool _enableOptimization;

        public MemoryMonitor(long maxMemoryMB, bool enableOptimization = true)
        {
            _maxMemoryMB = maxMemoryMB;
            _enableOptimization = enableOptimization;
        }

        public long GetCurrentMemoryUsageMB()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return GC.GetTotalMemory(false) / (1024 * 1024);
        }

        public bool IsMemoryLimitExceeded()
        {
            if (!_enableOptimization)
                return false;

            return GetCurrentMemoryUsageMB() > _maxMemoryMB;
        }

        public void ForceCleanupIfNeeded()
        {
            if (IsMemoryLimitExceeded())
            {
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        public double GetMemoryUsagePercentage()
        {
            return (double)GetCurrentMemoryUsageMB() / _maxMemoryMB * 100;
        }

        public bool ShouldTriggerGC(int processedCount, int gcInterval = 1000)
        {
            return _enableOptimization && (processedCount % gcInterval == 0);
        }

        public string GetMemoryInfo()
        {
            var current = GetCurrentMemoryUsageMB();
            var percentage = GetMemoryUsagePercentage();
            return $"Memory: {current}/{_maxMemoryMB} MB ({percentage:F1}%)";
        }
    }
}