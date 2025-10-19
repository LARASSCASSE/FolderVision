using System;
using System.Threading.Tasks;
using FolderVision.Core;
using FolderVision.Core.Logging;
using FolderVision.Models;

namespace FolderVision.Examples
{
    /// <summary>
    /// Examples demonstrating the structured logging system
    /// </summary>
    public static class LoggingExamples
    {
        /// <summary>
        /// Example 1: Default logging (Info level, console + file)
        /// </summary>
        public static async Task DefaultLoggingExample(string folderPath)
        {
            Console.WriteLine("=== Example 1: Default Logging ===\n");

            var settings = ScanSettings.CreateDefault();
            // Logging is already configured with LoggingOptions.Default
            // - MinLevel: Info
            // - Console: Enabled with colors
            // - File: Enabled
            // - Location: %LocalAppData%\FolderVision\Logs\

            var engine = new ScanEngine();
            var result = await engine.ScanFolderAsync(folderPath, settings);

            Console.WriteLine($"\nScan completed. Check logs at:");
            Console.WriteLine($"  %LocalAppData%\\FolderVision\\Logs\\FolderVision_*.log");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 2: Verbose logging for debugging (Debug level)
        /// </summary>
        public static async Task VerboseLoggingExample(string folderPath)
        {
            Console.WriteLine("=== Example 2: Verbose Logging (Debug) ===\n");

            var settings = new ScanSettings
            {
                LoggingOptions = LoggingOptions.Verbose
                // - MinLevel: Debug (all messages)
                // - Console: Enabled
                // - File: Enabled
                // - Format: Structured (JSON)
            };

            var engine = new ScanEngine();
            var result = await engine.ScanFolderAsync(folderPath, settings);

            Console.WriteLine($"\nVerbose scan completed with {result.TotalFolders} folders.");
            Console.WriteLine("Check logs for detailed debug information.");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 3: Quiet logging (Warning and above only, file only)
        /// </summary>
        public static async Task QuietLoggingExample(string folderPath)
        {
            Console.WriteLine("=== Example 3: Quiet Logging (Warnings only) ===\n");

            var settings = new ScanSettings
            {
                LoggingOptions = LoggingOptions.Quiet
                // - MinLevel: Warning (only warnings, errors, critical)
                // - Console: Disabled
                // - File: Enabled
            };

            var engine = new ScanEngine();
            var result = await engine.ScanFolderAsync(folderPath, settings);

            Console.WriteLine($"\nQuiet scan completed.");
            Console.WriteLine("Only warnings and errors were logged to file.");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 4: Console only (no file logging)
        /// </summary>
        public static async Task ConsoleOnlyExample(string folderPath)
        {
            Console.WriteLine("=== Example 4: Console Only Logging ===\n");

            var settings = new ScanSettings
            {
                LoggingOptions = LoggingOptions.ConsoleOnly
                // - MinLevel: Info
                // - Console: Enabled with colors
                // - File: Disabled
            };

            var engine = new ScanEngine();
            var result = await engine.ScanFolderAsync(folderPath, settings);

            Console.WriteLine($"\nConsole-only scan completed.");
            Console.WriteLine("No log files were created.");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 5: File only (no console output, good for background tasks)
        /// </summary>
        public static async Task FileOnlyExample(string folderPath)
        {
            Console.WriteLine("=== Example 5: File Only Logging ===\n");

            var settings = new ScanSettings
            {
                LoggingOptions = LoggingOptions.FileOnly
                // - MinLevel: Info
                // - Console: Disabled
                // - File: Enabled
            };

            var engine = new ScanEngine();
            var result = await engine.ScanFolderAsync(folderPath, settings);

            Console.WriteLine($"\nFile-only scan completed silently.");
            Console.WriteLine("All logs were written to file only.");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 6: Custom logging configuration
        /// </summary>
        public static async Task CustomLoggingExample(string folderPath)
        {
            Console.WriteLine("=== Example 6: Custom Logging Configuration ===\n");

            var settings = new ScanSettings
            {
                LoggingOptions = new LoggingOptions
                {
                    MinLevel = LogLevel.Debug,
                    EnableConsoleLog = true,
                    EnableFileLog = true,
                    UseConsoleColors = true,
                    UseStructuredFormat = false,
                    LogDirectory = @"C:\MyCustomLogs",
                    MaxLogFileSizeMB = 20,
                    MaxLogFileCount = 10,
                    LogFilePrefix = "MyApp"
                }
            };

            Console.WriteLine("Custom configuration:");
            Console.WriteLine($"  Min Level: {settings.LoggingOptions.MinLevel}");
            Console.WriteLine($"  Console: {settings.LoggingOptions.EnableConsoleLog}");
            Console.WriteLine($"  File: {settings.LoggingOptions.EnableFileLog}");
            Console.WriteLine($"  Log Directory: {settings.LoggingOptions.LogDirectory}");
            Console.WriteLine($"  Max File Size: {settings.LoggingOptions.MaxLogFileSizeMB} MB");
            Console.WriteLine($"  Max File Count: {settings.LoggingOptions.MaxLogFileCount}");
            Console.WriteLine();

            var engine = new ScanEngine();
            var result = await engine.ScanFolderAsync(folderPath, settings);

            Console.WriteLine($"\nCustom scan completed.");
            Console.WriteLine($"Logs saved to: {settings.LoggingOptions.LogDirectory}");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 7: Structured (JSON) logging for parsing
        /// </summary>
        public static async Task StructuredLoggingExample(string folderPath)
        {
            Console.WriteLine("=== Example 7: Structured (JSON) Logging ===\n");

            var settings = new ScanSettings
            {
                LoggingOptions = new LoggingOptions
                {
                    MinLevel = LogLevel.Info,
                    EnableConsoleLog = true,
                    EnableFileLog = true,
                    UseStructuredFormat = true // JSON format
                }
            };

            Console.WriteLine("Structured logging enabled (JSON format).");
            Console.WriteLine("Logs will be in JSON for easy parsing and analysis.");
            Console.WriteLine();

            var engine = new ScanEngine();
            var result = await engine.ScanFolderAsync(folderPath, settings);

            Console.WriteLine($"\nStructured scan completed.");
            Console.WriteLine("Logs are in JSON format, ready for parsing tools.");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 8: Large folder scan with verbose logging
        /// </summary>
        public static async Task LargeFolderVerboseExample(string largeFolderPath)
        {
            Console.WriteLine("=== Example 8: Large Folder with Verbose Logging ===\n");

            var settings = ScanSettings.CreateForLargeFolders();
            // Automatically configured with LoggingOptions.Verbose for detailed tracking
            // - Debug level for all batch processing details
            // - Structured format for easy analysis
            // - Progress logged every 10 batches

            Console.WriteLine("Large folder settings with verbose logging:");
            Console.WriteLine($"  Min Level: {settings.LoggingOptions.MinLevel}");
            Console.WriteLine($"  Structured Format: {settings.LoggingOptions.UseStructuredFormat}");
            Console.WriteLine();

            var engine = new ScanEngine();
            var result = await engine.ScanFolderAsync(largeFolderPath, settings);

            Console.WriteLine($"\nLarge folder scan completed:");
            Console.WriteLine($"  Folders: {result.TotalFolders:N0}");
            Console.WriteLine($"  Files: {result.TotalFiles:N0}");
            Console.WriteLine($"  Duration: {result.ScanDuration.TotalSeconds:F2}s");
            Console.WriteLine("\nCheck logs for detailed batch processing information.");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 9: Disabled logging (for performance testing)
        /// </summary>
        public static async Task DisabledLoggingExample(string folderPath)
        {
            Console.WriteLine("=== Example 9: Logging Disabled ===\n");

            var settings = new ScanSettings
            {
                LoggingOptions = LoggingOptions.Disabled
                // - MinLevel: None
                // - Console: Disabled
                // - File: Disabled
            };

            Console.WriteLine("Logging is completely disabled.");
            Console.WriteLine("No logs will be generated (for performance benchmarks).");
            Console.WriteLine();

            var engine = new ScanEngine();
            var result = await engine.ScanFolderAsync(folderPath, settings);

            Console.WriteLine($"\nScan completed without logging:");
            Console.WriteLine($"  Folders: {result.TotalFolders:N0}");
            Console.WriteLine($"  Files: {result.TotalFiles:N0}");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 10: Comparison of different log levels
        /// </summary>
        public static async Task LogLevelComparisonExample(string folderPath)
        {
            Console.WriteLine("=== Example 10: Log Level Comparison ===\n");

            var levels = new[] { LogLevel.Debug, LogLevel.Info, LogLevel.Warning, LogLevel.Error };

            foreach (var level in levels)
            {
                Console.WriteLine($"\n--- Testing {level} level ---");

                var settings = new ScanSettings
                {
                    LoggingOptions = new LoggingOptions
                    {
                        MinLevel = level,
                        EnableConsoleLog = true,
                        EnableFileLog = false
                    }
                };

                var engine = new ScanEngine();
                var result = await engine.ScanFolderAsync(folderPath, settings);

                Console.WriteLine($"Scan with {level} completed: {result.TotalFolders} folders");
            }

            Console.WriteLine("\n\nLog level comparison complete.");
            Console.WriteLine("Notice how higher levels show fewer messages.");
            Console.WriteLine();
        }

        /// <summary>
        /// Main demo runner
        /// </summary>
        public static Task RunAllExamples()
        {
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║  FolderVision - Logging System Examples       ║");
            Console.WriteLine("║  Structured Logging Demonstrations            ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Note: Uncomment and provide actual paths to run examples

            /*
            string testFolder = @"C:\TestFolder";
            string largeFolder = @"C:\Windows\System32";

            await DefaultLoggingExample(testFolder);
            await VerboseLoggingExample(testFolder);
            await QuietLoggingExample(testFolder);
            await ConsoleOnlyExample(testFolder);
            await FileOnlyExample(testFolder);
            await CustomLoggingExample(testFolder);
            await StructuredLoggingExample(testFolder);
            await LargeFolderVerboseExample(largeFolder);
            await DisabledLoggingExample(testFolder);
            await LogLevelComparisonExample(testFolder);
            */

            Console.WriteLine("To run examples, uncomment the code and provide folder paths.");
            Console.WriteLine("\nLogging system is ready to use!");
            return Task.CompletedTask;
        }
    }
}
