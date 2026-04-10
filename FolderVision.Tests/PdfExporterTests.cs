using FolderVision.Exporters;
using FolderVision.Models;
using iText.Kernel.Pdf;

namespace FolderVision.Tests
{
    public class PdfExporterTests
    {
        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static ScanResult BuildSampleResult()
        {
            var result = new ScanResult { ScanStartTime = DateTime.Now.AddSeconds(-3) };

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

        private static ScanResult BuildTwoRootResult()
        {
            var result = new ScanResult { ScanStartTime = DateTime.Now.AddSeconds(-5) };

            var root1 = new FolderInfo("C:\\Users\\Pascal");
            root1.SetFileCount(47);
            root1.AddSubFolder(new FolderInfo("C:\\Users\\Pascal\\Documents") );
            root1.AddSubFolder(new FolderInfo("C:\\Users\\Pascal\\Downloads"));
            root1.AddSubFolder(new FolderInfo("C:\\Users\\Pascal\\Desktop"));

            var root2 = new FolderInfo("C:\\Work");
            root2.SetFileCount(300);
            root2.AddSubFolder(new FolderInfo("C:\\Work\\API"));
            root2.AddSubFolder(new FolderInfo("C:\\Work\\Frontend"));

            result.AddRootFolder(root1);
            result.AddRootFolder(root2);
            result.AddScannedPath("C:\\Users\\Pascal");
            result.AddScannedPath("C:\\Work");
            result.SetScanDuration(DateTime.Now);
            result.UpdateTotals();
            return result;
        }

        private static async Task<byte[]> GeneratePdfBytesAsync(PdfExporter exporter, ScanResult result)
        {
            var path = Path.Combine(Path.GetTempPath(), $"FolderVisionTest_{Guid.NewGuid()}.pdf");
            try
            {
                await exporter.ExportAsync(result, path);
                return await File.ReadAllBytesAsync(path);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static int CountPdfPages(byte[] pdfBytes)
        {
            using var ms = new MemoryStream(pdfBytes);
            using var reader = new PdfReader(ms);
            using var pdf = new PdfDocument(reader);
            return pdf.GetNumberOfPages();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Default options
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void PdfExportOptions_Default_UseEmojisIsFalse()
        {
            Assert.False(PdfExportOptions.Default.UseEmojis);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Integration — PDF generation
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ExportAsync_WithDefaultOptions_CreatesValidPdf()
        {
            var bytes = await GeneratePdfBytesAsync(new PdfExporter(), BuildSampleResult());

            Assert.True(bytes.Length > 0);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        }

        [Fact]
        public async Task ExportAsync_OneRoot_ProducesTwoPages()
        {
            // 1 root folder → 2 pages (overview + detail)
            var bytes = await GeneratePdfBytesAsync(new PdfExporter(), BuildSampleResult());
            var pages = CountPdfPages(bytes);

            Assert.Equal(2, pages);
        }

        [Fact]
        public async Task ExportAsync_TwoRoots_ProducesFourPages()
        {
            // 2 root folders → 4 pages (2 per root)
            var bytes = await GeneratePdfBytesAsync(new PdfExporter(), BuildTwoRootResult());
            var pages = CountPdfPages(bytes);

            Assert.Equal(4, pages);
        }

        [Fact]
        public async Task ExportAsync_EmptyResult_CreatesValidPdf()
        {
            var result = new ScanResult { ScanStartTime = DateTime.Now };
            result.SetScanDuration(DateTime.Now);

            var bytes = await GeneratePdfBytesAsync(new PdfExporter(), result);

            Assert.True(bytes.Length > 0);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        }

        [Fact]
        public async Task ExportAsync_CompactOptions_CreatesValidPdf()
        {
            var bytes = await GeneratePdfBytesAsync(new PdfExporter(PdfExportOptions.Compact), BuildSampleResult());

            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        }

        [Fact]
        public async Task ExportAsync_MaxDepthOne_DoesNotThrow()
        {
            var options = new PdfExportOptions { MaxTreeDepth = 1 };
            // result has depth 0,1,2 — depth cap should truncate gracefully
            var bytes = await GeneratePdfBytesAsync(new PdfExporter(options), BuildSampleResult());

            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public async Task ExportAsync_FiresProgressEvents()
        {
            var exporter = new PdfExporter();
            var progressEvents = new List<int>();
            exporter.ExportProgress += (_, e) => progressEvents.Add(e.PercentComplete);

            await GeneratePdfBytesAsync(exporter, BuildSampleResult());

            Assert.NotEmpty(progressEvents);
            Assert.All(progressEvents, p => Assert.InRange(p, 0, 100));
        }

        [Fact]
        public async Task ExportAsync_InvalidPath_ThrowsException()
        {
            await Assert.ThrowsAnyAsync<Exception>(() =>
                new PdfExporter().ExportAsync(BuildSampleResult(),
                    "Z:\\NonExistentDrive\\impossible\\path.pdf"));
        }
    }
}
