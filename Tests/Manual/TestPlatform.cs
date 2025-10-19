using System;
using FolderVision.Utils;

namespace FolderVision
{
    public static class TestPlatform
    {
        public static void TestPlatformHelper()
        {
            Console.WriteLine("=== PLATFORM HELPER TEST ===");
            Console.WriteLine();

            Console.WriteLine("Platform Detection:");
            Console.WriteLine($"- Is Windows: {PlatformHelper.IsWindows}");
            Console.WriteLine($"- Is Linux: {PlatformHelper.IsLinux}");
            Console.WriteLine($"- Is macOS: {PlatformHelper.IsMacOS}");
            Console.WriteLine($"- Platform Info: {PlatformHelper.GetPlatformInfo()}");
            Console.WriteLine();

            Console.WriteLine("Terminology:");
            Console.WriteLine($"- Scan Menu Title: {PlatformHelper.Terminology.ScanDrivesMenuTitle}");
            Console.WriteLine($"- Storage Title: {PlatformHelper.Terminology.AvailableStorageTitle}");
            Console.WriteLine($"- Folder Term: {PlatformHelper.Terminology.FolderOrDirectoryTerm}");
            Console.WriteLine($"- Drive Term: {PlatformHelper.Terminology.DriveOrVolumeTerm}");
            Console.WriteLine($"- Network Term: {PlatformHelper.Terminology.NetworkStorageTerm}");
            Console.WriteLine();

            Console.WriteLine("Symbols:");
            Console.WriteLine($"- Folder: {PlatformHelper.Symbols.FolderSymbol}");
            Console.WriteLine($"- Drive: {PlatformHelper.Symbols.DriveSymbol}");
            Console.WriteLine($"- Network: {PlatformHelper.Symbols.NetworkSymbol}");
            Console.WriteLine($"- Removable: {PlatformHelper.Symbols.RemovableSymbol}");
            Console.WriteLine();

            Console.WriteLine("Path Examples:");
            var examples = PlatformHelper.PathExamples.GetCommonPaths();
            foreach (var example in examples)
            {
                Console.WriteLine($"- {example}");
            }
            Console.WriteLine();

            Console.WriteLine("User-Friendly Paths:");
            var userPaths = PlatformHelper.PathExamples.GetUserFriendlyPaths();
            foreach (var path in userPaths)
            {
                Console.WriteLine($"- {path}");
            }
            Console.WriteLine();

            Console.WriteLine("Drive Type Descriptions:");
            Console.WriteLine($"- Fixed: {PlatformHelper.GetDriveTypeDescription(System.IO.DriveType.Fixed)}");
            Console.WriteLine($"- Network: {PlatformHelper.GetDriveTypeDescription(System.IO.DriveType.Network)}");
            Console.WriteLine($"- Removable: {PlatformHelper.GetDriveTypeDescription(System.IO.DriveType.Removable)}");
            Console.WriteLine($"- CD/ROM: {PlatformHelper.GetDriveTypeDescription(System.IO.DriveType.CDRom)}");
            Console.WriteLine();
        }
    }
}