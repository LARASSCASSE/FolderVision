using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FolderVision.Models
{
    public static class ScanResultExtensions
    {
        public static List<string> GetScannedPathsList(this ScanResult scanResult)
        {
            return scanResult.ScannedPaths.ToList();
        }

        public static List<FolderInfo> GetRootFoldersList(this ScanResult scanResult)
        {
            return scanResult.RootFolders.ToList();
        }
    }

    public static class FolderInfoExtensions
    {
        public static List<FolderInfo> GetSubFoldersList(this FolderInfo folderInfo)
        {
            return folderInfo.SubFolders.ToList();
        }
    }
}