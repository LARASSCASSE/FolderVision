using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FolderVision.Core;

namespace FolderVision.Ui
{
    public class ProgressDisplay
    {
        private readonly int _progressBarWidth;
        private int _lastDisplayLines;
        private bool _isDisplaying;
        private DateTime _lastUpdateTime;
        private readonly object _lockObject = new object();

        public ProgressDisplay(int progressBarWidth = 10)
        {
            _progressBarWidth = progressBarWidth;
            _lastDisplayLines = 0;
            _isDisplaying = false;
            _lastUpdateTime = DateTime.Now;
        }

        public void UpdateProgress(ProgressChangedEventArgs args, List<string> scanPaths)
        {
            lock (_lockObject)
            {
                // Throttle updates to prevent flickering (max 10 updates per second)
                var now = DateTime.Now;
                if ((now - _lastUpdateTime).TotalMilliseconds < 100)
                    return;

                _lastUpdateTime = now;

                ClearPreviousDisplay();
                RenderProgressDisplay(args, scanPaths);
                _isDisplaying = true;
            }
        }

        private void RenderProgressDisplay(ProgressChangedEventArgs args, List<string> scanPaths)
        {
            // Simplified single-line progress display to avoid console manipulation errors
            var pathsDisplay = string.Join(", ", scanPaths.Select(p => GetPathDisplayName(p)));
            if (pathsDisplay.Length > 30)
            {
                pathsDisplay = pathsDisplay.Substring(0, 27) + "...";
            }

            var progressBar = CreateProgressBar(args.PercentComplete);
            var output = $"Scanning {pathsDisplay} {progressBar} {args.PercentComplete:F0}% | {args.ProcessedItems:N0} items";

            // Pad to clear previous text
            var paddedOutput = output.PadRight(120);
            Console.Write("\r" + paddedOutput);

            _lastDisplayLines = 1;
        }

        private void ClearPreviousDisplay()
        {
            // Simplified: just clear the current line for simple, stable display
            // No complex cursor manipulation to avoid console errors
            if (_lastDisplayLines > 0 && _isDisplaying)
            {
                try
                {
                    // Simple: just reset cursor to beginning of line
                    Console.Write("\r");
                }
                catch
                {
                    // If that fails, just continue
                }
            }
        }

        private string CreateProgressBar(double percentComplete)
        {
            var filled = (int)Math.Round(percentComplete / 100.0 * _progressBarWidth);
            var empty = _progressBarWidth - filled;

            var bar = new StringBuilder();
            bar.Append('[');
            bar.Append(new string('█', filled));
            bar.Append(new string('░', empty));
            bar.Append(']');

            return bar.ToString();
        }

        private string GetPathDisplayName(string fullPath)
        {
            try
            {
                if (string.IsNullOrEmpty(fullPath))
                    return "Unknown";

                // For drive roots, show just the drive letter
                if (fullPath.Length <= 3 && fullPath.EndsWith("\\"))
                    return fullPath.TrimEnd('\\') + ":";

                // For other paths, show just the folder name
                var name = System.IO.Path.GetFileName(fullPath);
                return string.IsNullOrEmpty(name) ? fullPath : name;
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetThreadDisplayPath(string currentPath, List<string> scanPaths, int threadId)
        {
            try
            {
                if (string.IsNullOrEmpty(currentPath))
                {
                    // Try to get the scan path for this thread
                    if (threadId < scanPaths.Count)
                    {
                        var scanPath = scanPaths[threadId];
                        return GetPathDisplayName(scanPath) + "...";
                    }
                    return "Waiting...";
                }

                // Find the relative path from the scan root
                string basePath = "";
                if (threadId < scanPaths.Count)
                {
                    basePath = scanPaths[threadId];
                }

                if (!string.IsNullOrEmpty(basePath) && currentPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = currentPath.Substring(basePath.Length).TrimStart('\\', '/');
                    if (string.IsNullOrEmpty(relativePath))
                    {
                        return GetPathDisplayName(basePath) + "...";
                    }

                    var parts = relativePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        var displayPath = GetPathDisplayName(basePath) + "\\" + parts[0];
                        if (displayPath.Length > 25)
                        {
                            displayPath = displayPath.Substring(0, 22) + "...";
                        }
                        return displayPath + "...";
                    }
                }

                // Fallback to truncated current path
                return TruncatePath(currentPath, 25);
            }
            catch
            {
                return "Processing...";
            }
        }

        private long GetEstimatedFolderCount(ThreadProgressSummary thread)
        {
            // Estimate based on progress percentage and some heuristics
            if (thread.PercentComplete > 0)
            {
                return Math.Max(1, thread.PercentComplete * 50); // Rough estimate
            }
            return 0;
        }

        private long EstimateProcessedFolders(ThreadProgressSummary thread)
        {
            return GetEstimatedFolderCount(thread);
        }

        private long EstimateProcessedFiles(ThreadProgressSummary thread)
        {
            // Estimate files as roughly 10x folders
            return GetEstimatedFolderCount(thread) * 10;
        }

        private string FormatTimeRemaining(TimeSpan timeRemaining)
        {
            if (timeRemaining == TimeSpan.MaxValue || timeRemaining.TotalSeconds < 0)
                return "calculating...";

            if (timeRemaining.TotalSeconds < 60)
                return $"{timeRemaining.TotalSeconds:F0}s";

            if (timeRemaining.TotalMinutes < 60)
                return $"{timeRemaining.Minutes}m {timeRemaining.Seconds}s";

            return $"{timeRemaining.Hours}h {timeRemaining.Minutes}m";
        }

        private string TruncatePath(string path, int maxLength)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= maxLength)
                return path ?? "";

            if (maxLength <= 3)
                return "...";

            return "..." + path.Substring(path.Length - (maxLength - 3));
        }

        public void CompleteScan()
        {
            lock (_lockObject)
            {
                // Clear progress line and show completion
                Console.WriteLine(); // Move to new line
                SetConsoleColor(ConsoleColor.Green);
                Console.WriteLine("✓ Scan completed successfully");
                ResetConsoleColor();
                Console.WriteLine();
                _isDisplaying = false;
                _lastDisplayLines = 0;
            }
        }

        public void ShowReportGeneration(string reportType, int progress = 0, string outputFolder = "")
        {
            lock (_lockObject)
            {
                ClearPreviousDisplay();

                var output = new StringBuilder();
                var lineCount = 0;

                SetConsoleColor(ConsoleColor.Cyan);
                output.AppendLine("=== GENERATING REPORTS ===");
                lineCount++;

                ResetConsoleColor();
                SetConsoleColor(ConsoleColor.Green);
                output.AppendLine("✓ Scan completed");
                lineCount++;

                ResetConsoleColor();
                if (reportType.Contains("HTML"))
                {
                    if (progress >= 100 && reportType.Contains("completed"))
                    {
                        SetConsoleColor(ConsoleColor.Green);
                        output.AppendLine("✓ HTML report completed");
                    }
                    else
                    {
                        SetConsoleColor(ConsoleColor.Yellow);
                        output.AppendLine("⏳ Creating HTML report...");
                    }
                    ResetConsoleColor();
                    lineCount++;
                }

                if (reportType.Contains("PDF"))
                {
                    if (progress >= 100 && reportType.Contains("completed"))
                    {
                        SetConsoleColor(ConsoleColor.Green);
                        output.AppendLine("✓ PDF report completed");
                    }
                    else
                    {
                        SetConsoleColor(ConsoleColor.Yellow);
                        output.AppendLine("⏳ Creating PDF report...");
                    }
                    ResetConsoleColor();
                    lineCount++;
                }

                if (!string.IsNullOrEmpty(outputFolder))
                {
                    SetConsoleColor(ConsoleColor.Green);
                    output.AppendLine($"✓ Reports saved to: {outputFolder}");
                    ResetConsoleColor();
                    lineCount++;
                }

                Console.Write(output.ToString());
                _lastDisplayLines = lineCount;
                _isDisplaying = true;
            }
        }

        public void CompleteReportGeneration(string outputFolder)
        {
            lock (_lockObject)
            {
                ClearPreviousDisplay();

                SetConsoleColor(ConsoleColor.Cyan);
                Console.WriteLine("=== GENERATING REPORTS ===");
                SetConsoleColor(ConsoleColor.Green);
                Console.WriteLine("✓ Scan completed");
                Console.WriteLine("✓ HTML report completed");
                Console.WriteLine("✓ PDF report completed");
                Console.WriteLine($"✓ Reports saved to: {outputFolder}");
                ResetConsoleColor();
                Console.WriteLine();

                _isDisplaying = false;
            }
        }

        public void StartProgress(string initialMessage = "Starting...")
        {
            lock (_lockObject)
            {
                _isDisplaying = true;
                Console.WriteLine(initialMessage);
            }
        }

        public void HideProgress()
        {
            lock (_lockObject)
            {
                if (_isDisplaying)
                {
                    ClearPreviousDisplay();
                    _isDisplaying = false;
                }
            }
        }

        // Legacy methods for compatibility
        public void ShowProgress(double percentComplete, string currentPath = "", long processedItems = 0, long totalItems = 0)
        {
            // Legacy method - convert to new format
        }

        public void ShowProgress(ProgressChangedEventArgs args)
        {
            UpdateProgress(args, new List<string>());
        }

        public void UpdateProgress(int percentComplete, string status = "")
        {
            // Legacy method - minimal implementation
            Console.Write($"\rProgress: {percentComplete}% {status}");
        }

        public void CompleteProgress(string completionMessage = "Completed!")
        {
            CompleteScan();
            Console.WriteLine(completionMessage);
        }

        private void SetConsoleColor(ConsoleColor color)
        {
            Console.ForegroundColor = color;
        }

        private void ResetConsoleColor()
        {
            Console.ResetColor();
        }

        public bool IsDisplaying => _isDisplaying;
    }
}