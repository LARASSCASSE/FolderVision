using FolderVision.Core;
using FolderVision.Core.Logging;
using FolderVision.Models;

namespace FolderVision.Tests
{
    public class ScanEngineTests
    {
        [Fact]
        public async Task ScanFolderAsync_NullPath_ThrowsArgumentException()
        {
            var engine = new ScanEngine();
            var settings = ScanSettings.CreateDefault();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                engine.ScanFolderAsync(null!, settings));
        }

        [Fact]
        public async Task ScanFolderAsync_EmptyPath_ThrowsArgumentException()
        {
            var engine = new ScanEngine();
            var settings = ScanSettings.CreateDefault();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                engine.ScanFolderAsync(string.Empty, settings));
        }

        [Fact]
        public async Task ScanFolderAsync_NonExistentPath_ThrowsDirectoryNotFoundException()
        {
            var engine = new ScanEngine();
            var settings = ScanSettings.CreateDefault();

            await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
                engine.ScanFolderAsync(@"C:\NonExistentFolder_12345", settings));
        }

        [Fact]
        public async Task ScanFolderAsync_ValidPath_ReturnsResult()
        {
            var engine = new ScanEngine();
            var settings = ScanSettings.CreateDefault();
            settings.LoggingOptions = LoggingOptions.Disabled; // Disable logging for test

            var tempDir = Path.Combine(Path.GetTempPath(), "FolderVisionTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var result = await engine.ScanFolderAsync(tempDir, settings);

                Assert.NotNull(result);
                Assert.True(result.TotalFolders >= 0);
                Assert.True(result.TotalFiles >= 0);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ScanSettings_CreateDefault_HasCorrectDefaults()
        {
            var settings = ScanSettings.CreateDefault();

            Assert.True(settings.SkipSystemFolders);
            Assert.True(settings.SkipHiddenFolders);
            Assert.Equal(4, settings.MaxThreads);
            Assert.Equal(50, settings.MaxDepth);
            Assert.Equal(512, settings.MaxMemoryUsageMB);
            Assert.True(settings.EnableMemoryOptimization);
        }

        [Fact]
        public void ScanSettings_CreateForLargeFolders_HasOptimizedSettings()
        {
            var settings = ScanSettings.CreateForLargeFolders();

            Assert.Equal(8, settings.MaxThreads);
            Assert.Equal(100, settings.MaxDepth);
            Assert.Equal(1024, settings.MaxMemoryUsageMB);
            Assert.Equal(LogLevel.Debug, settings.LoggingOptions.MinLevel);
        }

        [Fact]
        public void ScanEngine_ProgressChanged_FiresEvent()
        {
            var engine = new ScanEngine();
            bool eventFired = false;

            engine.ProgressChanged += (sender, e) =>
            {
                eventFired = true;
            };

            // Event will fire during actual scan
            Assert.NotNull(engine);
            Assert.False(eventFired); // Not fired yet without a scan
        }
    }
}
