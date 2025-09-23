using System;
using System.Collections.Generic;

namespace FolderVision.Models
{
    public class ScanSettings
    {
        public ScanSettings()
        {
            ExcludedFolders = new List<string>();
            ExcludedExtensions = new List<string>();
            IncludedExtensions = new List<string>();
        }

        public int MaxDepth { get; set; } = int.MaxValue;
        public bool IncludeHiddenFiles { get; set; } = false;
        public bool IncludeSystemFiles { get; set; } = false;
        public bool FollowSymlinks { get; set; } = false;
        public int MaxThreads { get; set; } = Environment.ProcessorCount;
        public long MaxFileSize { get; set; } = long.MaxValue;
        public long MinFileSize { get; set; } = 0;
        public List<string> ExcludedFolders { get; set; }
        public List<string> ExcludedExtensions { get; set; }
        public List<string> IncludedExtensions { get; set; }
        public bool UseProgressReporting { get; set; } = true;
        public TimeSpan ScanTimeout { get; set; } = TimeSpan.FromHours(1);

        public void AddExcludedFolder(string folderPath)
        {
            throw new NotImplementedException();
        }

        public void AddExcludedExtension(string extension)
        {
            throw new NotImplementedException();
        }

        public void AddIncludedExtension(string extension)
        {
            throw new NotImplementedException();
        }

        public void RemoveExcludedFolder(string folderPath)
        {
            throw new NotImplementedException();
        }

        public void RemoveExcludedExtension(string extension)
        {
            throw new NotImplementedException();
        }

        public bool ShouldExcludeFolder(string folderPath)
        {
            throw new NotImplementedException();
        }

        public bool ShouldExcludeFile(string filePath, long fileSize)
        {
            throw new NotImplementedException();
        }

        public static ScanSettings Default => new ScanSettings();
    }
}