using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FolderVision.Core;
using FolderVision.Models;
using FolderVision.Exporters;
using FolderVision.Utils;

namespace FolderVision.Ui
{
    public enum ExportFormat
    {
        None,
        HtmlOnly,
        PdfOnly,
        Both
    }

    public class ConsoleUI
    {
        private readonly ProgressDisplay _progressDisplay;
        private ThreadManager? _threadManager;
        private ProgressTracker? _progressTracker;
        private static ScanSettings _defaultSettings = ScanSettings.CreateDefault();

        public ConsoleUI()
        {
            _progressDisplay = new ProgressDisplay();
        }

        public async Task RunAsync()
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.InputEncoding = System.Text.Encoding.UTF8;

                ShowWelcomeMessage();

                while (true)
                {
                    ShowMainMenu();
                    var choice = GetUserChoice();

                    switch (choice)
                    {
                        case "1":
                            var driveResult = await PerformDriveScanAsync();
                            if (driveResult != null)
                            {
                                var exportChoice = GetExportFormatChoice();
                                if (exportChoice != ExportFormat.None)
                                {
                                    await ExportResultsAsync(driveResult, exportChoice);
                                }
                            }
                            break;
                        case "2":
                            var folderResult = await PerformFolderScanAsync();
                            if (folderResult != null)
                            {
                                var exportChoice = GetExportFormatChoice();
                                if (exportChoice != ExportFormat.None)
                                {
                                    await ExportResultsAsync(folderResult, exportChoice);
                                }
                            }
                            break;
                        case "3":
                            ShowSettings();
                            break;
                        case "4":
                            DisplayInfo("Previous scan results feature not yet implemented.");
                            break;
                        case "5":
                            DisplayInfo("Goodbye!");
                            return;
                        default:
                            DisplayError("Invalid choice. Please try again.");
                            break;
                    }

                    if (choice != "5")
                    {
                        DisplayInfo("Press any key to continue...");
                        Console.ReadKey();
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayError($"An unexpected error occurred: {ex.Message}");
                WaitForKey();
            }
        }

        public void ShowWelcomeMessage()
        {
            Clear();
            SetConsoleColor(ConsoleColor.Cyan);
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        FOLDER VISION                        ║");
            Console.WriteLine("║                  Multi-Threaded Folder Scanner              ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            ResetConsoleColor();
            Console.WriteLine();
        }

        public void ShowMainMenu()
        {
            Clear();
            ShowWelcomeMessage();

            SetConsoleColor(ConsoleColor.Yellow);
            Console.WriteLine("═══ MAIN MENU ═══");
            ResetConsoleColor();
            Console.WriteLine();
            Console.WriteLine($"1. {PlatformHelper.Terminology.ScanDrivesMenuTitle}");
            Console.WriteLine("2. Scan Custom Folders");
            Console.WriteLine("3. Settings");
            Console.WriteLine("4. View Previous Scan Results");
            Console.WriteLine("5. Exit");
            Console.WriteLine();
            Console.Write("Select an option (1-5): ");
        }

        public async Task<string> ShowMainMenuAndGetChoiceAsync()
        {
            ShowMainMenu();
            return await Task.FromResult(GetUserChoice());
        }

        public async Task<ScanResult?> PerformDriveScanAsync()
        {
            Clear();
            SetConsoleColor(ConsoleColor.Green);
            Console.WriteLine($"═══ {PlatformHelper.Terminology.ScanDrivesMenuTitle.ToUpper().Replace("SCAN ", "")} SCANNER ═══");
            ResetConsoleColor();
            Console.WriteLine();

            var drives = GetAvailableDrives();
            if (!drives.Any())
            {
                DisplayError("No drives found!");
                return null;
            }

            DisplayDrives(drives);
            var selectedDrives = GetSelectedDrives(drives);

            if (!selectedDrives.Any())
            {
                DisplayWarning("No drives selected.");
                return null;
            }

            var settings = GetScanSettings();
            var drivePaths = selectedDrives.Select(d => d.RootDirectory.FullName).ToList();

            return await PerformScanAsync(drivePaths, settings);
        }

        public async Task<ScanResult?> PerformFolderScanAsync()
        {
            Clear();
            SetConsoleColor(ConsoleColor.Green);
            Console.WriteLine("═══ FOLDER SCANNER ═══");
            ResetConsoleColor();
            Console.WriteLine();

            var folders = GetCustomFolders();
            if (!folders.Any())
            {
                DisplayWarning("No folders selected.");
                return null;
            }

            var settings = GetScanSettings();
            return await PerformScanAsync(folders, settings);
        }


        private async Task<ScanResult?> PerformScanAsync(List<string> paths, ScanSettings settings)
        {
            _threadManager = new ThreadManager(settings.MaxThreads);
            _progressTracker = new ProgressTracker();

            try
            {
                // Subscribe to progress updates
                _progressTracker.ProgressChanged += (sender, e) =>
                {
                    _progressDisplay.UpdateProgress(e, paths);
                };

                DisplayInfo($"Starting scan of {paths.Count} path(s) with {settings.MaxThreads} threads...");
                Console.WriteLine($"Paths: {string.Join(", ", paths)}");
                Console.WriteLine($"Settings: MaxDepth={settings.MaxDepth}, SkipHidden={settings.SkipHiddenFolders}, SkipSystem={settings.SkipSystemFolders}");
                Console.WriteLine("Press ESC to cancel the scan.");
                Console.WriteLine();

                // Start scan
                var scanTask = _threadManager.ScanMultiplePathsAsync(paths, settings, _progressTracker);

                // Monitor for ESC key
                var keyTask = Task.Run(() =>
                {
                    while (!scanTask.IsCompleted)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true);
                            if (key.Key == ConsoleKey.Escape)
                            {
                                DisplayWarning("Canceling scan...");
                                _threadManager.CancelAll();
                                break;
                            }
                        }
                        System.Threading.Thread.Sleep(100);
                    }
                });

                var result = await scanTask;

                _progressDisplay.CompleteScan();

                // Validate that we actually got data from the scan
                if (result == null || result.TotalFolders == 0 && result.TotalFiles == 0)
                {
                    DisplayError("Scan completed but no data was collected.");
                    DisplayError("This may indicate:");
                    DisplayError("  • Permissions issues accessing the path");
                    DisplayError("  • MaxDepth limit was reached too early");
                    DisplayError("  • All folders were skipped due to settings");
                    DisplayError("  • Path is empty or inaccessible");

                    // Check for specific errors
                    var scanEngine = new ScanEngine();
                    if (_threadManager != null)
                    {
                        var threadInfos = _threadManager.GetThreadInfos();
                        foreach (var threadInfo in threadInfos.Values)
                        {
                            if (!string.IsNullOrEmpty(threadInfo.Error))
                            {
                                DisplayError($"  • Thread {threadInfo.ThreadId} error: {threadInfo.Error}");
                            }
                        }
                    }

                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    return null;
                }

                return result;
            }
            catch (Exception ex)
            {
                DisplayError($"Scan failed: {ex.Message}");
                DisplayError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    DisplayError($"Inner exception: {ex.InnerException.Message}");
                }
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return null;
            }
            finally
            {
                _threadManager?.Dispose();
            }
        }

        private List<DriveInfo> GetAvailableDrives()
        {
            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
                var validDrives = new List<DriveInfo>();

                foreach (var drive in drives)
                {
                    try
                    {
                        // Test if we can access the drive
                        var testAccess = drive.RootDirectory.Exists;
                        var driveSize = drive.TotalSize; // This will throw if drive is not accessible

                        // Include all drive types but warn about network drives
                        validDrives.Add(drive);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        DisplayWarning($"Access denied to drive {drive.Name} - skipping");
                    }
                    catch (IOException)
                    {
                        DisplayWarning($"I/O error accessing drive {drive.Name} - skipping");
                    }
                    catch (Exception ex)
                    {
                        DisplayWarning($"Error accessing drive {drive.Name}: {ex.Message} - skipping");
                    }
                }

                return validDrives;
            }
            catch (Exception ex)
            {
                DisplayError($"Error getting drives: {ex.Message}");
                return new List<DriveInfo>();
            }
        }

        private void DisplayDrives(List<DriveInfo> drives)
        {
            Console.WriteLine(PlatformHelper.Terminology.AvailableStorageTitle);
            Console.WriteLine("═════════════════");

            for (int i = 0; i < drives.Count; i++)
            {
                var drive = drives[i];
                try
                {
                    var totalSize = FormatFileSize(drive.TotalSize);
                    var freeSpace = FormatFileSize(drive.AvailableFreeSpace);
                    var usedSpace = FormatFileSize(drive.TotalSize - drive.AvailableFreeSpace);
                    var percentUsed = drive.TotalSize > 0 ? (double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100 : 0;

                    var driveTypeDescription = PlatformHelper.GetDriveTypeDescription(drive.DriveType);
                    var driveSymbol = drive.DriveType switch
                    {
                        DriveType.Network => PlatformHelper.Symbols.NetworkSymbol,
                        DriveType.CDRom => PlatformHelper.Symbols.OpticalSymbol,
                        DriveType.Removable => PlatformHelper.Symbols.RemovableSymbol,
                        DriveType.Fixed => PlatformHelper.Symbols.DriveSymbol,
                        _ => PlatformHelper.Symbols.SystemSymbol
                    };

                    Console.WriteLine($"{i + 1,2}. {drive.Name,-4} [{driveTypeDescription,-15}] {driveSymbol} {drive.VolumeLabel}");

                    if (drive.TotalSize == 0)
                    {
                        SetConsoleColor(ConsoleColor.Yellow);
                        Console.WriteLine($"     ⚠️ Empty or inaccessible drive");
                        ResetConsoleColor();
                    }
                    else
                    {
                        Console.WriteLine($"     Total: {totalSize,-10} Used: {usedSpace,-10} Free: {freeSpace,-10} ({percentUsed:F1}% used)");

                        if (drive.DriveType == DriveType.Network)
                        {
                            SetConsoleColor(ConsoleColor.Yellow);
                            Console.WriteLine($"     ⚠️ {PlatformHelper.Terminology.NetworkStorageTerm} - scanning may be slow");
                            ResetConsoleColor();
                        }
                    }
                }
                catch (Exception ex)
                {
                    SetConsoleColor(ConsoleColor.Red);
                    Console.WriteLine($"{i + 1,2}. {drive.Name,-4} [{drive.DriveType,-10}] ❌ Error: {ex.Message}");
                    ResetConsoleColor();
                }
                Console.WriteLine();
            }

            if (drives.Any(d => d.DriveType == DriveType.Network))
            {
                SetConsoleColor(ConsoleColor.Yellow);
                Console.WriteLine($"💡 Tip: {PlatformHelper.Terminology.NetworkStorageTerm}s may scan slowly. Consider using specific {PlatformHelper.Terminology.FolderOrDirectoryTerm} paths instead.");
                ResetConsoleColor();
                Console.WriteLine();
            }
        }

        private List<DriveInfo> GetSelectedDrives(List<DriveInfo> drives)
        {
            Console.WriteLine($"Enter {PlatformHelper.Terminology.DriveOrVolumeTerm} numbers to scan (e.g., 1,3,4) or 'all' for all {PlatformHelper.Terminology.DriveOrVolumeTerm}s:");
            Console.Write("Selection: ");

            var input = Console.ReadLine()?.Trim().ToLower() ?? "";
            var selectedDrives = new List<DriveInfo>();

            if (input == "all")
            {
                return drives;
            }

            var selections = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var selection in selections)
            {
                if (int.TryParse(selection.Trim(), out int index) && index >= 1 && index <= drives.Count)
                {
                    selectedDrives.Add(drives[index - 1]);
                }
            }

            return selectedDrives;
        }

        private List<string> GetCustomFolders()
        {
            var folders = new List<string>();

            Console.WriteLine($"Enter {PlatformHelper.Terminology.FolderOrDirectoryTerm} paths to scan (one per line, empty line to finish):");
            Console.WriteLine("Examples:");
            var platformExamples = PlatformHelper.PathExamples.GetCommonPaths();
            foreach (var example in platformExamples)
            {
                Console.WriteLine($"  {example}");
            }
            Console.WriteLine();

            while (true)
            {
                Console.Write($"{char.ToUpper(PlatformHelper.Terminology.FolderOrDirectoryTerm[0])}{PlatformHelper.Terminology.FolderOrDirectoryTerm.Substring(1)} {folders.Count + 1}: ");
                var input = Console.ReadLine()?.Trim() ?? "";

                if (string.IsNullOrEmpty(input))
                    break;

                if (Directory.Exists(input))
                {
                    folders.Add(input);
                    SetConsoleColor(ConsoleColor.Green);
                    Console.WriteLine($"  ✓ Added: {input}");
                    ResetConsoleColor();
                }
                else
                {
                    SetConsoleColor(ConsoleColor.Red);
                    Console.WriteLine($"  ✗ Directory not found: {input}");
                    ResetConsoleColor();
                }
            }

            return folders;
        }

        public ScanSettings GetScanSettings()
        {
            var settings = _defaultSettings.Clone();

            Console.WriteLine();
            SetConsoleColor(ConsoleColor.Yellow);
            Console.WriteLine("═══ SCAN SETTINGS ═══");
            ResetConsoleColor();

            // Max threads
            Console.Write($"Max concurrent threads [{settings.MaxThreads}]: ");
            var threadsInput = Console.ReadLine()?.Trim();
            if (int.TryParse(threadsInput, out int maxThreads) && maxThreads > 0)
            {
                settings.MaxThreads = maxThreads;
            }

            // Skip hidden folders
            Console.Write($"Skip hidden folders? (y/n) [{(settings.SkipHiddenFolders ? "y" : "n")}]: ");
            var hiddenInput = Console.ReadLine()?.Trim().ToLower();
            if (hiddenInput == "y" || hiddenInput == "yes")
                settings.SkipHiddenFolders = true;
            else if (hiddenInput == "n" || hiddenInput == "no")
                settings.SkipHiddenFolders = false;

            // Skip system folders
            Console.Write($"Skip system folders? (y/n) [{(settings.SkipSystemFolders ? "y" : "n")}]: ");
            var systemInput = Console.ReadLine()?.Trim().ToLower();
            if (systemInput == "y" || systemInput == "yes")
                settings.SkipSystemFolders = true;
            else if (systemInput == "n" || systemInput == "no")
                settings.SkipSystemFolders = false;

            // Max depth
            Console.Write($"Maximum scan depth [{settings.MaxDepth}]: ");
            var depthInput = Console.ReadLine()?.Trim();
            if (int.TryParse(depthInput, out int maxDepth) && maxDepth > 0)
            {
                settings.MaxDepth = maxDepth;
            }

            // Memory optimization
            Console.Write($"Enable memory optimization? (y/n) [{(settings.EnableMemoryOptimization ? "y" : "n")}]: ");
            var memOptInput = Console.ReadLine()?.Trim().ToLower();
            if (memOptInput == "y" || memOptInput == "yes")
                settings.EnableMemoryOptimization = true;
            else if (memOptInput == "n" || memOptInput == "no")
                settings.EnableMemoryOptimization = false;

            // Max memory usage (only if optimization is enabled)
            if (settings.EnableMemoryOptimization)
            {
                Console.Write($"Maximum memory usage (MB) [{settings.MaxMemoryUsageMB}]: ");
                var memInput = Console.ReadLine()?.Trim();
                if (long.TryParse(memInput, out long maxMemory) && maxMemory > 0)
                {
                    settings.MaxMemoryUsageMB = maxMemory;
                }
            }

            Console.WriteLine();
            SetConsoleColor(ConsoleColor.Yellow);
            Console.WriteLine("═══ TIMEOUT SETTINGS ═══");
            ResetConsoleColor();

            // Global timeout
            Console.Write($"Global scan timeout (minutes) [{settings.GlobalTimeout.TotalMinutes}]: ");
            var globalTimeoutInput = Console.ReadLine()?.Trim();
            if (int.TryParse(globalTimeoutInput, out int globalMinutes) && globalMinutes > 0)
            {
                settings.GlobalTimeout = TimeSpan.FromMinutes(globalMinutes);
            }

            // Directory timeout
            Console.Write($"Directory timeout (seconds) [{settings.DirectoryTimeout.TotalSeconds}]: ");
            var dirTimeoutInput = Console.ReadLine()?.Trim();
            if (int.TryParse(dirTimeoutInput, out int dirSeconds) && dirSeconds > 0)
            {
                settings.DirectoryTimeout = TimeSpan.FromSeconds(dirSeconds);
            }

            // Network drive timeout
            Console.Write($"Network drive timeout (seconds) [{settings.NetworkDriveTimeout.TotalSeconds}]: ");
            var networkTimeoutInput = Console.ReadLine()?.Trim();
            if (int.TryParse(networkTimeoutInput, out int networkSeconds) && networkSeconds > 0)
            {
                settings.NetworkDriveTimeout = TimeSpan.FromSeconds(networkSeconds);
            }

            return settings;
        }

        public void ShowSettings()
        {
            while (true)
            {
                Clear();
                SetConsoleColor(ConsoleColor.Magenta);
                Console.WriteLine("═══ DEFAULT SETTINGS ═══");
                ResetConsoleColor();
                Console.WriteLine();
                Console.WriteLine("Current default settings (used for all new scans):");
                Console.WriteLine();
                Console.WriteLine($"• Max Threads: {_defaultSettings.MaxThreads}");
                Console.WriteLine($"• Max Depth: {_defaultSettings.MaxDepth}");
                Console.WriteLine($"• Memory Optimization: {(_defaultSettings.EnableMemoryOptimization ? "Enabled" : "Disabled")}");
                Console.WriteLine($"• Max Memory Usage: {_defaultSettings.MaxMemoryUsageMB} MB");
                Console.WriteLine($"• Global Timeout: {_defaultSettings.GlobalTimeout.TotalMinutes} minutes");
                Console.WriteLine($"• Directory Timeout: {_defaultSettings.DirectoryTimeout.TotalSeconds} seconds");
                Console.WriteLine($"• Network Drive Timeout: {_defaultSettings.NetworkDriveTimeout.TotalSeconds} seconds");
                Console.WriteLine($"• Skip Hidden Folders: {(_defaultSettings.SkipHiddenFolders ? "Yes" : "No")}");
                Console.WriteLine($"• Skip System Folders: {(_defaultSettings.SkipSystemFolders ? "Yes" : "No")}");
                Console.WriteLine();
                SetConsoleColor(ConsoleColor.Yellow);
                Console.WriteLine("═══ OPTIONS ═══");
                ResetConsoleColor();
                Console.WriteLine();
                Console.WriteLine("1. Modify default settings");
                Console.WriteLine("2. Reset to factory defaults");
                Console.WriteLine("3. Return to main menu");
                Console.WriteLine();
                Console.Write("Select option (1-3): ");

                var choice = GetUserChoice();
                switch (choice)
                {
                    case "1":
                        ModifyDefaultSettings();
                        break;
                    case "2":
                        ResetToFactoryDefaults();
                        break;
                    case "3":
                        return;
                    default:
                        DisplayError("Invalid choice. Press any key to continue...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private void ModifyDefaultSettings()
        {
            Clear();
            SetConsoleColor(ConsoleColor.Cyan);
            Console.WriteLine("═══ MODIFY DEFAULT SETTINGS ═══");
            ResetConsoleColor();
            Console.WriteLine();

            // Max threads
            Console.Write($"Max concurrent threads [{_defaultSettings.MaxThreads}]: ");
            var threadsInput = Console.ReadLine()?.Trim();
            if (int.TryParse(threadsInput, out int maxThreads) && maxThreads > 0)
            {
                _defaultSettings.MaxThreads = maxThreads;
            }

            // Skip hidden folders
            Console.Write($"Skip hidden folders? (y/n) [{(_defaultSettings.SkipHiddenFolders ? "y" : "n")}]: ");
            var hiddenInput = Console.ReadLine()?.Trim().ToLower();
            if (hiddenInput == "y" || hiddenInput == "yes")
                _defaultSettings.SkipHiddenFolders = true;
            else if (hiddenInput == "n" || hiddenInput == "no")
                _defaultSettings.SkipHiddenFolders = false;

            // Skip system folders
            Console.Write($"Skip system folders? (y/n) [{(_defaultSettings.SkipSystemFolders ? "y" : "n")}]: ");
            var systemInput = Console.ReadLine()?.Trim().ToLower();
            if (systemInput == "y" || systemInput == "yes")
                _defaultSettings.SkipSystemFolders = true;
            else if (systemInput == "n" || systemInput == "no")
                _defaultSettings.SkipSystemFolders = false;

            // Max depth
            Console.Write($"Maximum scan depth [{_defaultSettings.MaxDepth}]: ");
            var depthInput = Console.ReadLine()?.Trim();
            if (int.TryParse(depthInput, out int maxDepth) && maxDepth > 0)
            {
                _defaultSettings.MaxDepth = maxDepth;
            }

            // Memory optimization
            Console.Write($"Enable memory optimization? (y/n) [{(_defaultSettings.EnableMemoryOptimization ? "y" : "n")}]: ");
            var memOptInput = Console.ReadLine()?.Trim().ToLower();
            if (memOptInput == "y" || memOptInput == "yes")
                _defaultSettings.EnableMemoryOptimization = true;
            else if (memOptInput == "n" || memOptInput == "no")
                _defaultSettings.EnableMemoryOptimization = false;

            // Max memory usage (only if optimization is enabled)
            if (_defaultSettings.EnableMemoryOptimization)
            {
                Console.Write($"Maximum memory usage (MB) [{_defaultSettings.MaxMemoryUsageMB}]: ");
                var memInput = Console.ReadLine()?.Trim();
                if (long.TryParse(memInput, out long maxMemory) && maxMemory > 0)
                {
                    _defaultSettings.MaxMemoryUsageMB = maxMemory;
                }
            }

            Console.WriteLine();
            SetConsoleColor(ConsoleColor.Yellow);
            Console.WriteLine("═══ TIMEOUT SETTINGS ═══");
            ResetConsoleColor();

            // Global timeout
            Console.Write($"Global scan timeout (minutes) [{_defaultSettings.GlobalTimeout.TotalMinutes}]: ");
            var globalTimeoutInput = Console.ReadLine()?.Trim();
            if (int.TryParse(globalTimeoutInput, out int globalMinutes) && globalMinutes > 0)
            {
                _defaultSettings.GlobalTimeout = TimeSpan.FromMinutes(globalMinutes);
            }

            // Directory timeout
            Console.Write($"Directory timeout (seconds) [{_defaultSettings.DirectoryTimeout.TotalSeconds}]: ");
            var dirTimeoutInput = Console.ReadLine()?.Trim();
            if (int.TryParse(dirTimeoutInput, out int dirSeconds) && dirSeconds > 0)
            {
                _defaultSettings.DirectoryTimeout = TimeSpan.FromSeconds(dirSeconds);
            }

            // Network drive timeout
            Console.Write($"Network drive timeout (seconds) [{_defaultSettings.NetworkDriveTimeout.TotalSeconds}]: ");
            var networkTimeoutInput = Console.ReadLine()?.Trim();
            if (int.TryParse(networkTimeoutInput, out int networkSeconds) && networkSeconds > 0)
            {
                _defaultSettings.NetworkDriveTimeout = TimeSpan.FromSeconds(networkSeconds);
            }

            DisplaySuccess("Default settings updated successfully!");
            Console.WriteLine();
            Console.WriteLine("Press any key to return to settings menu...");
            Console.ReadKey();
        }

        private void ResetToFactoryDefaults()
        {
            _defaultSettings = ScanSettings.CreateDefault();
            DisplaySuccess("Settings reset to factory defaults!");
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        public async Task ExportResultsAsync(ScanResult result, ExportFormat format)
        {
            try
            {
                string outputFolder = "";

                switch (format)
                {
                    case ExportFormat.HtmlOnly:
                        DisplayInfo("Generating HTML report...");
                        var htmlExporter = new HtmlExporter();
                        await htmlExporter.ExportAsync(result);
                        outputFolder = GetOutputFolderPath(result);
                        DisplaySuccess("HTML report generated successfully!");
                        break;

                    case ExportFormat.PdfOnly:
                        DisplayInfo("Generating PDF report...");
                        var pdfExporter = new PdfExporter();
                        await pdfExporter.ExportAsync(result);
                        outputFolder = GetOutputFolderPath(result);
                        DisplaySuccess("PDF report generated successfully!");
                        break;

                    case ExportFormat.Both:
                        DisplayInfo("Generating reports...");
                        var htmlExp = new HtmlExporter();
                        var pdfExp = new PdfExporter();

                        DisplayInfo("Creating HTML report...");
                        await htmlExp.ExportAsync(result);
                        DisplaySuccess("HTML report completed!");

                        DisplayInfo("Creating PDF report...");
                        await pdfExp.ExportAsync(result);
                        DisplaySuccess("PDF report completed!");

                        outputFolder = GetOutputFolderPath(result);
                        break;
                }

                if (!string.IsNullOrEmpty(outputFolder))
                {
                    DisplayReportResults(outputFolder, format);

                    if (ConfirmAction("Open folder containing reports? (y/n): "))
                    {
                        OpenFolder(outputFolder);
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayError($"Export failed: {ex.Message}");
            }
        }

        public ExportFormat GetExportFormatChoice()
        {
            Console.WriteLine();
            SetConsoleColor(ConsoleColor.Yellow);
            Console.WriteLine("═══ EXPORT OPTIONS ═══");
            ResetConsoleColor();
            Console.WriteLine();
            Console.WriteLine("Choose export format:");
            Console.WriteLine("1. HTML report only");
            Console.WriteLine("2. PDF report only");
            Console.WriteLine("3. Both HTML and PDF reports (recommended)");
            Console.WriteLine("4. Skip export");
            Console.WriteLine();
            Console.Write("Select option (1-4) [3]: ");

            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) input = "3"; // Default to both

            return input switch
            {
                "1" => ExportFormat.HtmlOnly,
                "2" => ExportFormat.PdfOnly,
                "3" => ExportFormat.Both,
                "4" => ExportFormat.None,
                _ => ExportFormat.Both // Default to both
            };
        }

        private void DisplayReportResults(string outputFolder, ExportFormat format)
        {
            Console.WriteLine();
            SetConsoleColor(ConsoleColor.Green);
            Console.WriteLine("═══ REPORTS GENERATED ═══");
            ResetConsoleColor();
            Console.WriteLine();
            DisplayInfo($"Reports saved to: {outputFolder}");
            Console.WriteLine();
            Console.WriteLine("Files created:");

            if (format == ExportFormat.HtmlOnly || format == ExportFormat.Both)
            {
                SetConsoleColor(ConsoleColor.Green);
                Console.WriteLine("  ✓ FolderScan_Report.html");
                ResetConsoleColor();
            }

            if (format == ExportFormat.PdfOnly || format == ExportFormat.Both)
            {
                SetConsoleColor(ConsoleColor.Green);
                Console.WriteLine("  ✓ FolderScan_Report.pdf");
                ResetConsoleColor();
            }
        }

        public string GetOutputFolderPath(ScanResult scanResult)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var folderName = FileHelper.CreateScanFolderName(scanResult.ScannedPaths);
            return Path.Combine(desktop, folderName);
        }

        private void OpenFolder(string folderPath)
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"\"{folderPath}\"");
                    }
                    else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    {
                        System.Diagnostics.Process.Start("xdg-open", $"\"{folderPath}\"");
                    }
                    else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                    {
                        System.Diagnostics.Process.Start("open", $"\"{folderPath}\"");
                    }
                    DisplaySuccess("Folder opened successfully!");
                }
                else
                {
                    DisplayError("Output folder not found.");
                }
            }
            catch (Exception ex)
            {
                DisplayError($"Failed to open folder: {ex.Message}");
            }
        }

        public string GetExportPath()
        {
            Console.Write("Enter output file path (default: scan_results.html): ");
            var path = Console.ReadLine()?.Trim();
            return string.IsNullOrEmpty(path) ? "scan_results.html" : path;
        }

        public void DisplayScanResult(ScanResult result)
        {
            Console.WriteLine();
            SetConsoleColor(ConsoleColor.Green);
            Console.WriteLine("═══ SCAN COMPLETED ═══");
            ResetConsoleColor();
            Console.WriteLine();
            Console.WriteLine($"Scan Duration: {result.ScanDuration:hh\\:mm\\:ss}");
            Console.WriteLine($"Total Folders: {result.TotalFolders:N0}");
            Console.WriteLine($"Total Files: {result.TotalFiles:N0}");
            Console.WriteLine($"Root Folders Found: {result.RootFolders.Count}");
            Console.WriteLine();

            if (result.RootFolders.Any())
            {
                Console.WriteLine("Scanned Locations:");
                foreach (var rootFolder in result.RootFolders.Take(5))
                {
                    Console.WriteLine($"  • {rootFolder.FullPath}: {rootFolder.SubFolderCount:N0} folders, {rootFolder.FileCount:N0} files");
                }

                if (result.RootFolders.Count > 5)
                {
                    Console.WriteLine($"  ... and {result.RootFolders.Count - 5} more locations");
                }
            }
        }

        public void DisplayError(string message)
        {
            Console.WriteLine();
            SetConsoleColor(ConsoleColor.Red);
            Console.WriteLine($"❌ Error: {message}");
            ResetConsoleColor();
        }

        public void DisplayWarning(string message)
        {
            Console.WriteLine();
            SetConsoleColor(ConsoleColor.Yellow);
            Console.WriteLine($"⚠️  Warning: {message}");
            ResetConsoleColor();
        }

        public void DisplayInfo(string message)
        {
            Console.WriteLine();
            SetConsoleColor(ConsoleColor.Cyan);
            Console.WriteLine($"ℹ️  {message}");
            ResetConsoleColor();
        }

        public void DisplaySuccess(string message)
        {
            Console.WriteLine();
            SetConsoleColor(ConsoleColor.Green);
            Console.WriteLine($"✅ {message}");
            ResetConsoleColor();
        }

        public bool ConfirmAction(string message)
        {
            Console.WriteLine();
            Console.Write(message);
            var response = Console.ReadLine()?.Trim().ToLower();
            return response == "y" || response == "yes";
        }

        private string GetUserChoice()
        {
            return Console.ReadLine()?.Trim() ?? "";
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int suffixIndex = 0;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1} {suffixes[suffixIndex]}";
        }

        public void WaitForKey()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }

        public void Clear()
        {
            Console.Clear();
        }

        private void SetConsoleColor(ConsoleColor color)
        {
            Console.ForegroundColor = color;
        }

        private void ResetConsoleColor()
        {
            Console.ResetColor();
        }

        public void Cleanup()
        {
            try
            {
                _threadManager?.Dispose();
                _progressTracker = null;
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}