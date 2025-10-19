using System;
using System.IO;
using System.Text;

namespace FolderVision.Core.Logging.Providers
{
    /// <summary>
    /// Log provider that writes to a file with automatic rotation
    /// </summary>
    public class FileLogProvider : ILogProvider, IDisposable
    {
        private readonly string _logDirectory;
        private readonly string _logFilePrefix;
        private readonly long _maxFileSizeBytes;
        private readonly int _maxFileCount;
        private readonly bool _useStructuredFormat;
        private readonly object _lock = new object();
        private StreamWriter? _writer;
        private string? _currentLogFile;
        private long _currentFileSize;

        /// <summary>
        /// Creates a new file log provider
        /// </summary>
        /// <param name="logDirectory">Directory where log files will be stored</param>
        /// <param name="logFilePrefix">Prefix for log file names (default: "FolderVision")</param>
        /// <param name="maxFileSizeMB">Maximum size of a single log file in MB (default: 10MB)</param>
        /// <param name="maxFileCount">Maximum number of log files to keep (default: 5)</param>
        /// <param name="useStructuredFormat">Whether to use JSON-like structured format</param>
        public FileLogProvider(
            string? logDirectory = null,
            string logFilePrefix = "FolderVision",
            int maxFileSizeMB = 10,
            int maxFileCount = 5,
            bool useStructuredFormat = false)
        {
            _logDirectory = logDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FolderVision", "Logs");

            _logFilePrefix = logFilePrefix;
            _maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
            _maxFileCount = maxFileCount;
            _useStructuredFormat = useStructuredFormat;

            // Ensure log directory exists
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            InitializeLogFile();
        }

        public void Write(LogEntry entry)
        {
            if (entry == null)
                return;

            lock (_lock)
            {
                try
                {
                    // Check if we need to rotate the log file
                    if (_currentFileSize >= _maxFileSizeBytes)
                    {
                        RotateLogFile();
                    }

                    var message = _useStructuredFormat ? entry.ToStructuredString() : entry.ToString();
                    _writer?.WriteLine(message);

                    // Update file size estimate
                    _currentFileSize += Encoding.UTF8.GetByteCount(message) + Environment.NewLine.Length;
                }
                catch
                {
                    // Don't let logging errors crash the application
                }
            }
        }

        public void Flush()
        {
            lock (_lock)
            {
                try
                {
                    _writer?.Flush();
                }
                catch
                {
                    // Ignore flush errors
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _writer?.Flush();
                _writer?.Dispose();
                _writer = null;
            }
        }

        private void InitializeLogFile()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _currentLogFile = Path.Combine(_logDirectory, $"{_logFilePrefix}_{timestamp}.log");

            _writer = new StreamWriter(_currentLogFile, append: true, Encoding.UTF8)
            {
                AutoFlush = true
            };

            _currentFileSize = 0;

            // Write header
            _writer.WriteLine($"=== FolderVision Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            _writer.WriteLine();
        }

        private void RotateLogFile()
        {
            // Close current file
            _writer?.Flush();
            _writer?.Dispose();

            // Clean up old log files
            CleanupOldLogFiles();

            // Create new log file
            InitializeLogFile();
        }

        private void CleanupOldLogFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, $"{_logFilePrefix}_*.log")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();

                // Keep only the most recent files (up to _maxFileCount)
                var filesToDelete = logFiles.Skip(_maxFileCount - 1); // -1 because we're about to create a new one

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// Gets the current log file path
        /// </summary>
        public string? CurrentLogFile => _currentLogFile;

        /// <summary>
        /// Gets the log directory path
        /// </summary>
        public string LogDirectory => _logDirectory;
    }
}
