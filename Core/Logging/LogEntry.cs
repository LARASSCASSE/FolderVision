using System;

namespace FolderVision.Core.Logging
{
    /// <summary>
    /// Represents a single log entry with structured information
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Timestamp when the log entry was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Severity level of the log entry
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Category or source of the log entry (e.g., "ScanEngine", "HtmlExporter")
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The log message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Optional exception associated with this log entry
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Thread ID that generated the log entry
        /// </summary>
        public int ThreadId { get; set; }

        /// <summary>
        /// Optional correlation ID for tracking related log entries
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Additional context properties (e.g., file path, folder count, etc.)
        /// </summary>
        public Dictionary<string, object>? Properties { get; set; }

        public LogEntry(LogLevel level, string category, string message)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Category = category ?? "Unknown";
            Message = message ?? string.Empty;
            ThreadId = Environment.CurrentManagedThreadId;
        }

        /// <summary>
        /// Formats the log entry as a readable string
        /// </summary>
        public override string ToString()
        {
            var exceptionInfo = Exception != null ? $" | Exception: {Exception.GetType().Name}: {Exception.Message}" : "";
            var correlationInfo = !string.IsNullOrEmpty(CorrelationId) ? $" | CorrelationId: {CorrelationId}" : "";

            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level,-8}] [{Category}] {Message}{correlationInfo}{exceptionInfo}";
        }

        /// <summary>
        /// Formats the log entry as a JSON-like structure
        /// </summary>
        public string ToStructuredString()
        {
            var parts = new System.Text.StringBuilder();
            parts.Append($"{{\"timestamp\":\"{Timestamp:O}\",");
            parts.Append($"\"level\":\"{Level}\",");
            parts.Append($"\"category\":\"{Category}\",");
            parts.Append($"\"message\":\"{EscapeJson(Message)}\",");
            parts.Append($"\"threadId\":{ThreadId}");

            if (!string.IsNullOrEmpty(CorrelationId))
            {
                parts.Append($",\"correlationId\":\"{CorrelationId}\"");
            }

            if (Exception != null)
            {
                parts.Append($",\"exception\":{{\"type\":\"{Exception.GetType().Name}\",\"message\":\"{EscapeJson(Exception.Message)}\"}}");
            }

            if (Properties != null && Properties.Count > 0)
            {
                parts.Append(",\"properties\":{");
                var first = true;
                foreach (var kvp in Properties)
                {
                    if (!first) parts.Append(",");
                    parts.Append($"\"{kvp.Key}\":\"{EscapeJson(kvp.Value?.ToString() ?? "")}\"");
                    first = false;
                }
                parts.Append("}");
            }

            parts.Append("}");
            return parts.ToString();
        }

        private static string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
