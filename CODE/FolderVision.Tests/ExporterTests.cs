using FolderVision.Models;

namespace FolderVision.Tests
{
    public class ExporterTests
    {
        [Fact]
        public void PdfExportOptions_Default_HasCorrectValues()
        {
            var options = PdfExportOptions.Default;

            Assert.True(options.IncludeHeader);
            Assert.Equal(8, options.MaxTreeDepth);
            Assert.Equal(10, options.FontSize);
            Assert.True(options.IncludeDuplicates);
        }

        [Fact]
        public void PdfExportOptions_Compact_HasCompactSettings()
        {
            var options = PdfExportOptions.Compact;

            Assert.Equal(3, options.MaxTreeDepth);
        }

        [Fact]
        public void PdfExportOptions_Detailed_HasDeepTree()
        {
            var options = PdfExportOptions.Detailed;

            Assert.Equal(10, options.MaxTreeDepth);
        }

        [Fact]
        public void PdfExportOptions_French_HasCustomTitle()
        {
            var options = PdfExportOptions.French;

            Assert.Equal("Rapport de Scan de Dossiers", options.CustomTitle);
        }
    }
}
