using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using FolderVision.Models;

namespace FolderVision.Core
{
    public class ScanEngine
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly object _lockObject = new object();
        private long _processedDirectories;
        private long _totalDirectories;
        private readonly List<string> _errors;

        public ScanEngine()
        {
            _errors = new List<string>();
        }

        public async Task<ScanResult> ScanFolderAsync(string folderPath, ScanSettings settings)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));

            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

            var scanResult = new ScanResult
            {
                ScanStartTime = DateTime.Now
            };

            _cancellationTokenSource = new CancellationTokenSource();
            _processedDirectories = 0;
            _totalDirectories = 0;
            _errors.Clear();

            try
            {
                // First pass: estimate total directories for progress reporting
                await EstimateDirectoryCountAsync(folderPath, settings, _cancellationTokenSource.Token);

                // Second pass: actual scanning
                var rootFolder = await ScanDirectoryAsync(folderPath, settings, _cancellationTokenSource.Token);

                if (rootFolder != null)
                {
                    scanResult.AddRootFolder(rootFolder);
                    scanResult.AddScannedPath(folderPath);
                }

                scanResult.SetScanDuration(DateTime.Now);
                ScanCompleted?.Invoke(this, scanResult);
                return scanResult;
            }
            catch (OperationCanceledException)
            {
                scanResult.SetScanDuration(DateTime.Now);
                return scanResult;
            }
            catch (Exception ex)
            {
                _errors.Add($"Scan failed: {ex.Message}");
                scanResult.SetScanDuration(DateTime.Now);
                return scanResult;
            }
        }

        public async Task<FolderInfo?> ScanDirectoryAsync(string path, ScanSettings settings, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            try
            {
                // Check if we should skip this folder
                if (settings.ShouldSkipFolder(path))
                    return null;

                var folderInfo = new FolderInfo(path);

                // Get directory info
                var directoryInfo = new DirectoryInfo(path);
                folderInfo.LastModified = directoryInfo.LastWriteTime;

                // Report progress
                ReportProgress(path);

                // Scan files in current directory
                await ScanFilesAsync(folderInfo, directoryInfo, settings, cancellationToken);

                // Scan subdirectories
                await ScanSubdirectoriesAsync(folderInfo, directoryInfo, settings, cancellationToken);

                folderInfo.UpdateCounts();
                return folderInfo;
            }
            catch (UnauthorizedAccessException)
            {
                LogError($"Access denied: {path}");
                return null;
            }
            catch (PathTooLongException)
            {
                LogError($"Path too long: {path}");
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                LogError($"Directory not found: {path}");
                return null;
            }
            catch (IOException ex)
            {
                LogError($"IO error in {path}: {ex.Message}");
                return null;
            }
            catch (SecurityException)
            {
                LogError($"Security error accessing: {path}");
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Unexpected error in {path}: {ex.Message}");
                return null;
            }
        }

        private async Task ScanFilesAsync(FolderInfo folderInfo, DirectoryInfo directoryInfo, ScanSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var files = await Task.Run(() =>
                {
                    try
                    {
                        return directoryInfo.GetFiles();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        LogError($"Access denied to files in: {directoryInfo.FullName}");
                        return Array.Empty<FileInfo>();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error getting files in {directoryInfo.FullName}: {ex.Message}");
                        return Array.Empty<FileInfo>();
                    }
                }, cancellationToken);

                var fileCount = 0;
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        // Skip hidden files if settings specify
                        if (settings.SkipHiddenFolders && (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        fileCount++;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error processing file {file.FullName}: {ex.Message}");
                    }
                }

                folderInfo.SetFileCount(fileCount);
            }
            catch (Exception ex)
            {
                LogError($"Error scanning files in {directoryInfo.FullName}: {ex.Message}");
                folderInfo.SetFileCount(0);
            }
        }

        private async Task ScanSubdirectoriesAsync(FolderInfo folderInfo, DirectoryInfo directoryInfo, ScanSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var subdirectories = await Task.Run(() =>
                {
                    try
                    {
                        return directoryInfo.GetDirectories();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        LogError($"Access denied to subdirectories in: {directoryInfo.FullName}");
                        return Array.Empty<DirectoryInfo>();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error getting subdirectories in {directoryInfo.FullName}: {ex.Message}");
                        return Array.Empty<DirectoryInfo>();
                    }
                }, cancellationToken);

                // Optimize for large directory structures
                if (subdirectories.Length > 100)
                {
                    // Process in batches for very large directories
                    await ProcessSubdirectoriesInBatches(folderInfo, subdirectories, settings, cancellationToken);
                }
                else
                {
                    // Standard parallel processing for smaller directories
                    var semaphore = new SemaphoreSlim(settings.MaxThreads, settings.MaxThreads);
                    var tasks = new List<Task>();

                    foreach (var subdir in subdirectories)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        tasks.Add(ProcessSubdirectoryAsync(folderInfo, subdir, settings, semaphore, cancellationToken));
                    }

                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error scanning subdirectories in {directoryInfo.FullName}: {ex.Message}");
            }
        }

        private async Task ProcessSubdirectoriesInBatches(FolderInfo folderInfo, DirectoryInfo[] subdirectories, ScanSettings settings, CancellationToken cancellationToken)
        {
            const int batchSize = 50; // Process 50 directories at a time
            var semaphore = new SemaphoreSlim(Math.Min(settings.MaxThreads, 8), Math.Min(settings.MaxThreads, 8)); // Limit concurrency for large batches

            for (int i = 0; i < subdirectories.Length; i += batchSize)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var batch = subdirectories.Skip(i).Take(batchSize);
                var batchTasks = new List<Task>();

                foreach (var subdir in batch)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    batchTasks.Add(ProcessSubdirectoryAsync(folderInfo, subdir, settings, semaphore, cancellationToken));
                }

                await Task.WhenAll(batchTasks);

                // Allow GC to collect completed tasks and reduce memory pressure
                if (i % (batchSize * 4) == 0) // Every 4 batches
                {
                    GC.Collect(0, GCCollectionMode.Optimized);
                }
            }
        }

        private async Task ProcessSubdirectoryAsync(FolderInfo parentFolder, DirectoryInfo subdir, ScanSettings settings, SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var subFolderInfo = await ScanDirectoryAsync(subdir.FullName, settings, cancellationToken);
                if (subFolderInfo != null)
                {
                    lock (_lockObject)
                    {
                        parentFolder.AddSubFolder(subFolderInfo);
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task EstimateDirectoryCountAsync(string path, ScanSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                _totalDirectories = await Task.Run(() => EstimateDirectoryCountRecursive(path, settings, cancellationToken), cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Error estimating directory count: {ex.Message}");
                _totalDirectories = 1; // Fallback to prevent division by zero
            }
        }

        private long EstimateDirectoryCountRecursive(string path, ScanSettings settings, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return 0;

            try
            {
                if (settings.ShouldSkipFolder(path))
                    return 0;

                long count = 1; // Count current directory

                var directoryInfo = new DirectoryInfo(path);
                var subdirectories = directoryInfo.GetDirectories();

                foreach (var subdir in subdirectories)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        count += EstimateDirectoryCountRecursive(subdir.FullName, settings, cancellationToken);
                    }
                    catch (Exception)
                    {
                        // Skip directories we can't access during estimation
                    }
                }

                return count;
            }
            catch (Exception)
            {
                return 1; // Return 1 for the current directory even if we can't scan subdirectories
            }
        }

        private void ReportProgress(string currentPath)
        {
            lock (_lockObject)
            {
                _processedDirectories++;

                var percentComplete = _totalDirectories > 0
                    ? (int)((_processedDirectories * 100) / _totalDirectories)
                    : 0;

                var args = new ProgressEventArgs
                {
                    PercentComplete = Math.Min(percentComplete, 100),
                    CurrentPath = currentPath,
                    ProcessedFiles = _processedDirectories,
                    TotalFiles = _totalDirectories
                };

                ProgressChanged?.Invoke(this, args);
            }
        }

        private void LogError(string message)
        {
            lock (_lockObject)
            {
                _errors.Add(message);
            }
        }

        public void CancelScan()
        {
            _cancellationTokenSource?.Cancel();
        }

        public IReadOnlyList<string> GetErrors()
        {
            lock (_lockObject)
            {
                return _errors.ToList();
            }
        }

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        public event EventHandler<ScanResult>? ScanCompleted;
        public event EventHandler<string>? ErrorOccurred;
    }

    public class ProgressEventArgs : EventArgs
    {
        public int PercentComplete { get; set; }
        public string CurrentPath { get; set; } = string.Empty;
        public long ProcessedFiles { get; set; }
        public long TotalFiles { get; set; }
    }
}