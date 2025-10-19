using System;
using System.Collections.Generic;

namespace FolderVision.Core.Logging
{
    /// <summary>
    /// Main logger implementation with multiple providers support
    /// </summary>
    public class Logger : ILogger, IDisposable
    {
        private readonly string _category;
        private readonly LogLevel _minLevel;
        private readonly List<ILogProvider> _providers;
        private readonly object _lock = new object();
        private string? _correlationId;

        public Logger(string category, LogLevel minLevel = LogLevel.Info)
        {
            _category = category ?? "Default";
            _minLevel = minLevel;
            _providers = new List<ILogProvider>();
        }

        /// <summary>
        /// Adds a log provider (console, file, etc.)
        /// </summary>
        public void AddProvider(ILogProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            lock (_lock)
            {
                _providers.Add(provider);
            }
        }

        /// <summary>
        /// Removes a log provider
        /// </summary>
        public void RemoveProvider(ILogProvider provider)
        {
            if (provider == null)
                return;

            lock (_lock)
            {
                _providers.Remove(provider);
            }
        }

        /// <summary>
        /// Sets a correlation ID for tracking related log entries
        /// </summary>
        public void SetCorrelationId(string? correlationId)
        {
            _correlationId = correlationId;
        }

        public void Log(LogLevel level, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
        {
            if (!IsEnabled(level))
                return;

            var entry = new LogEntry(level, _category, message)
            {
                Exception = exception,
                Properties = properties,
                CorrelationId = _correlationId
            };

            lock (_lock)
            {
                foreach (var provider in _providers)
                {
                    try
                    {
                        provider.Write(entry);
                    }
                    catch
                    {
                        // Don't let logging errors crash the application
                    }
                }
            }
        }

        public void Debug(string message, Dictionary<string, object>? properties = null)
        {
            Log(LogLevel.Debug, message, null, properties);
        }

        public void Info(string message, Dictionary<string, object>? properties = null)
        {
            Log(LogLevel.Info, message, null, properties);
        }

        public void Warning(string message, Dictionary<string, object>? properties = null)
        {
            Log(LogLevel.Warning, message, null, properties);
        }

        public void Error(string message, Exception? exception = null, Dictionary<string, object>? properties = null)
        {
            Log(LogLevel.Error, message, exception, properties);
        }

        public void Critical(string message, Exception? exception = null, Dictionary<string, object>? properties = null)
        {
            Log(LogLevel.Critical, message, exception, properties);
        }

        public bool IsEnabled(LogLevel level)
        {
            return level >= _minLevel && level != LogLevel.None;
        }

        /// <summary>
        /// Flushes all providers
        /// </summary>
        public void Flush()
        {
            lock (_lock)
            {
                foreach (var provider in _providers)
                {
                    try
                    {
                        provider.Flush();
                    }
                    catch
                    {
                        // Ignore flush errors
                    }
                }
            }
        }

        public void Dispose()
        {
            Flush();

            lock (_lock)
            {
                foreach (var provider in _providers)
                {
                    try
                    {
                        provider.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }
                _providers.Clear();
            }
        }
    }
}
