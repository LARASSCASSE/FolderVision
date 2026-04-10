using FolderVision.Exporters;
using FolderVision.Models;

namespace FolderVision.Tests
{
    public class PdfExporterTests
    {
        private static ScanResult BuildSampleResult()
        {
            var result = new ScanResult
            {
                ScanStartTime = DateTime.Now.AddSeconds(-3)
            };

            var root = new FolderInfo("C:\\SampleRoot");
            root.SetFileCount(10);

            var child1 = new FolderInfo("C:\\SampleRoot\\Documents");
            child1.SetFileCount(5);

            var child2 = new FolderInfo("C:\\SampleRoot\\Images");
            child2.SetFileCount(25);

            var grandchild = new FolderInfo("C:\\SampleRoot\\Documents\\Reports");
            grandchild.SetFileCount(3);

            child1.AddSubFolder(grandchild);
            root.AddSubFolder(child1);
            root.AddSubFolder(child2);
            result.AddRootFolder(root);
            result.AddScannedPath("C:\\SampleRoot");
            result.SetScanDuration(DateTime.Now);
            result.UpdateTotals();

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Default options
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void PdfExportOptions_Default_UseEmojisIsFalse()
        {
            // HELVETICA cannot render Unicode emoji — default must be false
            var options = PdfExportOptions.Default;
            Assert.False(options.UseEmojis);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Integration — actually generates a PDF
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ExportAsync_WithDefaultOptions_CreatesValidPdf()
        {
            var exporter = new PdfExporter();
            var result = BuildSampleResult();
            var outputPath = Path.Combine(Path.GetTempPath(), $"FolderVisionTest_{Guid.NewGuid()}.pdf");

            try
            {
                await exporter.ExportAsync(result, outputPath);

                Assert.True(File.Exists(outputPath), "PDF file should exist");
                var bytes = await File.ReadAllBytesAsync(outputPath);
                Assert.True(bytes.Length > 0, "PDF file should not be empty");
                // Valid PDF starts with %PDF-
                Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public async Task ExportAsync_CompactOptions_CreatesValidPdf()
        {
            var exporter = new PdfExporter(PdfExportOptions.Compact);
            var result = BuildSampleResult();
            var outputPath = Path.Combine(Path.GetTempPath(), $"FolderVisionCompact_{Guid.NewGuid()}.pdf");

            try
            {
                await exporter.ExportAsync(result, outputPath);

                Assert.True(File.Exists(outputPath));
                var bytes = await File.ReadAllBytesAsync(outputPath);
                Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public async Task ExportAsync_NoFolderTree_CreatesValidPdf()
        {
            var options = new PdfExportOptions
            {
                IncludeFolderTree = false,
                IncludeStatistics = true,
                IncludeTableOfContents = false,
                UseEmojis = false
            };
            var exporter = new PdfExporter(options);
            var result = BuildSampleResult();
            var outputPath = Path.Combine(Path.GetTempPath(), $"FolderVisionNoTree_{Guid.NewGuid()}.pdf");

            try
            {
                await exporter.ExportAsync(result, outputPath);
                Assert.True(File.Exists(outputPath));
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public async Task ExportAsync_EmptyResult_CreatesValidPdf()
        {
            var exporter = new PdfExporter();
            var result = new ScanResult { ScanStartTime = DateTime.Now };
            result.SetScanDuration(DateTime.Now);
            var outputPath = Path.Combine(Path.GetTempPath(), $"FolderVisionEmpty_{Guid.NewGuid()}.pdf");

            try
            {
                await exporter.ExportAsync(result, outputPath);
                Assert.True(File.Exists(outputPath));
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public async Task ExportAsync_MaxDepthLimitsTree()
        {
            var options = new PdfExportOptions { MaxTreeDepth = 1, UseEmojis = false };
            var exporter = new PdfExporter(options);
            var result = BuildSampleResult(); // has depth 0,1,2
            var outputPath = Path.Combine(Path.GetTempPath(), $"FolderVisionDepth_{Guid.NewGuid()}.pdf");

            try
            {
                // Should not throw even with MaxTreeDepth=1 limiting output
                await exporter.ExportAsync(result, outputPath);
                Assert.True(File.Exists(outputPath));
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public async Task ExportAsync_FiresProgressEvents()
        {
            var exporter = new PdfExporter();
            var result = BuildSampleResult();
            var outputPath = Path.Combine(Path.GetTempPath(), $"FolderVisionProgress_{Guid.NewGuid()}.pdf");
            var progressEvents = new List<int>();

            exporter.ExportProgress += (_, e) => progressEvents.Add(e.PercentComplete);

            try
            {
                await exporter.ExportAsync(result, outputPath);
                Assert.NotEmpty(progressEvents);
                Assert.All(progressEvents, p => Assert.InRange(p, 0, 100));
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public async Task ExportAsync_InvalidPath_ThrowsException()
        {
            var exporter = new PdfExporter();
            var result = BuildSampleResult();
            var invalidPath = "Z:\\NonExistentDrive\\impossible\\path.pdf";

            await Assert.ThrowsAnyAsync<Exception>(() =>
                exporter.ExportAsync(result, invalidPath));
        }
    }
}
