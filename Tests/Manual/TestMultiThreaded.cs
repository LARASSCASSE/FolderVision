using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FolderVision.Core;
using FolderVision.Models;

namespace FolderVision
{
    public class TestMultiThreaded
    {
        public static async Task TestMultiPathScanning()
        {
            Console.WriteLine("=== Testing Multi-Threaded Scanning ===");

            var threadManager = new ThreadManager(maxConcurrency: 4);
            var progressTracker = new ProgressTracker();
            var settings = ScanSettings.CreateDefault();

            // Test paths - you can modify these to actual paths on your system
            var testPaths = new List<string>
            {
                Environment.CurrentDirectory,
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            // Filter to only existing paths
            var validPaths = new List<string>();
            foreach (var path in testPaths)
            {
                if (System.IO.Directory.Exists(path))
                {
                    validPaths.Add(path);
                    Console.WriteLine($"Added test path: {path}");
                }
            }

            if (validPaths.Count == 0)
            {
                Console.WriteLine("No valid test paths found!");
                return;
            }

            // Subscribe to progress events
            progressTracker.ProgressChanged += (sender, e) =>
            {
                Console.Clear();
                Console.WriteLine("=== Multi-Threaded Folder Scan Progress ===");
                Console.WriteLine($"Overall Progress: {e.PercentComplete:F1}%");
                Console.WriteLine($"Elapsed: {e.ElapsedTime:hh\\:mm\\:ss}");
                Console.WriteLine($"Estimated Remaining: {e.EstimatedTimeRemaining:hh\\:mm\\:ss}");
                Console.WriteLine($"Active Threads: {e.ActiveThreads}");
                Console.WriteLine($"Completed Threads: {e.CompletedThreads}/{e.TotalItems}");
                Console.WriteLine($"Failed Threads: {e.FailedThreads}");
                Console.WriteLine();

                Console.WriteLine("Thread Details:");
                Console.WriteLine(new string('-', 80));
                foreach (var thread in e.ThreadDetails)
                {
                    var status = thread.Status switch
                    {
                        ThreadProgressStatus.Pending => "PENDING",
                        ThreadProgressStatus.Running => "RUNNING",
                        ThreadProgressStatus.Completed => "DONE",
                        ThreadProgressStatus.Failed => "FAILED",
                        ThreadProgressStatus.Cancelled => "CANCELLED",
                        _ => "UNKNOWN"
                    };

                    var currentPath = string.IsNullOrEmpty(thread.CurrentPath)
                        ? "Waiting..."
                        : thread.CurrentPath.Length > 50
                            ? "..." + thread.CurrentPath.Substring(thread.CurrentPath.Length - 47)
                            : thread.CurrentPath;

                    Console.WriteLine($"Thread {thread.ThreadId,2}: [{status,-8}] {thread.PercentComplete,3}% | {currentPath}");

                    if (!string.IsNullOrEmpty(thread.Error))
                    {
                        Console.WriteLine($"           Error: {thread.Error}");
                    }
                }
                Console.WriteLine(new string('-', 80));
            };

            try
            {
                Console.WriteLine($"Starting scan of {validPaths.Count} paths with max {threadManager.MaxConcurrency} concurrent threads...");

                var startTime = DateTime.Now;
                var result = await threadManager.ScanMultiplePathsAsync(validPaths, settings, progressTracker);
                var endTime = DateTime.Now;

                Console.Clear();
                Console.WriteLine("=== Scan Results ===");
                Console.WriteLine($"Scan Duration: {endTime - startTime:hh\\:mm\\:ss}");
                Console.WriteLine($"Total Root Folders: {result.RootFolders.Count}");
                Console.WriteLine($"Total Folders: {result.TotalFolders}");
                Console.WriteLine($"Total Files: {result.TotalFiles}");
                Console.WriteLine($"Scanned Paths: {result.ScannedPaths.Count}");
                Console.WriteLine();

                Console.WriteLine("Scanned Paths:");
                foreach (var path in result.ScannedPaths)
                {
                    Console.WriteLine($"  - {path}");
                }

                Console.WriteLine();
                Console.WriteLine("Root Folders Found:");
                foreach (var rootFolder in result.RootFolders)
                {
                    Console.WriteLine($"  - {rootFolder.Name}: {rootFolder.SubFolderCount} subfolders, {rootFolder.FileCount} files");
                }

                // Check for thread failures
                var threadInfos = threadManager.GetThreadInfos();
                var failedThreads = threadInfos.Values.Where(t => t.Status == ThreadStatus.Failed).ToList();

                if (failedThreads.Any())
                {
                    Console.WriteLine();
                    Console.WriteLine("Failed Threads:");
                    foreach (var failedThread in failedThreads)
                    {
                        Console.WriteLine($"  - Thread {failedThread.ThreadId} ({failedThread.Path}): {failedThread.Error}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                threadManager.Dispose();
            }
        }

        public static async Task TestProgressTrackerOnly()
        {
            Console.WriteLine("=== Testing Progress Tracker Only ===");

            var progressTracker = new ProgressTracker();

            progressTracker.ProgressChanged += (sender, e) =>
            {
                Console.WriteLine($"Progress: {e.PercentComplete:F1}% | " +
                                $"Active: {e.ActiveThreads} | " +
                                $"Completed: {e.CompletedThreads}/{e.TotalItems} | " +
                                $"ETA: {e.EstimatedTimeRemaining:hh\\:mm\\:ss}");
            };

            // Simulate 3 threads
            progressTracker.Initialize(3);

            // Simulate thread progress
            for (int i = 0; i < 100; i += 10)
            {
                await Task.Delay(500); // Simulate work
                progressTracker.UpdateThreadProgress(0, i, $"Processing path A - step {i}");

                if (i >= 30)
                    progressTracker.UpdateThreadProgress(1, i - 30, $"Processing path B - step {i - 30}");

                if (i >= 60)
                    progressTracker.UpdateThreadProgress(2, i - 60, $"Processing path C - step {i - 60}");
            }

            // Complete threads
            progressTracker.CompleteThread(0);
            await Task.Delay(1000);
            progressTracker.CompleteThread(1);
            await Task.Delay(500);
            progressTracker.CompleteThread(2);

            Console.WriteLine("Progress tracker test completed!");
        }
    }
}