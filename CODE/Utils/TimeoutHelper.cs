using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FolderVision.Models;

namespace FolderVision.Utils
{
    public static class TimeoutHelper
    {
        public static bool IsNetworkPath(string path)
        {
            try
            {
                // Check if it's a UNC path (\\server\share) or mapped drive pointing to network
                if (path.StartsWith(@"\\") || path.StartsWith("//"))
                    return true;

                // For mapped drives, check the drive type
                if (Path.IsPathRooted(path))
                {
                    var driveInfo = new DriveInfo(Path.GetPathRoot(path) ?? path);
                    return driveInfo.DriveType == DriveType.Network;
                }

                return false;
            }
            catch
            {
                // If we can't determine, assume it's local
                return false;
            }
        }

        public static async Task<T> ExecuteWithTimeout<T>(
            Func<CancellationToken, Task<T>> operation,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                return await operation(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
            }
        }

        public static async Task ExecuteWithTimeout(
            Func<CancellationToken, Task> operation,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                await operation(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
            }
        }

        public static TimeSpan GetTimeoutForPath(string path, ScanSettings settings)
        {
            return IsNetworkPath(path) ? settings.NetworkDriveTimeout : settings.DirectoryTimeout;
        }
    }
}