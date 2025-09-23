using System;
using System.Collections.Generic;

namespace FolderVision.Models
{
    public class FolderInfo
    {
        public FolderInfo()
        {
            SubFolders = new List<FolderInfo>();
            Files = new List<FileInfo>();
        }

        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public FolderInfo? Parent { get; set; }
        public List<FolderInfo> SubFolders { get; set; }
        public List<FileInfo> Files { get; set; }
        public long Size { get; set; }
        public int FileCount { get; set; }
        public int SubFolderCount { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime Created { get; set; }
        public int Depth { get; set; }

        public void AddSubFolder(FolderInfo subFolder)
        {
            throw new NotImplementedException();
        }

        public void AddFile(FileInfo file)
        {
            throw new NotImplementedException();
        }

        public void CalculateSize()
        {
            throw new NotImplementedException();
        }

        public void CalculateCounts()
        {
            throw new NotImplementedException();
        }

        public string GetFormattedSize()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<FolderInfo> GetAllSubFolders()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<FileInfo> GetAllFiles()
        {
            throw new NotImplementedException();
        }
    }

    public class FileInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime Created { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsHidden { get; set; }

        public string GetFormattedSize()
        {
            throw new NotImplementedException();
        }
    }
}