using System;

namespace FolderVision.Utils
{
    /// <summary>
    /// Formatting options for file size display
    /// </summary>
    public class FileSizeFormattingOptions
    {
        /// <summary>
        /// Language/locale for file size units
        /// </summary>
        public FileSizeLocale Locale { get; set; } = FileSizeLocale.English;

        /// <summary>
        /// Unit system (binary 1024-based vs decimal 1000-based)
        /// </summary>
        public FileSizeUnitSystem UnitSystem { get; set; } = FileSizeUnitSystem.Binary;

        /// <summary>
        /// Number of decimal places to show
        /// </summary>
        public int DecimalPlaces { get; set; } = 2;

        /// <summary>
        /// Whether to include space between number and unit
        /// </summary>
        public bool IncludeSpace { get; set; } = true;

        /// <summary>
        /// Default formatting options (English, Binary, 2 decimals)
        /// </summary>
        public static FileSizeFormattingOptions Default => new FileSizeFormattingOptions();

        /// <summary>
        /// French formatting options
        /// </summary>
        public static FileSizeFormattingOptions French => new FileSizeFormattingOptions
        {
            Locale = FileSizeLocale.French,
            UnitSystem = FileSizeUnitSystem.Binary,
            DecimalPlaces = 2
        };

        /// <summary>
        /// Decimal (SI) units formatting
        /// </summary>
        public static FileSizeFormattingOptions Decimal => new FileSizeFormattingOptions
        {
            Locale = FileSizeLocale.English,
            UnitSystem = FileSizeUnitSystem.Decimal,
            DecimalPlaces = 2
        };
    }

    /// <summary>
    /// Locale for file size unit names
    /// </summary>
    public enum FileSizeLocale
    {
        English,
        French
    }

    /// <summary>
    /// Unit system for file size calculation
    /// </summary>
    public enum FileSizeUnitSystem
    {
        /// <summary>
        /// Binary units (1024-based): KB, MB, GB (IEC standard: KiB, MiB, GiB)
        /// </summary>
        Binary,

        /// <summary>
        /// Decimal units (1000-based): KB, MB, GB (SI standard)
        /// </summary>
        Decimal
    }

    /// <summary>
    /// Utility class for formatting file sizes with internationalization support
    /// </summary>
    public static class FileSizeFormatter
    {
        private static readonly string[][] UnitsByLocale = new[]
        {
            // English
            new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB" },
            // French
            new[] { "o", "Ko", "Mo", "Go", "To", "Po", "Eo" }
        };

        private static readonly string[][] DecimalUnitsByLocale = new[]
        {
            // English (SI)
            new[] { "B", "kB", "MB", "GB", "TB", "PB", "EB" },
            // French (SI)
            new[] { "o", "ko", "Mo", "Go", "To", "Po", "Eo" }
        };

        /// <summary>
        /// Formats a file size in bytes to a human-readable string
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <param name="options">Formatting options (null for default)</param>
        /// <returns>Formatted file size string</returns>
        public static string Format(long bytes, FileSizeFormattingOptions? options = null)
        {
            options ??= FileSizeFormattingOptions.Default;

            if (bytes == 0)
            {
                var zeroUnit = GetUnitName(0, options);
                return FormatValue(0, zeroUnit, options);
            }

            if (bytes < 0)
            {
                return $"-{Format(-bytes, options)}";
            }

            var divisor = options.UnitSystem == FileSizeUnitSystem.Binary ? 1024.0 : 1000.0;
            var unitIndex = 0;
            var size = (double)bytes;

            // Find the appropriate unit
            var maxUnits = GetUnitNames(options).Length;
            while (size >= divisor && unitIndex < maxUnits - 1)
            {
                size /= divisor;
                unitIndex++;
            }

            var unit = GetUnitName(unitIndex, options);
            return FormatValue(size, unit, options);
        }

        /// <summary>
        /// Formats a file size with default English binary units
        /// </summary>
        public static string FormatDefault(long bytes)
        {
            return Format(bytes, FileSizeFormattingOptions.Default);
        }

        /// <summary>
        /// Formats a file size with French units
        /// </summary>
        public static string FormatFrench(long bytes)
        {
            return Format(bytes, FileSizeFormattingOptions.French);
        }

        /// <summary>
        /// Formats a file size with decimal (SI) units
        /// </summary>
        public static string FormatDecimal(long bytes)
        {
            return Format(bytes, FileSizeFormattingOptions.Decimal);
        }

        private static string FormatValue(double size, string unit, FileSizeFormattingOptions options)
        {
            var formatString = $"{{0:F{options.DecimalPlaces}}}";
            var formattedNumber = string.Format(formatString, size);
            var separator = options.IncludeSpace ? " " : "";
            return $"{formattedNumber}{separator}{unit}";
        }

        private static string[] GetUnitNames(FileSizeFormattingOptions options)
        {
            var localeIndex = (int)options.Locale;
            return options.UnitSystem == FileSizeUnitSystem.Binary
                ? UnitsByLocale[localeIndex]
                : DecimalUnitsByLocale[localeIndex];
        }

        private static string GetUnitName(int unitIndex, FileSizeFormattingOptions options)
        {
            var units = GetUnitNames(options);
            return unitIndex < units.Length ? units[unitIndex] : units[units.Length - 1];
        }

        /// <summary>
        /// Tries to parse a formatted file size string back to bytes
        /// </summary>
        /// <param name="formattedSize">The formatted size string (e.g., "1.5 MB")</param>
        /// <param name="bytes">The parsed size in bytes</param>
        /// <param name="options">Formatting options used (null for auto-detect)</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParse(string formattedSize, out long bytes, FileSizeFormattingOptions? options = null)
        {
            bytes = 0;
            if (string.IsNullOrWhiteSpace(formattedSize))
                return false;

            options ??= FileSizeFormattingOptions.Default;

            // Remove spaces and parse
            var trimmed = formattedSize.Trim();
            var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0 || parts.Length > 2)
                return false;

            if (!double.TryParse(parts[0], out var value))
                return false;

            if (parts.Length == 1)
            {
                // Assume bytes
                bytes = (long)value;
                return true;
            }

            var unit = parts[1];
            var units = GetUnitNames(options);
            var unitIndex = Array.IndexOf(units, unit);

            if (unitIndex < 0)
            {
                // Try other unit system or locale
                return false;
            }

            var divisor = options.UnitSystem == FileSizeUnitSystem.Binary ? 1024.0 : 1000.0;
            var multiplier = Math.Pow(divisor, unitIndex);
            bytes = (long)(value * multiplier);

            return true;
        }
    }
}
