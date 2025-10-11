using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FolderVision.Models
{
    public class FolderInfo
    {
        private readonly object _lockObject = new object();

        public FolderInfo()
        {
            SubFolders = new List<FolderInfo>();
        }

        public FolderInfo(string fullPath) : this()
        {
            FullPath = fullPath;
            Name = System.IO.Path.GetFileName(fullPath) ?? string.Empty;
        }

        public string FullPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SubFolderCount { get; set; }
        public int FileCount { get; set; }
        public List<FolderInfo> SubFolders { get; set; }
        public DateTime LastModified { get; set; }

        public void AddSubFolder(FolderInfo subFolder)
        {
            lock (_lockObject)
            {
                if (subFolder != null && !SubFolders.Contains(subFolder))
                {
                    SubFolders.Add(subFolder);
                    UpdateCounts();
                }
            }
        }

        public void UpdateCounts()
        {
            lock (_lockObject)
            {
                SubFolderCount = SubFolders.Count;
            }
        }

        public void SetFileCount(int fileCount)
        {
            lock (_lockObject)
            {
                FileCount = fileCount;
            }
        }

        public IEnumerable<FolderInfo> GetAllSubFolders()
        {
            foreach (var subFolder in SubFolders)
            {
                yield return subFolder;
                foreach (var nestedFolder in subFolder.GetAllSubFolders())
                {
                    yield return nestedFolder;
                }
            }
        }

        public int GetTotalSubFolderCount()
        {
            lock (_lockObject)
            {
                return SubFolders.Sum(sf => 1 + sf.GetTotalSubFolderCount());
            }
        }

        public int GetTotalFileCount()
        {
            lock (_lockObject)
            {
                return FileCount + SubFolders.Sum(sf => sf.GetTotalFileCount());
            }
        }

        public override string ToString()
        {
            lock (_lockObject)
            {
                return $"{Name} ({SubFolderCount} folders, {FileCount} files)";
            }
        }
    }
}