using FolderVision.Core.Logging;

namespace FolderVision.Models
{
    /// <summary>
    /// Configuration options for the logging system
    /// </summary>
    public class LoggingOptions
    {
        /// <summary>
        /// Minimum log level to record
        /// </summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// Enable console logging
        /// </summary>
        public bool EnableConsoleLog { get; set; } = true;

        /// <summary>
        /// Enable file logging
        /// </summary>
        public bool EnableFileLog { get; set; } = true;

        /// <summary>
        /// Use colors in console output
        /// </summary>
        public bool UseConsoleColors { get; set; } = true;

        /// <summary>
        /// Use structured (JSON-like) format for logs
        /// </summary>
        public bool UseStructuredFormat { get; set; } = false;

        /// <summary>
        /// Directory where log files will be stored (null = default AppData location)
        /// </summary>
        public string? LogDirectory { get; set; }

        /// <summary>
        /// Maximum size of a single log file in MB
        /// </summary>
        public int MaxLogFileSizeMB { get; set; } = 10;

        /// <summary>
        /// Maximum number of log files to keep (older files are deleted)
        /// </summary>
        public int MaxLogFileCount { get; set; } = 5;

        /// <summary>
        /// Prefix for log file names
        /// </summary>
        public string LogFilePrefix { get; set; } = "FolderVision";

        /// <summary>
        /// Default logging configuration (Info level, console + file)
        /// </summary>
        public static LoggingOptions Default => new LoggingOptions();

        /// <summary>
        /// Verbose logging (Debug level, console + file, structured format)
        /// </summary>
        public static LoggingOptions Verbose => new LoggingOptions
        {
            MinLevel = LogLevel.Debug,
            EnableConsoleLog = true,
            EnableFileLog = true,
            UseStructuredFormat = true
        };

        /// <summary>
        /// Quiet logging (Warning level and above, file only)
        /// </summary>
        public static LoggingOptions Quiet => new LoggingOptions
        {
            MinLevel = LogLevel.Warning,
            EnableConsoleLog = false,
            EnableFileLog = true
        };

        /// <summary>
        /// Console only logging (no file)
        /// </summary>
        public static LoggingOptions ConsoleOnly => new LoggingOptions
        {
            MinLevel = LogLevel.Info,
            EnableConsoleLog = true,
            EnableFileLog = false
        };

        /// <summary>
        /// File only logging (no console, good for background services)
        /// </summary>
        public static LoggingOptions FileOnly => new LoggingOptions
        {
            MinLevel = LogLevel.Info,
            EnableConsoleLog = false,
            EnableFileLog = true
        };

        /// <summary>
        /// Disabled logging
        /// </summary>
        public static LoggingOptions Disabled => new LoggingOptions
        {
            MinLevel = LogLevel.None,
            EnableConsoleLog = false,
            EnableFileLog = false
        };

        public LoggingOptions Clone()
        {
            return new LoggingOptions
            {
                MinLevel = MinLevel,
                EnableConsoleLog = EnableConsoleLog,
                EnableFileLog = EnableFileLog,
                UseConsoleColors = UseConsoleColors,
                UseStructuredFormat = UseStructuredFormat,
                LogDirectory = LogDirectory,
                MaxLogFileSizeMB = MaxLogFileSizeMB,
                MaxLogFileCount = MaxLogFileCount,
                LogFilePrefix = LogFilePrefix
            };
        }
    }
}
