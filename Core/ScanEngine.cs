using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FolderVision.Models;

namespace FolderVision.Core
{
    public class ScanEngine
    {
        public ScanEngine()
        {
        }

        public async Task<ScanResult> ScanFolderAsync(string folderPath, ScanSettings settings)
        {
            throw new NotImplementedException();
        }

        public void CancelScan()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        public event EventHandler<ScanResult>? ScanCompleted;
    }

    public class ProgressEventArgs : EventArgs
    {
        public int PercentComplete { get; set; }
        public string CurrentPath { get; set; } = string.Empty;
        public long ProcessedFiles { get; set; }
        public long TotalFiles { get; set; }
    }
}