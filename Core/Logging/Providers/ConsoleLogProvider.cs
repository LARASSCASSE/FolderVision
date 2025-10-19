using System;

namespace FolderVision.Core.Logging.Providers
{
    /// <summary>
    /// Log provider that writes to the console with color coding
    /// </summary>
    public class ConsoleLogProvider : ILogProvider
    {
        private readonly bool _useColors;
        private readonly bool _useStructuredFormat;
        private readonly object _lock = new object();

        public ConsoleLogProvider(bool useColors = true, bool useStructuredFormat = false)
        {
            _useColors = useColors;
            _useStructuredFormat = useStructuredFormat;
        }

        public void Write(LogEntry entry)
        {
            if (entry == null)
                return;

            lock (_lock)
            {
                var originalColor = Console.ForegroundColor;

                try
                {
                    if (_useColors)
                    {
                        Console.ForegroundColor = GetColorForLevel(entry.Level);
                    }

                    var message = _useStructuredFormat ? entry.ToStructuredString() : entry.ToString();
                    Console.WriteLine(message);
                }
                finally
                {
                    if (_useColors)
                    {
                        Console.ForegroundColor = originalColor;
                    }
                }
            }
        }

        public void Flush()
        {
            // Console doesn't need flushing
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        private ConsoleColor GetColorForLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
        }
    }
}
