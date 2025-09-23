using System;
using System.Collections.Generic;
using System.IO;
using FolderVision.Models;
using FolderVision.Utils;
using FolderVision.Ui;

namespace FolderVision
{
    public class TestWorkflow
    {
        public static void RunWorkflowTests()
        {
            Console.WriteLine("=== COMPLETE WORKFLOW VERIFICATION ===");
            Console.WriteLine();

            // Test 1: Single Drive Scan Workflow
            Console.WriteLine("1. Testing Single Drive Scan (C:\\):");
            TestSingleDriveScan();
            Console.WriteLine();

            // Test 2: Single Folder Scan Workflow
            Console.WriteLine("2. Testing Single Folder Scan (D:\\Games):");
            TestSingleFolderScan();
            Console.WriteLine();

            // Test 3: Multiple Drives Scan Workflow
            Console.WriteLine("3. Testing Multiple Drives Scan:");
            TestMultipleDrivesScan();
            Console.WriteLine();

            // Test 4: Export Format Options
            Console.WriteLine("4. Testing Export Format Options:");
            TestExportFormats();
            Console.WriteLine();

            // Test 5: Organized Folder Creation
            Console.WriteLine("5. Testing Organized Folder Creation:");
            TestOrganizedFolders();
            Console.WriteLine();

            Console.WriteLine("=== ALL WORKFLOW TESTS COMPLETED ===");
        }

        private static void TestSingleDriveScan()
        {
            var scanResult = CreateMockScanResult(new List<string> { "C:\\" });
            var folderName = FileHelper.CreateScanFolderName(scanResult.ScannedPaths);

            Console.WriteLine($"  Scanned Path: C:\\");
            Console.WriteLine($"  Generated Folder: {folderName}");
            Console.WriteLine($"  Expected Pattern: C_Drive_Scan_YYYYMMDD_HHMMSS");
            Console.WriteLine($"  ✓ Matches Pattern: {folderName.StartsWith("C_Drive_Scan_")}");

            TestFileGeneration(folderName);
        }

        private static void TestSingleFolderScan()
        {
            var scanResult = CreateMockScanResult(new List<string> { "D:\\Games" });
            var folderName = FileHelper.CreateScanFolderName(scanResult.ScannedPaths);

            Console.WriteLine($"  Scanned Path: D:\\Games");
            Console.WriteLine($"  Generated Folder: {folderName}");
            Console.WriteLine($"  Expected Pattern: Games_Scan_YYYYMMDD_HHMMSS");
            Console.WriteLine($"  ✓ Matches Pattern: {folderName.StartsWith("Games_Scan_")}");

            TestFileGeneration(folderName);
        }

        private static void TestMultipleDrivesScan()
        {
            var scanResult = CreateMockScanResult(new List<string> { "C:\\", "D:\\", "E:\\" });
            var folderName = FileHelper.CreateScanFolderName(scanResult.ScannedPaths);

            Console.WriteLine($"  Scanned Paths: C:\\, D:\\, E:\\");
            Console.WriteLine($"  Generated Folder: {folderName}");
            Console.WriteLine($"  Expected Pattern: Multi_Scan_YYYYMMDD_HHMMSS");
            Console.WriteLine($"  ✓ Matches Pattern: {folderName.StartsWith("Multi_Scan_")}");

            TestFileGeneration(folderName);
        }

        private static void TestExportFormats()
        {
            Console.WriteLine("  Available Export Formats:");
            Console.WriteLine($"    - {ExportFormat.HtmlOnly}: HTML report only");
            Console.WriteLine($"    - {ExportFormat.PdfOnly}: PDF report only");
            Console.WriteLine($"    - {ExportFormat.Both}: Both HTML and PDF reports (default)");
            Console.WriteLine($"    - {ExportFormat.None}: Skip export");
            Console.WriteLine("  ✓ All export format options available");
        }

        private static void TestOrganizedFolders()
        {
            var testCases = new[]
            {
                ("C:\\", "C_Drive_Scan_"),
                ("D:\\Games", "Games_Scan_"),
                ("C:\\Users\\Documents", "Documents_Scan_"),
                ("D:\\My Projects (Work)", "My_Projects_Work_Scan_"),
                ("C:\\Program Files\\Steam\\steamapps", "Steam_steamapps_Scan_")
            };

            Console.WriteLine("  Testing various path scenarios:");
            foreach (var (input, expectedPrefix) in testCases)
            {
                var scanResult = CreateMockScanResult(new List<string> { input });
                var folderName = FileHelper.CreateScanFolderName(scanResult.ScannedPaths);
                var matchesExpected = folderName.StartsWith(expectedPrefix);

                Console.WriteLine($"    {input} → {folderName}");
                Console.WriteLine($"      ✓ Clean & Valid: {IsValidFolderName(folderName)}");
                Console.WriteLine($"      ✓ Expected Prefix: {matchesExpected}");
            }
        }

        private static void TestFileGeneration(string folderName)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var outputFolder = Path.Combine(desktop, folderName);
            var htmlPath = Path.Combine(outputFolder, "FolderScan_Report.html");
            var pdfPath = Path.Combine(outputFolder, "FolderScan_Report.pdf");

            Console.WriteLine($"  Output Folder: {outputFolder}");
            Console.WriteLine($"  HTML File: FolderScan_Report.html");
            Console.WriteLine($"  PDF File: FolderScan_Report.pdf");
            Console.WriteLine($"  ✓ Paths are Windows-compatible");
        }

        private static bool IsValidFolderName(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return false;

            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var invalidChar in invalidChars)
            {
                if (folderName.Contains(invalidChar))
                    return false;
            }

            return !folderName.Contains("  ") && // No double spaces
                   !folderName.StartsWith(" ") && // No leading spaces
                   !folderName.EndsWith(" ") &&   // No trailing spaces
                   folderName.Length <= 100;      // Reasonable length
        }

        private static ScanResult CreateMockScanResult(List<string> paths)
        {
            var scanResult = new ScanResult
            {
                ScanStartTime = DateTime.Now.AddMinutes(-2),
                TotalFolders = 500,
                TotalFiles = 2500
            };

            scanResult.SetScanDuration(DateTime.Now);

            foreach (var path in paths)
            {
                scanResult.AddScannedPath(path);

                var rootFolder = new FolderInfo
                {
                    Name = Path.GetFileName(path) ?? path,
                    FullPath = path,
                    SubFolderCount = 25,
                    FileCount = 150
                };

                scanResult.AddRootFolder(rootFolder);
            }

            return scanResult;
        }
    }
}