using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FolderVision.Models
{
    public class ScanResult
    {
        private readonly object _lockObject = new object();

        public ScanResult()
        {
            RootFolders = new List<FolderInfo>();
            ScannedPaths = new List<string>();
        }

        public List<FolderInfo> RootFolders { get; set; }
        public int TotalFolders { get; set; }
        public int TotalFiles { get; set; }
        public DateTime ScanStartTime { get; set; }
        public TimeSpan ScanDuration { get; set; }
        public List<string> ScannedPaths { get; set; }

        public void AddRootFolder(FolderInfo folder)
        {
            lock (_lockObject)
            {
                if (folder != null && !RootFolders.Contains(folder))
                {
                    RootFolders.Add(folder);
                    UpdateTotals();
                }
            }
        }

        public void AddScannedPath(string path)
        {
            lock (_lockObject)
            {
                if (!string.IsNullOrEmpty(path) && !ScannedPaths.Contains(path))
                {
                    ScannedPaths.Add(path);
                }
            }
        }

        public void UpdateTotals()
        {
            lock (_lockObject)
            {
                TotalFolders = RootFolders.Sum(rf => 1 + rf.GetTotalSubFolderCount());
                TotalFiles = RootFolders.Sum(rf => rf.GetTotalFileCount());
            }
        }

        public void SetScanDuration(DateTime endTime)
        {
            ScanDuration = endTime - ScanStartTime;
        }

        public FolderInfo? FindFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            foreach (var rootFolder in RootFolders)
            {
                if (rootFolder.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase))
                    return rootFolder;

                var found = FindFolderRecursive(rootFolder, path);
                if (found != null)
                    return found;
            }

            return null;
        }

        private FolderInfo? FindFolderRecursive(FolderInfo folder, string path)
        {
            foreach (var subFolder in folder.SubFolders)
            {
                if (subFolder.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase))
                    return subFolder;

                var found = FindFolderRecursive(subFolder, path);
                if (found != null)
                    return found;
            }

            return null;
        }

        public IEnumerable<FolderInfo> GetAllFolders()
        {
            foreach (var rootFolder in RootFolders)
            {
                yield return rootFolder;
                foreach (var subFolder in rootFolder.GetAllSubFolders())
                {
                    yield return subFolder;
                }
            }
        }

        public override string ToString()
        {
            lock (_lockObject)
            {
                return $"Scan Result: {TotalFolders} folders, {TotalFiles} files in {ScanDuration.TotalSeconds:F1}s";
            }
        }
    }
}