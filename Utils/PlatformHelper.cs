using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FolderVision.Utils
{
    public static class PlatformHelper
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsUnix => IsLinux || IsMacOS;

        public static class Terminology
        {
            public static string ScanDrivesMenuTitle => IsWindows ? "Scan Drives" : "Scan File Systems";
            public static string AvailableStorageTitle => IsWindows ? "Available Drives:" : "Available File Systems:";
            public static string FolderOrDirectoryTerm => IsWindows ? "folder" : "directory";
            public static string DriveOrVolumeTerm => IsWindows ? "drive" : "volume";
            public static string NetworkStorageTerm => IsWindows ? "Network Drive" : "Network Mount";
            public static string RemovableStorageTerm => IsWindows ? "Removable Drive" : "Removable Media";
            public static string OpticalStorageTerm => IsWindows ? "CD/DVD Drive" : "Optical Media";
        }

        public static class PathExamples
        {
            public static List<string> GetCommonPaths()
            {
                if (IsWindows)
                {
                    return new List<string>
                    {
                        @"C:\Users",
                        @"C:\Program Files",
                        @"D:\Projects",
                        @"E:\Documents"
                    };
                }
                else if (IsMacOS)
                {
                    return new List<string>
                    {
                        "/Users",
                        "/Applications",
                        "/Documents",
                        "/Volumes/ExternalDrive"
                    };
                }
                else // Linux
                {
                    return new List<string>
                    {
                        "/home/user/documents",
                        "/opt/projects",
                        "/var/data",
                        "/mnt/external"
                    };
                }
            }

            public static List<string> GetUserFriendlyPaths()
            {
                if (IsWindows)
                {
                    return new List<string>
                    {
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        @"C:\Program Files"
                    };
                }
                else if (IsMacOS)
                {
                    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    return new List<string>
                    {
                        homeDir,
                        $"{homeDir}/Documents",
                        $"{homeDir}/Desktop",
                        "/Applications"
                    };
                }
                else // Linux
                {
                    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    return new List<string>
                    {
                        homeDir,
                        $"{homeDir}/Documents",
                        $"{homeDir}/Desktop",
                        "/opt"
                    };
                }
            }
        }

        public static class Symbols
        {
            public static string FolderSymbol => IsWindows ? "📁" : "📂";
            public static string DriveSymbol => IsWindows ? "💾" : "💽";
            public static string NetworkSymbol => "🌐";
            public static string RemovableSymbol => IsWindows ? "🔌" : "📱";
            public static string OpticalSymbol => "💿";
            public static string SystemSymbol => IsWindows ? "⚙️" : "🔧";
        }

        public static string GetPlatformInfo()
        {
            var platform = IsWindows ? "Windows" : IsMacOS ? "macOS" : "Linux";
            var architecture = RuntimeInformation.OSArchitecture.ToString();
            return $"{platform} ({architecture})";
        }

        public static string GetDriveTypeDescription(System.IO.DriveType driveType)
        {
            return driveType switch
            {
                System.IO.DriveType.Fixed => IsWindows ? "Local Drive" : "Fixed Volume",
                System.IO.DriveType.Network => Terminology.NetworkStorageTerm,
                System.IO.DriveType.Removable => Terminology.RemovableStorageTerm,
                System.IO.DriveType.CDRom => Terminology.OpticalStorageTerm,
                System.IO.DriveType.Ram => IsWindows ? "RAM Drive" : "RAM Disk",
                _ => "Unknown"
            };
        }
    }
}