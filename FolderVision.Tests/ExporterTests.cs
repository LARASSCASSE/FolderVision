using FolderVision.Exporters;
using FolderVision.Models;

namespace FolderVision.Tests
{
    public class ExporterTests
    {
        [Fact]
        public void HtmlExportOptions_Default_HasCorrectValues()
        {
            var options = HtmlExportOptions.Default;

            Assert.Equal(ColorScheme.Default, options.ColorScheme);
            Assert.True(options.IncludeFolderTree);
            Assert.True(options.IncludeStatistics);
            Assert.True(options.UseEmojis);
        }

        [Fact]
        public void HtmlExportOptions_Compact_HasMinimalSettings()
        {
            var options = HtmlExportOptions.Compact;

            Assert.False(options.UseEmojis);
            Assert.False(options.IncludeStatistics);
            Assert.Equal(3, options.MaxTreeDepth);
        }

        [Theory]
        [InlineData(ColorScheme.Blue)]
        [InlineData(ColorScheme.Green)]
        [InlineData(ColorScheme.Red)]
        [InlineData(ColorScheme.Dark)]
        [InlineData(ColorScheme.Monochrome)]
        public void HtmlExportOptions_ColorScheme_CanBeSet(ColorScheme scheme)
        {
            var options = new HtmlExportOptions
            {
                ColorScheme = scheme
            };

            Assert.Equal(scheme, options.ColorScheme);
        }

        [Fact]
        public void PdfExportOptions_Default_HasCorrectValues()
        {
            var options = PdfExportOptions.Default;

            Assert.Equal(ColorScheme.Default, options.ColorScheme);
            Assert.True(options.IncludeFolderTree);
            Assert.True(options.IncludeStatistics);
        }

        [Fact]
        public void PdfExportOptions_Compact_HasCompactSettings()
        {
            var options = PdfExportOptions.Compact;

            Assert.False(options.UseEmojis);
            Assert.False(options.IncludeStatistics);
            Assert.False(options.IncludeTableOfContents);
            Assert.Equal(3, options.MaxTreeDepth);
        }

        [Fact]
        public void ColorSchemeHelper_GetColors_ReturnsCorrectColors()
        {
            var (primary, secondary, accent) = ColorSchemeHelper.GetColors(ColorScheme.Blue);

            Assert.Equal("#3182ce", primary);
            Assert.Equal("#2c5282", secondary);
            Assert.Equal("#4299e1", accent);
        }
    }
}
