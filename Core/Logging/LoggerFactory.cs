using System;
using System.Collections.Generic;
using FolderVision.Core.Logging.Providers;

namespace FolderVision.Core.Logging
{
    /// <summary>
    /// Factory for creating and managing loggers
    /// </summary>
    public static class LoggerFactory
    {
        private static readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>();
        private static readonly object _lock = new object();
        private static LogLevel _globalMinLevel = LogLevel.Info;
        private static readonly List<ILogProvider> _globalProviders = new List<ILogProvider>();

        /// <summary>
        /// Creates or retrieves a logger for the specified category
        /// </summary>
        public static Logger GetLogger(string category, LogLevel? minLevel = null)
        {
            lock (_lock)
            {
                if (!_loggers.TryGetValue(category, out var logger))
                {
                    logger = new Logger(category, minLevel ?? _globalMinLevel);

                    // Add all global providers to the new logger
                    foreach (var provider in _globalProviders)
                    {
                        logger.AddProvider(provider);
                    }

                    _loggers[category] = logger;
                }

                return logger;
            }
        }

        /// <summary>
        /// Creates a logger for a specific type
        /// </summary>
        public static Logger GetLogger<T>()
        {
            return GetLogger(typeof(T).Name);
        }

        /// <summary>
        /// Sets the global minimum log level for all new loggers
        /// </summary>
        public static void SetGlobalMinLevel(LogLevel level)
        {
            lock (_lock)
            {
                _globalMinLevel = level;
            }
        }

        /// <summary>
        /// Adds a global provider to all loggers (existing and new)
        /// </summary>
        public static void AddGlobalProvider(ILogProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            lock (_lock)
            {
                _globalProviders.Add(provider);

                // Add to all existing loggers
                foreach (var logger in _loggers.Values)
                {
                    logger.AddProvider(provider);
                }
            }
        }

        /// <summary>
        /// Configures default logging (console + file)
        /// </summary>
        public static void ConfigureDefault(
            LogLevel minLevel = LogLevel.Info,
            bool enableConsole = true,
            bool enableFile = true,
            string? logDirectory = null,
            bool useColors = true)
        {
            SetGlobalMinLevel(minLevel);

            if (enableConsole)
            {
                AddGlobalProvider(new ConsoleLogProvider(useColors));
            }

            if (enableFile)
            {
                AddGlobalProvider(new FileLogProvider(logDirectory));
            }
        }

        /// <summary>
        /// Flushes all loggers
        /// </summary>
        public static void FlushAll()
        {
            lock (_lock)
            {
                foreach (var logger in _loggers.Values)
                {
                    logger.Flush();
                }
            }
        }

        /// <summary>
        /// Disposes all loggers and clears the cache
        /// </summary>
        public static void DisposeAll()
        {
            lock (_lock)
            {
                foreach (var logger in _loggers.Values)
                {
                    logger.Dispose();
                }

                _loggers.Clear();

                foreach (var provider in _globalProviders)
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

                _globalProviders.Clear();
            }
        }
    }
}
