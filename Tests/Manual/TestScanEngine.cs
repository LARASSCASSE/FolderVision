using System;
using System.Threading.Tasks;
using FolderVision.Core;
using FolderVision.Models;

namespace FolderVision
{
    public class TestScanEngine
    {
        public static async Task TestBasicScanning()
        {
            var scanEngine = new ScanEngine();
            var settings = ScanSettings.CreateDefault();

            // Test with current directory
            var currentDir = Environment.CurrentDirectory;
            settings.AddPathToScan(currentDir);

            try
            {
                Console.WriteLine($"Testing scan of: {currentDir}");

                scanEngine.ProgressChanged += (sender, e) =>
                {
                    Console.WriteLine($"Progress: {e.PercentComplete}% - {e.CurrentPath}");
                };

                scanEngine.ScanCompleted += (sender, result) =>
                {
                    Console.WriteLine($"Scan completed: {result.TotalFolders} folders, {result.TotalFiles} files");
                };

                var result = await scanEngine.ScanFolderAsync(currentDir, settings);

                Console.WriteLine($"Final result: {result}");

                if (scanEngine.HasErrors)
                {
                    Console.WriteLine("Errors encountered:");
                    foreach (var error in scanEngine.GetErrors())
                    {
                        Console.WriteLine($"  - {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
            }
        }
    }
}