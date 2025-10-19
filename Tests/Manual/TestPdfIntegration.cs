using System;
using System.Collections.Generic;
using FolderVision.Models;
using FolderVision.Utils;

namespace FolderVision
{
    public class TestPdfIntegration
    {
        public static void RunTests()
        {
            Console.WriteLine("=== Testing PDF Integration ===");
            Console.WriteLine();

            // Create mock scan result
            var scanResult = CreateMockScanResult();

            Console.WriteLine("Mock ScanResult created:");
            Console.WriteLine($"- Total Folders: {scanResult.TotalFolders}");
            Console.WriteLine($"- Total Files: {scanResult.TotalFiles}");
            Console.WriteLine($"- Scanned Paths: {string.Join(", ", scanResult.ScannedPaths)}");
            Console.WriteLine($"- Root Folders: {scanResult.RootFolders.Count}");
            Console.WriteLine();

            // Test folder name generation
            var folderName = FileHelper.CreateScanFolderName(scanResult.ScannedPaths);
            Console.WriteLine($"Generated folder name: {folderName}");

            // Test output path generation (simulated)
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var outputFolder = System.IO.Path.Combine(desktop, folderName);
            var htmlPath = System.IO.Path.Combine(outputFolder, "FolderScan_Report.html");
            var pdfPath = System.IO.Path.Combine(outputFolder, "FolderScan_Report.pdf");

            Console.WriteLine();
            Console.WriteLine("Expected output paths:");
            Console.WriteLine($"Folder: {outputFolder}");
            Console.WriteLine($"HTML: {htmlPath}");
            Console.WriteLine($"PDF: {pdfPath}");

            Console.WriteLine();
            Console.WriteLine("=== PDF Integration test completed ===");
        }

        private static ScanResult CreateMockScanResult()
        {
            var scanResult = new ScanResult
            {
                ScanStartTime = DateTime.Now.AddMinutes(-5),
                TotalFolders = 1250,
                TotalFiles = 15780
            };

            scanResult.SetScanDuration(DateTime.Now);
            scanResult.AddScannedPath("C:\\");

            // Create mock root folder
            var rootFolder = new FolderInfo
            {
                Name = "C:",
                FullPath = "C:\\",
                SubFolderCount = 15,
                FileCount = 125
            };

            // Add some mock subfolders
            rootFolder.SubFolders.Add(new FolderInfo
            {
                Name = "Users",
                FullPath = "C:\\Users",
                SubFolderCount = 8,
                FileCount = 42
            });

            rootFolder.SubFolders.Add(new FolderInfo
            {
                Name = "Program Files",
                FullPath = "C:\\Program Files",
                SubFolderCount = 125,
                FileCount = 2340
            });

            scanResult.AddRootFolder(rootFolder);

            return scanResult;
        }
    }
}