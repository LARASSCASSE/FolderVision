using System;
using System.Collections.Generic;

namespace FolderVision.Models
{
    public class ScanResult
    {
        public ScanResult()
        {
            Folders = new List<FolderInfo>();
            Errors = new List<string>();
        }

        public string RootPath { get; set; } = string.Empty;
        public DateTime ScanStartTime { get; set; }
        public DateTime ScanEndTime { get; set; }
        public TimeSpan ScanDuration => ScanEndTime - ScanStartTime;
        public List<FolderInfo> Folders { get; set; }
        public long TotalFiles { get; set; }
        public long TotalDirectories { get; set; }
        public long TotalSize { get; set; }
        public List<string> Errors { get; set; }
        public bool IsCompleted { get; set; }
        public bool WasCancelled { get; set; }

        public void AddFolder(FolderInfo folder)
        {
            throw new NotImplementedException();
        }

        public void AddError(string error)
        {
            throw new NotImplementedException();
        }

        public FolderInfo? FindFolder(string path)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<FolderInfo> GetLargestFolders(int count = 10)
        {
            throw new NotImplementedException();
        }

        public string GetFormattedSize()
        {
            throw new NotImplementedException();
        }
    }
}