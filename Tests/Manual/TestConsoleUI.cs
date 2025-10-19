using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FolderVision.Core;
using FolderVision.Ui;

namespace FolderVision
{
    public class TestConsoleUI
    {
        public static async Task TestProgressDisplayOnly()
        {
            Console.WriteLine("=== Testing Progress Display ===");
            Console.WriteLine("This will simulate the exact progress display format.");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            var progressDisplay = new ProgressDisplay();
            var scanPaths = new List<string> { "C:\\", "D:\\", "E:\\" };

            // Simulate progress updates
            for (int overall = 0; overall <= 100; overall += 5)
            {
                var args = new ProgressChangedEventArgs
                {
                    PercentComplete = overall,
                    ActiveThreads = Math.Min(4, (overall / 25) + 1),
                    TotalItems = 4,
                    CompletedThreads = overall / 25,
                    ElapsedTime = TimeSpan.FromSeconds(overall * 2),
                    EstimatedTimeRemaining = TimeSpan.FromSeconds((100 - overall) * 2),
                    ThreadDetails = CreateMockThreadDetails(overall)
                };

                progressDisplay.UpdateProgress(args, scanPaths);
                await Task.Delay(500); // Update every 500ms
            }

            progressDisplay.CompleteScan();
            Console.WriteLine("Progress display test completed!");
        }

        private static List<ThreadProgressSummary> CreateMockThreadDetails(int overallProgress)
        {
            var threads = new List<ThreadProgressSummary>();

            // Thread 1 - C:\Program Files
            if (overallProgress >= 0)
            {
                threads.Add(new ThreadProgressSummary
                {
                    ThreadId = 0,
                    PercentComplete = Math.Min(100, overallProgress + 20),
                    Status = overallProgress >= 80 ? ThreadProgressStatus.Completed : ThreadProgressStatus.Running,
                    CurrentPath = "C:\\Program Files\\Microsoft\\Office",
                    ElapsedTime = TimeSpan.FromMinutes(overallProgress / 10.0)
                });
            }

            // Thread 2 - D:\Games
            if (overallProgress >= 25)
            {
                threads.Add(new ThreadProgressSummary
                {
                    ThreadId = 1,
                    PercentComplete = Math.Min(100, overallProgress - 10),
                    Status = overallProgress >= 90 ? ThreadProgressStatus.Completed : ThreadProgressStatus.Running,
                    CurrentPath = "D:\\Games\\Steam\\steamapps\\common",
                    ElapsedTime = TimeSpan.FromMinutes((overallProgress - 25) / 10.0)
                });
            }

            // Thread 3 - C:\Users
            if (overallProgress >= 50)
            {
                threads.Add(new ThreadProgressSummary
                {
                    ThreadId = 2,
                    PercentComplete = Math.Min(100, overallProgress - 25),
                    Status = ThreadProgressStatus.Running,
                    CurrentPath = "C:\\Users\\John\\AppData\\Local\\Temp",
                    ElapsedTime = TimeSpan.FromMinutes((overallProgress - 50) / 10.0)
                });
            }

            // Thread 4 - E:\Backup
            if (overallProgress >= 75)
            {
                threads.Add(new ThreadProgressSummary
                {
                    ThreadId = 3,
                    PercentComplete = Math.Min(100, overallProgress - 40),
                    Status = ThreadProgressStatus.Running,
                    CurrentPath = "E:\\Backup\\Documents\\Projects\\2024",
                    ElapsedTime = TimeSpan.FromMinutes((overallProgress - 75) / 10.0)
                });
            }

            return threads;
        }

        public static async Task TestFullConsoleUI()
        {
            Console.WriteLine("=== Testing Full Console UI ===");
            Console.WriteLine("This will test the complete console UI experience.");
            Console.WriteLine("Note: This is a simulation - actual scanning requires valid paths.");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            try
            {
                var consoleUI = new ConsoleUI();

                // Test welcome message
                consoleUI.ShowWelcomeMessage();
                await Task.Delay(2000);

                // Test drive display simulation
                TestDriveDisplay();
                await Task.Delay(3000);

                // Test scan settings
                TestScanSettings();
                await Task.Delay(2000);

                // Test progress display
                await TestSimulatedScan();

                Console.WriteLine("Full Console UI test completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
            }
        }

        private static void TestDriveDisplay()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("═══ DRIVE SCANNER ═══");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine("Available Drives:");
            Console.WriteLine("═════════════════");
            Console.WriteLine(" 1. C:\\   [Fixed     ] Windows");
            Console.WriteLine("     Total: 500.0 GB   Used: 350.2 GB   Free: 149.8 GB   (70.0% used)");
            Console.WriteLine();
            Console.WriteLine(" 2. D:\\   [Fixed     ] Data");
            Console.WriteLine("     Total: 1.0 TB     Used: 750.5 GB   Free: 273.5 GB   (75.1% used)");
            Console.WriteLine();
            Console.WriteLine(" 3. E:\\   [Removable ] Backup");
            Console.WriteLine("     Total: 2.0 TB     Used: 1.2 TB     Free: 800.0 GB   (60.0% used)");
            Console.WriteLine();
            Console.WriteLine("Enter drive numbers to scan (e.g., 1,3,4) or 'all' for all drives:");
            Console.WriteLine("Selection: 1,2,3 (simulated)");
        }

        private static void TestScanSettings()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("═══ SCAN SETTINGS ═══");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Max concurrent threads [4]: 4");
            Console.WriteLine("Skip hidden folders? (y/n) [y]: y");
            Console.WriteLine("Skip system folders? (y/n) [y]: y");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("ℹ️  Starting scan of 3 path(s) with 4 threads...");
            Console.ResetColor();
            Console.WriteLine("Press ESC to cancel the scan.");
            Console.WriteLine();
        }

        private static async Task TestSimulatedScan()
        {
            var progressDisplay = new ProgressDisplay();
            var scanPaths = new List<string> { "C:\\", "D:\\", "E:\\" };

            Console.WriteLine("Starting simulated scan...");
            await Task.Delay(1000);

            // Run the progress simulation
            for (int progress = 0; progress <= 100; progress += 2)
            {
                var args = new ProgressChangedEventArgs
                {
                    PercentComplete = progress,
                    ActiveThreads = Math.Min(3, (progress / 33) + 1),
                    TotalItems = 3,
                    CompletedThreads = progress / 33,
                    ElapsedTime = TimeSpan.FromSeconds(progress * 0.5),
                    EstimatedTimeRemaining = TimeSpan.FromSeconds((100 - progress) * 0.5),
                    ThreadDetails = CreateMockThreadDetails(progress)
                };

                progressDisplay.UpdateProgress(args, scanPaths);
                await Task.Delay(200);

                // Simulate ESC key check (in real app this would be handled differently)
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine("Scan cancelled!");
                        return;
                    }
                }
            }

            progressDisplay.CompleteScan();

            // Show final results
            ShowScanResults();
        }

        private static void ShowScanResults()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("═══ SCAN COMPLETED ═══");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Scan Duration: 00:01:30");
            Console.WriteLine("Total Folders: 15,847");
            Console.WriteLine("Total Files: 234,521");
            Console.WriteLine("Root Folders Found: 3");
            Console.WriteLine();
            Console.WriteLine("Scanned Locations:");
            Console.WriteLine("  • C:\\: 8,247 folders, 156,321 files");
            Console.WriteLine("  • D:\\: 4,856 folders, 45,123 files");
            Console.WriteLine("  • E:\\: 2,744 folders, 33,077 files");
            Console.WriteLine();
            Console.WriteLine("Export results to HTML? (y/n): n (simulated)");
        }

        public static void TestUnicodeCharacters()
        {
            Console.WriteLine("=== Testing Unicode Characters ===");
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("Progress Bar Characters:");
            Console.WriteLine($"Filled: {'█'} Empty: {'░'}");
            Console.WriteLine("Example: [████████░░] 80%");
            Console.WriteLine();

            Console.WriteLine("Tree Structure Characters:");
            Console.WriteLine("├── Branch");
            Console.WriteLine("├── Another Branch");
            Console.WriteLine("└── Last Branch");
            Console.WriteLine();

            Console.WriteLine("Box Drawing Characters:");
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        FOLDER VISION                        ║");
            Console.WriteLine("║                  Multi-Threaded Folder Scanner              ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            Console.WriteLine("Status Symbols:");
            Console.WriteLine("✅ Success message");
            Console.WriteLine("❌ Error message");
            Console.WriteLine("⚠️  Warning message");
            Console.WriteLine("ℹ️  Info message");
            Console.WriteLine("✓ Added item");
            Console.WriteLine("✗ Failed item");
        }

        public static async Task RunAllTests()
        {
            Console.Clear();
            Console.WriteLine("FolderVision Console UI Test Suite");
            Console.WriteLine("===================================");
            Console.WriteLine();
            Console.WriteLine("Select test to run:");
            Console.WriteLine("1. Progress Display Only");
            Console.WriteLine("2. Full Console UI Simulation");
            Console.WriteLine("3. Unicode Characters Test");
            Console.WriteLine("4. Run All Tests");
            Console.WriteLine("5. Exit");
            Console.WriteLine();
            Console.Write("Selection (1-5): ");

            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await TestProgressDisplayOnly();
                    break;
                case "2":
                    await TestFullConsoleUI();
                    break;
                case "3":
                    TestUnicodeCharacters();
                    break;
                case "4":
                    TestUnicodeCharacters();
                    await Task.Delay(3000);
                    await TestProgressDisplayOnly();
                    await Task.Delay(2000);
                    await TestFullConsoleUI();
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Invalid selection.");
                    break;
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}