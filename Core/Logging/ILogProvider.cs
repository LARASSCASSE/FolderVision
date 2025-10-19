namespace FolderVision.Core.Logging
{
    /// <summary>
    /// Interface for log output providers (console, file, etc.)
    /// </summary>
    public interface ILogProvider
    {
        /// <summary>
        /// Writes a log entry to the output
        /// </summary>
        void Write(LogEntry entry);

        /// <summary>
        /// Flushes any buffered log entries
        /// </summary>
        void Flush();

        /// <summary>
        /// Disposes resources used by the provider
        /// </summary>
        void Dispose();
    }
}
