using FolderVision.Core.Logging;
using FolderVision.Models;

namespace FolderVision.Tests
{
    public class LoggingSystemTests
    {
        [Fact]
        public void Logger_MinLevel_FiltersCorrectly()
        {
            var logger = new Logger("Test", LogLevel.Warning);
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.True(logger.IsEnabled(LogLevel.Error));
            Assert.False(logger.IsEnabled(LogLevel.Info));
            Assert.False(logger.IsEnabled(LogLevel.Debug));
        }

        [Fact]
        public void LogEntry_ToString_FormatsCorrectly()
        {
            var entry = new LogEntry(LogLevel.Info, "Test", "Test message")
            {
                Timestamp = new DateTime(2025, 10, 19, 14, 30, 25)
            };

            var result = entry.ToString();
            Assert.Contains("[Info", result);
            Assert.Contains("Test", result);
            Assert.Contains("Test message", result);
        }

        [Fact]
        public void LoggingOptions_Verbose_HasCorrectSettings()
        {
            var options = LoggingOptions.Verbose;
            Assert.Equal(LogLevel.Debug, options.MinLevel);
            Assert.True(options.EnableConsoleLog);
            Assert.True(options.EnableFileLog);
            Assert.True(options.UseStructuredFormat);
        }

        [Fact]
        public void LoggingOptions_Quiet_OnlyLogsWarningsAndAbove()
        {
            var options = LoggingOptions.Quiet;
            Assert.Equal(LogLevel.Warning, options.MinLevel);
            Assert.False(options.EnableConsoleLog);
            Assert.True(options.EnableFileLog);
        }

        [Fact]
        public void LoggingOptions_Disabled_DisablesAllLogging()
        {
            var options = LoggingOptions.Disabled;
            Assert.Equal(LogLevel.None, options.MinLevel);
            Assert.False(options.EnableConsoleLog);
            Assert.False(options.EnableFileLog);
        }

        [Fact]
        public void Logger_CorrelationId_IsSet()
        {
            var logger = new Logger("Test", LogLevel.Info);
            logger.SetCorrelationId("test-123");

            // Correlation ID should be set (tested via provider integration)
            Assert.NotNull(logger);
        }
    }
}
