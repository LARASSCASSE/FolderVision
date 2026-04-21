using FolderVision.Utils;
using FolderVision.Models;
using System.Globalization;

namespace FolderVision.Tests
{
    public class FileSizeFormatterTests
    {
        // Use current culture's decimal separator for tests
        private static readonly string Sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        [Theory]
        [InlineData(0, "0{0}00 B")]
        [InlineData(512, "512{0}00 B")]
        [InlineData(1024, "1{0}00 KB")]
        [InlineData(1536, "1{0}50 KB")]
        [InlineData(1048576, "1{0}00 MB")]
        [InlineData(1073741824, "1{0}00 GB")]
        [InlineData(1099511627776, "1{0}00 TB")]
        public void Format_BinaryUnits_ReturnsCorrectFormat(long bytes, string expectedTemplate)
        {
            var expected = string.Format(expectedTemplate, Sep);
            var result = FileSizeFormatter.Format(bytes);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1000, "1{0}00 kB")]
        [InlineData(1000000, "1{0}00 MB")]
        [InlineData(1000000000, "1{0}00 GB")]
        public void Format_DecimalUnits_ReturnsCorrectFormat(long bytes, string expectedTemplate)
        {
            var expected = string.Format(expectedTemplate, Sep);
            var result = FileSizeFormatter.FormatDecimal(bytes);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1024, "1,00 Ko")]
        [InlineData(1048576, "1,00 Mo")]
        [InlineData(1073741824, "1,00 Go")]
        public void Format_FrenchLocale_ReturnsCorrectFormat(long bytes, string expected)
        {
            var result = FileSizeFormatter.FormatFrench(bytes);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Format_NegativeBytes_ReturnsNegativeFormat()
        {
            var result = FileSizeFormatter.Format(-1024);
            Assert.StartsWith("-", result);
        }

        [Theory]
        [InlineData("1 KB", 1024)]
        [InlineData("2 GB", 2147483648)]
        public void TryParse_ValidInput_ReturnsTrue(string input, long expectedBytes)
        {
            var result = FileSizeFormatter.TryParse(input, out var bytes);
            Assert.True(result);
            Assert.Equal(expectedBytes, bytes);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("")]
        [InlineData(null)]
        public void TryParse_InvalidInput_ReturnsFalse(string? input)
        {
            var result = FileSizeFormatter.TryParse(input!, out var bytes);
            Assert.False(result);
            Assert.Equal(0, bytes);
        }
    }
}
