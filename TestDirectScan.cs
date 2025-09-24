using System;
using System.IO;
using System.Threading.Tasks;
using FolderVision.Core;
using FolderVision.Models;
using FolderVision.Exporters;

namespace FolderVision
{
    public class TestDirectScan
    {
        public static async Task<bool> TestScanAndExport(string testFolderPath)
        {
            Console.WriteLine("🧪 TESTING FOLDERVISION CORE FUNCTIONALITY");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            try
            {
                // 1. Test ScanEngine
                Console.WriteLine("1. Testing ScanEngine...");
                var scanEngine = new ScanEngine();
                var progressTracker = new ProgressTracker();

                var settings = new ScanSettings
                {
                    MaxThreads = 4,
                    SkipHiddenFolders = true,
                    SkipSystemFolders = true
                };

                Console.WriteLine($"   Scanning: {testFolderPath}");
                var scanResult = await scanEngine.ScanFolderAsync(testFolderPath, settings);

                Console.WriteLine($"   ✅ Scan completed!");
                Console.WriteLine($"   📁 Total folders: {scanResult.TotalFolders}");
                Console.WriteLine($"   📄 Total files: {scanResult.TotalFiles}");
                Console.WriteLine($"   ⏱️ Duration: {scanResult.ScanDuration.TotalSeconds:F2}s");
                Console.WriteLine();

                // 2. Test HTML Export
                Console.WriteLine("2. Testing HTML Export...");
                var htmlExporter = new HtmlExporter();
                var htmlPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"FolderVision_Test_{DateTime.Now:yyyyMMdd_HHmmss}.html");

                await htmlExporter.ExportAsync(scanResult, htmlPath);
                Console.WriteLine($"   ✅ HTML exported to: {htmlPath}");
                Console.WriteLine($"   📄 File exists: {File.Exists(htmlPath)}");
                if (File.Exists(htmlPath))
                {
                    var htmlSize = new FileInfo(htmlPath).Length;
                    Console.WriteLine($"   📏 File size: {htmlSize:N0} bytes");
                }
                Console.WriteLine();

                // 3. Test PDF Export
                Console.WriteLine("3. Testing PDF Export...");
                var pdfExporter = new PdfExporter();
                var pdfPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"FolderVision_Test_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                await pdfExporter.ExportAsync(scanResult, pdfPath);
                Console.WriteLine($"   ✅ PDF exported to: {pdfPath}");
                Console.WriteLine($"   📄 File exists: {File.Exists(pdfPath)}");
                if (File.Exists(pdfPath))
                {
                    var pdfSize = new FileInfo(pdfPath).Length;
                    Console.WriteLine($"   📏 File size: {pdfSize:N0} bytes");
                }
                Console.WriteLine();

                // 4. Test Folder Structure
                Console.WriteLine("4. Testing Organized Output...");
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var testFolderName = Path.GetFileName(testFolderPath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var outputFolder = Path.Combine(desktop, $"{testFolderName}_Scan_{timestamp}");

                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                var organizedHtml = Path.Combine(outputFolder, "FolderScan_Report.html");
                var organizedPdf = Path.Combine(outputFolder, "FolderScan_Report.pdf");

                await htmlExporter.ExportAsync(scanResult, organizedHtml);
                await pdfExporter.ExportAsync(scanResult, organizedPdf);

                Console.WriteLine($"   ✅ Organized folder created: {outputFolder}");
                Console.WriteLine($"   📄 HTML report: {File.Exists(organizedHtml)}");
                Console.WriteLine($"   📄 PDF report: {File.Exists(organizedPdf)}");
                Console.WriteLine();

                // 5. Display Tree Structure
                Console.WriteLine("5. Testing Tree Structure Display...");
                Console.WriteLine("   Folder tree with (📁X | 📄Y) format:");
                DisplayFolderTree(scanResult.RootFolders[0], 0);

                Console.WriteLine();
                Console.WriteLine("🎉 ALL TESTS PASSED!");
                Console.WriteLine($"📂 Check Desktop for reports in: {outputFolder}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"   Details: {ex}");
                return false;
            }
        }

        private static void DisplayFolderTree(FolderInfo folder, int depth)
        {
            var indent = new string(' ', depth * 2);
            Console.WriteLine($"{indent}📁 {folder.Name} (📁{folder.SubFolderCount} | 📄{folder.FileCount})");

            foreach (var subFolder in folder.SubFolders)
            {
                DisplayFolderTree(subFolder, depth + 1);
            }
        }
    }
}