using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FolderVision.Models
{
    public class ScanSettings
    {
        public ScanSettings()
        {
            PathsToScan = new List<string>();
            MaxThreads = 4;
            MaxDepth = 50; // Default safe depth limit
            MaxMemoryUsageMB = 512; // Default 512MB limit
            EnableMemoryOptimization = true; // Enable by default
            GlobalTimeout = TimeSpan.FromMinutes(30); // 30 minutes max total scan
            DirectoryTimeout = TimeSpan.FromSeconds(10); // 10 seconds per directory
            NetworkDriveTimeout = TimeSpan.FromSeconds(30); // 30 seconds for network drives
            EnableAdaptiveBatching = true; // Enable adaptive batch sizing for large folders
            MaxDirectoriesPerBatch = 100; // Default batch size (overridden by adaptive batching)
        }

        public List<string> PathsToScan { get; set; }
        public bool SkipSystemFolders { get; set; }
        public bool SkipHiddenFolders { get; set; }
        public int MaxThreads { get; set; }
        public int MaxDepth { get; set; }
        public long MaxMemoryUsageMB { get; set; }
        public bool EnableMemoryOptimization { get; set; }
        public TimeSpan GlobalTimeout { get; set; }
        public TimeSpan DirectoryTimeout { get; set; }
        public TimeSpan NetworkDriveTimeout { get; set; }

        /// <summary>
        /// Enable adaptive batch sizing for large folder structures (>1000 subdirectories)
        /// When enabled, batch size and concurrency are automatically adjusted based on directory count
        /// </summary>
        public bool EnableAdaptiveBatching { get; set; }

        /// <summary>
        /// Maximum number of directories to process in a single batch (when adaptive batching is disabled)
        /// </summary>
        public int MaxDirectoriesPerBatch { get; set; }

        public void AddPathToScan(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && !PathsToScan.Contains(path))
            {
                PathsToScan.Add(path);
            }
        }

        public void RemovePathToScan(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                PathsToScan.Remove(path);
            }
        }

        public void ClearPaths()
        {
            PathsToScan.Clear();
        }

        public bool ShouldSkipFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return true;

            var folderName = Path.GetFileName(folderPath);
            var attributes = File.GetAttributes(folderPath);

            if (SkipHiddenFolders && (attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return true;

            if (SkipSystemFolders && (attributes & FileAttributes.System) == FileAttributes.System)
                return true;

            return false;
        }

        public bool HasValidPaths()
        {
            return PathsToScan.Any(path => Directory.Exists(path));
        }

        public void ValidatePaths()
        {
            PathsToScan = PathsToScan.Where(path => Directory.Exists(path)).ToList();
        }

        public static ScanSettings CreateDefault()
        {
            return new ScanSettings
            {
                SkipSystemFolders = true,
                SkipHiddenFolders = true,
                MaxThreads = 4,
                MaxDepth = 50,
                MaxMemoryUsageMB = 512,
                EnableMemoryOptimization = true,
                GlobalTimeout = TimeSpan.FromMinutes(30),
                DirectoryTimeout = TimeSpan.FromSeconds(10),
                NetworkDriveTimeout = TimeSpan.FromSeconds(30),
                EnableAdaptiveBatching = true,
                MaxDirectoriesPerBatch = 100
            };
        }

        /// <summary>
        /// Creates settings optimized for scanning very large folder structures (>10k directories)
        /// </summary>
        public static ScanSettings CreateForLargeFolders()
        {
            return new ScanSettings
            {
                SkipSystemFolders = true,
                SkipHiddenFolders = true,
                MaxThreads = 8, // More threads for better parallelization
                MaxDepth = 100, // Higher depth limit
                MaxMemoryUsageMB = 1024, // 1GB for large scans
                EnableMemoryOptimization = true,
                GlobalTimeout = TimeSpan.FromHours(2), // Longer timeout for large scans
                DirectoryTimeout = TimeSpan.FromSeconds(30), // More time per directory
                NetworkDriveTimeout = TimeSpan.FromMinutes(2),
                EnableAdaptiveBatching = true, // Critical for large folders
                MaxDirectoriesPerBatch = 50 // Smaller batches for memory efficiency
            };
        }

        public ScanSettings Clone()
        {
            return new ScanSettings
            {
                PathsToScan = new List<string>(PathsToScan),
                SkipSystemFolders = SkipSystemFolders,
                SkipHiddenFolders = SkipHiddenFolders,
                MaxThreads = MaxThreads,
                MaxDepth = MaxDepth,
                MaxMemoryUsageMB = MaxMemoryUsageMB,
                EnableMemoryOptimization = EnableMemoryOptimization,
                GlobalTimeout = GlobalTimeout,
                DirectoryTimeout = DirectoryTimeout,
                NetworkDriveTimeout = NetworkDriveTimeout,
                EnableAdaptiveBatching = EnableAdaptiveBatching,
                MaxDirectoriesPerBatch = MaxDirectoriesPerBatch
            };
        }

        public override string ToString()
        {
            return $"ScanSettings: {PathsToScan.Count} paths, MaxThreads={MaxThreads}, MaxDepth={MaxDepth}, SkipSystem={SkipSystemFolders}, SkipHidden={SkipHiddenFolders}";
        }
    }
}