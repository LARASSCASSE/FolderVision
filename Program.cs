using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FolderVision.Ui;
using FolderVision.Core;
using FolderVision.Models;
using FolderVision.Exporters;

namespace FolderVision
{
    class Program
    {
        private static ConsoleUI? _consoleUI;
        private static bool _isRunning = true;

        static async Task Main(string[] args)
        {
            SetupConsole();
            Console.CancelKeyPress += OnCancelKeyPress;

            try
            {
                // Temporary test mode - uncomment to test utilities
                if (args.Length > 0 && args[0] == "--test")
                {
                    TestFileHelper.RunTests();
                    TestPdfIntegration.RunTests();
                    TestWorkflow.RunWorkflowTests();
                    return;
                }

                // Direct scan test mode
                if (args.Length > 0 && args[0] == "--scan-test")
                {
                    string testPath = args.Length > 1 ? args[1] : "./test_scan_folder";
                    var success = await TestDirectScan.TestScanAndExport(Path.GetFullPath(testPath));
                    Environment.ExitCode = success ? 0 : 1;
                    return;
                }

                await RunApplicationAsync();
            }
            catch (Exception ex)
            {
                await HandleUnexpectedError(ex);
            }
            finally
            {
                CleanupAndExit();
            }
        }

        private static void SetupConsole()
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.InputEncoding = System.Text.Encoding.UTF8;
                Console.Title = "FolderVision - Multi-Threaded Folder Scanner";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.WindowWidth = Math.Min(120, Console.LargestWindowWidth);
                    Console.WindowHeight = Math.Min(40, Console.LargestWindowHeight);
                }
            }
            catch (Exception)
            {
                // Ignore console setup errors - some environments don't support these operations
            }
        }

        private static async Task RunApplicationAsync()
        {
            _consoleUI = new ConsoleUI();
            ScanResult? lastScanResult = null;
            string? lastExportPath = null;

            ShowApplicationHeader();

            while (_isRunning)
            {
                try
                {
                    var mainMenuChoice = await _consoleUI.ShowMainMenuAndGetChoiceAsync();

                    switch (mainMenuChoice)
                    {
                        case "1":
                            var driveResult = await PerformDriveScanAsync();
                            if (driveResult != null)
                            {
                                lastScanResult = driveResult.Value.scanResult;
                                lastExportPath = driveResult.Value.exportPath;
                                await HandlePostScanOptions(lastScanResult, lastExportPath);
                            }
                            break;

                        case "2":
                            var folderResult = await PerformFolderScanAsync();
                            if (folderResult != null)
                            {
                                lastScanResult = folderResult.Value.scanResult;
                                lastExportPath = folderResult.Value.exportPath;
                                await HandlePostScanOptions(lastScanResult, lastExportPath);
                            }
                            break;

                        case "3":
                            _consoleUI.ShowSettings();
                            _consoleUI.WaitForKey();
                            break;

                        case "4":
                            if (lastScanResult != null && !string.IsNullOrEmpty(lastExportPath))
                            {
                                await ShowLastScanResults(lastScanResult, lastExportPath);
                            }
                            else
                            {
                                _consoleUI.DisplayWarning("No previous scan results available.");
                                _consoleUI.WaitForKey();
                            }
                            break;

                        case "5":
                            _consoleUI.DisplayInfo("Thank you for using FolderVision!");
                            _isRunning = false;
                            break;

                        default:
                            _consoleUI.DisplayError("Invalid choice. Please select options 1-5.");
                            await Task.Delay(1500);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    _consoleUI.DisplayWarning("Operation was cancelled by user.");
                    _consoleUI.WaitForKey();
                }
                catch (UnauthorizedAccessException ex)
                {
                    _consoleUI.DisplayError($"Access denied: {ex.Message}");
                    _consoleUI.DisplayInfo("Try running as administrator or select different folders.");
                    _consoleUI.WaitForKey();
                }
                catch (IOException ex)
                {
                    _consoleUI.DisplayError($"File system error: {ex.Message}");
                    _consoleUI.WaitForKey();
                }
                catch (Exception ex)
                {
                    _consoleUI.DisplayError($"An error occurred: {ex.Message}");
                    _consoleUI.WaitForKey();
                }
            }
        }

        private static void ShowApplicationHeader()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                             FOLDER VISION                               ║");
            Console.WriteLine("║                    Professional Multi-Threaded Scanner                  ║");
            Console.WriteLine("║                         Generate Beautiful HTML Reports                 ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Welcome! This application will help you scan and analyze folder structures");
            Console.WriteLine("with multi-threaded performance and generate professional HTML reports.");
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }

        private static async Task<(ScanResult scanResult, string exportPath)?> PerformDriveScanAsync()
        {
            try
            {
                _consoleUI!.DisplayInfo("Starting drive scan...");
                var result = await _consoleUI.PerformDriveScanAsync();

                if (result != null)
                {
                    _consoleUI.DisplayScanResult(result);

                    var exportChoice = _consoleUI.GetExportFormatChoice();
                    if (exportChoice != ExportFormat.None)
                    {
                        await _consoleUI.ExportResultsAsync(result, exportChoice);
                        var outputFolder = _consoleUI.GetOutputFolderPath(result);
                        return (result, outputFolder);
                    }
                    return (result, string.Empty);
                }
                return null;
            }
            catch (Exception ex)
            {
                _consoleUI!.DisplayError($"Drive scan failed: {ex.Message}");
                _consoleUI.WaitForKey();
                return null;
            }
        }

        private static async Task<(ScanResult scanResult, string exportPath)?> PerformFolderScanAsync()
        {
            try
            {
                _consoleUI!.DisplayInfo("Starting folder scan...");
                var result = await _consoleUI.PerformFolderScanAsync();

                if (result != null)
                {
                    _consoleUI.DisplayScanResult(result);

                    var exportChoice = _consoleUI.GetExportFormatChoice();
                    if (exportChoice != ExportFormat.None)
                    {
                        await _consoleUI.ExportResultsAsync(result, exportChoice);
                        var outputFolder = _consoleUI.GetOutputFolderPath(result);
                        return (result, outputFolder);
                    }
                    return (result, string.Empty);
                }
                return null;
            }
            catch (Exception ex)
            {
                _consoleUI!.DisplayError($"Folder scan failed: {ex.Message}");
                _consoleUI.WaitForKey();
                return null;
            }
        }


        private static async Task HandlePostScanOptions(ScanResult scanResult, string exportPath)
        {
            if (string.IsNullOrEmpty(exportPath))
            {
                _consoleUI!.WaitForKey();
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Choose what to do next:");
            Console.WriteLine("1. Open HTML report in browser");
            Console.WriteLine("2. Show report file location");
            Console.WriteLine("3. Perform another scan");
            Console.WriteLine("4. Return to main menu");
            Console.Write("Your choice (1-4): ");

            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await OpenHtmlReport(exportPath);
                    break;
                case "2":
                    ShowReportLocation(exportPath);
                    _consoleUI!.WaitForKey();
                    break;
                case "3":
                    return; // Will loop back to main menu for another scan
                case "4":
                default:
                    return; // Return to main menu
            }
        }

        private static async Task OpenHtmlReport(string filePath)
        {
            try
            {
                _consoleUI!.DisplayInfo($"Opening HTML report: {Path.GetFileName(filePath)}");

                if (!File.Exists(filePath))
                {
                    _consoleUI.DisplayError("HTML report file not found!");
                    _consoleUI.WaitForKey();
                    return;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", filePath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", filePath);
                }
                else
                {
                    _consoleUI.DisplayWarning("Unable to open file automatically on this platform.");
                    ShowReportLocation(filePath);
                    _consoleUI.WaitForKey();
                    return;
                }

                _consoleUI.DisplaySuccess("HTML report opened in your default browser!");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                _consoleUI!.DisplayError($"Failed to open HTML report: {ex.Message}");
                ShowReportLocation(filePath);
                _consoleUI.WaitForKey();
            }
        }

        private static void ShowReportLocation(string filePath)
        {
            _consoleUI!.DisplayInfo("Report saved to:");
            Console.WriteLine($"  📁 {Path.GetDirectoryName(filePath)}");
            Console.WriteLine($"  📄 {Path.GetFileName(filePath)}");
            Console.WriteLine();
            Console.WriteLine("You can manually open this file in your browser.");
        }

        private static async Task ShowLastScanResults(ScanResult scanResult, string exportPath)
        {
            _consoleUI!.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("═══ PREVIOUS SCAN RESULTS ═══");
            Console.ResetColor();

            _consoleUI.DisplayScanResult(scanResult);

            if (!string.IsNullOrEmpty(exportPath) && File.Exists(exportPath))
            {
                Console.WriteLine();
                if (_consoleUI.ConfirmAction("Open previous HTML report? (y/n): "))
                {
                    await OpenHtmlReport(exportPath);
                }
            }
            else
            {
                Console.WriteLine();
                _consoleUI.DisplayWarning("Previous HTML report is no longer available.");
            }

            _consoleUI.WaitForKey();
        }

        private static async Task HandleUnexpectedError(Exception ex)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    UNEXPECTED ERROR OCCURRED                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine($"Error Type: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("This error has been logged. If the problem persists, please contact support.");
            Console.WriteLine("Include the error details above when reporting the issue.");

            // Log error to file
            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"FolderVision_Error_{DateTime.Now:yyyyMMdd_HHmmss}.log");

                var logContent = $"FolderVision Error Log\n" +
                               $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                               $"OS: {Environment.OSVersion}\n" +
                               $"Runtime: {RuntimeInformation.FrameworkDescription}\n" +
                               $"Error Type: {ex.GetType().FullName}\n" +
                               $"Message: {ex.Message}\n" +
                               $"Stack Trace:\n{ex.StackTrace}\n";

                if (ex.InnerException != null)
                {
                    logContent += $"\nInner Exception: {ex.InnerException}\n";
                }

                await File.WriteAllTextAsync(logPath, logContent);
                Console.WriteLine($"\nError details saved to: {logPath}");
            }
            catch
            {
                Console.WriteLine("Unable to save error log to file.");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true; // Prevent immediate termination
            _isRunning = false;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    SHUTDOWN REQUESTED                         ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Gracefully shutting down FolderVision...");
            Console.WriteLine("Please wait for any ongoing operations to complete.");
        }

        private static void CleanupAndExit()
        {
            try
            {
                _consoleUI?.Cleanup();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Thank you for using FolderVision!");
                Console.WriteLine("Have a great day!");
                Console.ResetColor();

                // Give user a moment to read the message
                Task.Delay(1500).Wait();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}