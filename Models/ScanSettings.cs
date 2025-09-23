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
        }

        public List<string> PathsToScan { get; set; }
        public bool SkipSystemFolders { get; set; }
        public bool SkipHiddenFolders { get; set; }
        public int MaxThreads { get; set; }

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
                MaxThreads = 4
            };
        }

        public ScanSettings Clone()
        {
            return new ScanSettings
            {
                PathsToScan = new List<string>(PathsToScan),
                SkipSystemFolders = SkipSystemFolders,
                SkipHiddenFolders = SkipHiddenFolders,
                MaxThreads = MaxThreads
            };
        }

        public override string ToString()
        {
            return $"ScanSettings: {PathsToScan.Count} paths, MaxThreads={MaxThreads}, SkipSystem={SkipSystemFolders}, SkipHidden={SkipHiddenFolders}";
        }
    }
}