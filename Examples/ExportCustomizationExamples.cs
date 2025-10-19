using System;
using System.Threading.Tasks;
using FolderVision.Core;
using FolderVision.Models;
using FolderVision.Exporters;
using FolderVision.Utils;

namespace FolderVision.Examples
{
    /// <summary>
    /// Examples demonstrating the new export customization features (Problems 10-12)
    /// </summary>
    public static class ExportCustomizationExamples
    {
        /// <summary>
        /// Example 1: File size formatting with different locales and units
        /// </summary>
        public static void FileSizeFormattingExamples()
        {
            Console.WriteLine("=== File Size Formatting Examples ===\n");

            long fileSize = 1536000; // 1.5 MB

            // Default formatting (English, binary)
            var defaultFormat = FileSizeFormatter.FormatDefault(fileSize);
            Console.WriteLine($"Default (EN, Binary):  {defaultFormat}");

            // French formatting
            var frenchFormat = FileSizeFormatter.FormatFrench(fileSize);
            Console.WriteLine($"French (FR, Binary):   {frenchFormat}");

            // Decimal/SI units
            var decimalFormat = FileSizeFormatter.FormatDecimal(fileSize);
            Console.WriteLine($"Decimal (EN, SI):      {decimalFormat}");

            // Custom formatting
            var customOptions = new FileSizeFormattingOptions
            {
                Locale = FileSizeLocale.French,
                UnitSystem = FileSizeUnitSystem.Decimal,
                DecimalPlaces = 1,
                IncludeSpace = false
            };
            var customFormat = FileSizeFormatter.Format(fileSize, customOptions);
            Console.WriteLine($"Custom (FR, SI, 1dp):  {customFormat}");

            Console.WriteLine();
        }

        /// <summary>
        /// Example 2: HTML export with French localization
        /// </summary>
        public static async Task HtmlExportFrenchExample(string folderPath)
        {
            Console.WriteLine("=== HTML Export - French Localization ===\n");

            // Scan folder
            var settings = ScanSettings.CreateDefault();
            var engine = new ScanEngine();
            var scanResult = await engine.ScanFolderAsync(folderPath, settings);

            // Export with French options
            var htmlExporter = new HtmlExporter(HtmlExportOptions.French);
            await htmlExporter.ExportAsync(scanResult, "scan_report_fr.html");

            Console.WriteLine("French HTML report generated: scan_report_fr.html");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 3: HTML export with custom color scheme
        /// </summary>
        public static async Task HtmlExportCustomColorsExample(string folderPath)
        {
            Console.WriteLine("=== HTML Export - Custom Colors ===\n");

            var settings = ScanSettings.CreateDefault();
            var engine = new ScanEngine();
            var scanResult = await engine.ScanFolderAsync(folderPath, settings);

            // Green color scheme
            var greenOptions = new HtmlExportOptions
            {
                ColorScheme = ColorScheme.Green,
                CustomTitle = "Folder Analysis - Project Green",
                UseEmojis = true,
                MaxTreeDepth = 5
            };

            var htmlExporter = new HtmlExporter(greenOptions);
            await htmlExporter.ExportAsync(scanResult, "scan_report_green.html");

            Console.WriteLine("Green-themed HTML report generated: scan_report_green.html");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 4: Compact HTML export (minimal, no emojis)
        /// </summary>
        public static async Task HtmlExportCompactExample(string folderPath)
        {
            Console.WriteLine("=== HTML Export - Compact Format ===\n");

            var settings = ScanSettings.CreateDefault();
            var engine = new ScanEngine();
            var scanResult = await engine.ScanFolderAsync(folderPath, settings);

            // Use predefined compact options
            var htmlExporter = new HtmlExporter(HtmlExportOptions.Compact);
            await htmlExporter.ExportAsync(scanResult, "scan_report_compact.html");

            Console.WriteLine("Compact HTML report generated: scan_report_compact.html");
            Console.WriteLine("Features: No emojis, max depth 3, collapsed by default");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 5: PDF export with custom options
        /// </summary>
        public static async Task PdfExportCustomExample(string folderPath)
        {
            Console.WriteLine("=== PDF Export - Custom Configuration ===\n");

            var settings = ScanSettings.CreateDefault();
            var engine = new ScanEngine();
            var scanResult = await engine.ScanFolderAsync(folderPath, settings);

            // Custom PDF options
            var pdfOptions = new PdfExportOptions
            {
                ColorScheme = ColorScheme.Blue,
                CustomTitle = "Detailed Folder Analysis",
                UseEmojis = false,
                MaxTreeDepth = 8,
                IncludeTableOfContents = true,
                IncludePageNumbers = true,
                FontSize = 11,
                FileSizeFormat = FileSizeFormattingOptions.Default
            };

            var pdfExporter = new PdfExporter(pdfOptions);
            await pdfExporter.ExportAsync(scanResult, "scan_report_custom.pdf");

            Console.WriteLine("Custom PDF report generated: scan_report_custom.pdf");
            Console.WriteLine("Features: Blue theme, no emojis, depth 8, TOC included");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 6: Scanning large folders with optimized settings
        /// </summary>
        public static async Task LargeFolderScanExample(string largeFolderPath)
        {
            Console.WriteLine("=== Large Folder Scan - Optimized Settings ===\n");

            // Use optimized settings for large folders
            var settings = ScanSettings.CreateForLargeFolders();

            Console.WriteLine("Settings for large folders:");
            Console.WriteLine($"  Max Threads: {settings.MaxThreads}");
            Console.WriteLine($"  Max Depth: {settings.MaxDepth}");
            Console.WriteLine($"  Memory Limit: {settings.MaxMemoryUsageMB} MB");
            Console.WriteLine($"  Adaptive Batching: {settings.EnableAdaptiveBatching}");
            Console.WriteLine($"  Batch Size: {settings.MaxDirectoriesPerBatch}");
            Console.WriteLine();

            var engine = new ScanEngine();

            // Monitor progress
            engine.ProgressChanged += (sender, args) =>
            {
                if (args.PercentComplete % 10 == 0)
                {
                    Console.WriteLine($"Progress: {args.PercentComplete}% - {args.CurrentPath}");
                }
            };

            var scanResult = await engine.ScanFolderAsync(largeFolderPath, settings);

            Console.WriteLine($"\nScan complete:");
            Console.WriteLine($"  Total Folders: {scanResult.TotalFolders:N0}");
            Console.WriteLine($"  Total Files: {scanResult.TotalFiles:N0}");
            Console.WriteLine($"  Duration: {scanResult.ScanDuration.TotalSeconds:F2}s");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 7: Full customization - Enterprise report
        /// </summary>
        public static async Task EnterpriseReportExample(string folderPath)
        {
            Console.WriteLine("=== Enterprise Report - Full Customization ===\n");

            // Custom scan settings
            var scanSettings = new ScanSettings
            {
                SkipSystemFolders = true,
                SkipHiddenFolders = true,
                MaxThreads = 8,
                MaxDepth = 50,
                EnableAdaptiveBatching = true
            };

            var engine = new ScanEngine();
            var scanResult = await engine.ScanFolderAsync(folderPath, scanSettings);

            // HTML Export - Professional theme
            var htmlOptions = new HtmlExportOptions
            {
                ColorScheme = ColorScheme.Monochrome,
                CustomTitle = "Enterprise IT Asset Report - Q4 2025",
                UseEmojis = false,
                IncludeFolderTree = true,
                IncludeStatistics = true,
                IncludeHeader = true,
                MaxTreeDepth = 10,
                CollapseByDefault = false,
                FileSizeFormat = new FileSizeFormattingOptions
                {
                    Locale = FileSizeLocale.English,
                    UnitSystem = FileSizeUnitSystem.Binary,
                    DecimalPlaces = 2
                }
            };

            var htmlExporter = new HtmlExporter(htmlOptions);
            await htmlExporter.ExportAsync(scanResult, "enterprise_report.html");

            // PDF Export - Detailed
            var pdfOptions = new PdfExportOptions
            {
                ColorScheme = ColorScheme.Monochrome,
                CustomTitle = "Enterprise IT Asset Report - Q4 2025",
                UseEmojis = false,
                IncludeFolderTree = true,
                IncludeStatistics = true,
                IncludeTableOfContents = true,
                MaxTreeDepth = 7,
                FontSize = 10,
                IncludePageNumbers = true,
                FileSizeFormat = htmlOptions.FileSizeFormat
            };

            var pdfExporter = new PdfExporter(pdfOptions);
            await pdfExporter.ExportAsync(scanResult, "enterprise_report.pdf");

            Console.WriteLine("Enterprise reports generated:");
            Console.WriteLine("  - enterprise_report.html (interactive)");
            Console.WriteLine("  - enterprise_report.pdf (printable)");
            Console.WriteLine("\nFeatures:");
            Console.WriteLine("  - Professional monochrome theme");
            Console.WriteLine("  - No emojis (corporate style)");
            Console.WriteLine("  - Deep folder analysis (depth 7-10)");
            Console.WriteLine("  - Binary file size units");
            Console.WriteLine("  - Complete documentation");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 8: Comparison of different color schemes
        /// </summary>
        public static async Task ColorSchemeComparisonExample(string folderPath)
        {
            Console.WriteLine("=== Color Scheme Comparison ===\n");

            var settings = ScanSettings.CreateDefault();
            var engine = new ScanEngine();
            var scanResult = await engine.ScanFolderAsync(folderPath, settings);

            var schemes = new[]
            {
                ColorScheme.Default,
                ColorScheme.Blue,
                ColorScheme.Green,
                ColorScheme.Red,
                ColorScheme.Dark,
                ColorScheme.Monochrome
            };

            foreach (var scheme in schemes)
            {
                var options = new HtmlExportOptions
                {
                    ColorScheme = scheme,
                    CustomTitle = $"Scan Report - {scheme} Theme"
                };

                var exporter = new HtmlExporter(options);
                var filename = $"scan_report_{scheme.ToString().ToLower()}.html";
                await exporter.ExportAsync(scanResult, filename);

                Console.WriteLine($"Generated: {filename}");
            }

            Console.WriteLine("\nAll color schemes exported successfully!");
            Console.WriteLine();
        }

        /// <summary>
        /// Main demo runner
        /// </summary>
        public static Task RunAllExamples()
        {
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║  FolderVision - Export Customization Examples ║");
            Console.WriteLine("║  Problems 10-12 Solutions Demonstration       ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Example 1: File size formatting
            FileSizeFormattingExamples();

            // Note: The following examples require actual folder paths
            // Uncomment and provide paths to run them:

            /*
            string testFolder = @"C:\TestFolder";

            await HtmlExportFrenchExample(testFolder);
            await HtmlExportCustomColorsExample(testFolder);
            await HtmlExportCompactExample(testFolder);
            await PdfExportCustomExample(testFolder);
            await ColorSchemeComparisonExample(testFolder);
            await EnterpriseReportExample(testFolder);

            // For large folder testing
            string largeFolder = @"C:\Windows\System32";
            await LargeFolderScanExample(largeFolder);
            */

            Console.WriteLine("Demo complete! Check the generated files.");
            return Task.CompletedTask;
        }
    }
}
