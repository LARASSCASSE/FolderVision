using System;
using System.Collections.Generic;

namespace FolderVision.Core.Logging
{
    /// <summary>
    /// Interface for structured logging
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message at the specified level
        /// </summary>
        void Log(LogLevel level, string message, Exception? exception = null, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Logs a debug message
        /// </summary>
        void Debug(string message, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Logs an informational message
        /// </summary>
        void Info(string message, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        void Warning(string message, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Logs an error message
        /// </summary>
        void Error(string message, Exception? exception = null, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Logs a critical error message
        /// </summary>
        void Critical(string message, Exception? exception = null, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Checks if a log level is enabled
        /// </summary>
        bool IsEnabled(LogLevel level);
    }
}
