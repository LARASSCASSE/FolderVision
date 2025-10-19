using System;
using FolderVision.Utils;

namespace FolderVision.Models
{
    /// <summary>
    /// Configuration options for HTML export customization
    /// </summary>
    public class HtmlExportOptions
    {
        /// <summary>
        /// Color scheme for the HTML export
        /// </summary>
        public ColorScheme ColorScheme { get; set; } = ColorScheme.Default;

        /// <summary>
        /// Custom primary color (overrides color scheme if set)
        /// </summary>
        public string? CustomPrimaryColor { get; set; }

        /// <summary>
        /// Custom secondary color (overrides color scheme if set)
        /// </summary>
        public string? CustomSecondaryColor { get; set; }

        /// <summary>
        /// Whether to include the folder tree section
        /// </summary>
        public bool IncludeFolderTree { get; set; } = true;

        /// <summary>
        /// Whether to include the statistics section
        /// </summary>
        public bool IncludeStatistics { get; set; } = true;

        /// <summary>
        /// Whether to include the header with scan info
        /// </summary>
        public bool IncludeHeader { get; set; } = true;

        /// <summary>
        /// Whether to use emojis in the report
        /// </summary>
        public bool UseEmojis { get; set; } = true;

        /// <summary>
        /// Custom title for the report (null uses default)
        /// </summary>
        public string? CustomTitle { get; set; }

        /// <summary>
        /// File size formatting options
        /// </summary>
        public FileSizeFormattingOptions FileSizeFormat { get; set; } = FileSizeFormattingOptions.Default;

        /// <summary>
        /// Maximum depth to show in folder tree (0 = unlimited)
        /// </summary>
        public int MaxTreeDepth { get; set; } = 0;

        /// <summary>
        /// Whether to collapse all folders by default
        /// </summary>
        public bool CollapseByDefault { get; set; } = false;

        /// <summary>
        /// Font family for the report
        /// </summary>
        public string FontFamily { get; set; } = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";

        /// <summary>
        /// Default HTML export options
        /// </summary>
        public static HtmlExportOptions Default => new HtmlExportOptions();

        /// <summary>
        /// Compact HTML export (minimal sections, no emojis)
        /// </summary>
        public static HtmlExportOptions Compact => new HtmlExportOptions
        {
            UseEmojis = false,
            IncludeStatistics = false,
            MaxTreeDepth = 3,
            CollapseByDefault = true
        };

        /// <summary>
        /// French localized export
        /// </summary>
        public static HtmlExportOptions French => new HtmlExportOptions
        {
            FileSizeFormat = FileSizeFormattingOptions.French,
            CustomTitle = "Rapport de Scan de Dossiers"
        };
    }

    /// <summary>
    /// Configuration options for PDF export customization
    /// </summary>
    public class PdfExportOptions
    {
        /// <summary>
        /// Color scheme for the PDF export
        /// </summary>
        public ColorScheme ColorScheme { get; set; } = ColorScheme.Default;

        /// <summary>
        /// Whether to include the folder tree section
        /// </summary>
        public bool IncludeFolderTree { get; set; } = true;

        /// <summary>
        /// Whether to include the statistics section
        /// </summary>
        public bool IncludeStatistics { get; set; } = true;

        /// <summary>
        /// Whether to include the table of contents
        /// </summary>
        public bool IncludeTableOfContents { get; set; } = true;

        /// <summary>
        /// Whether to include the header with scan info
        /// </summary>
        public bool IncludeHeader { get; set; } = true;

        /// <summary>
        /// Whether to use emojis in the report
        /// </summary>
        public bool UseEmojis { get; set; } = true;

        /// <summary>
        /// Custom title for the report (null uses default)
        /// </summary>
        public string? CustomTitle { get; set; }

        /// <summary>
        /// File size formatting options
        /// </summary>
        public FileSizeFormattingOptions FileSizeFormat { get; set; } = FileSizeFormattingOptions.Default;

        /// <summary>
        /// Maximum depth to show in folder tree (0 = unlimited, recommended 5 for PDFs)
        /// </summary>
        public int MaxTreeDepth { get; set; } = 5;

        /// <summary>
        /// Font size for body text
        /// </summary>
        public int FontSize { get; set; } = 10;

        /// <summary>
        /// Whether to add page numbers
        /// </summary>
        public bool IncludePageNumbers { get; set; } = true;

        /// <summary>
        /// Default PDF export options
        /// </summary>
        public static PdfExportOptions Default => new PdfExportOptions();

        /// <summary>
        /// Compact PDF export (minimal sections, limited depth)
        /// </summary>
        public static PdfExportOptions Compact => new PdfExportOptions
        {
            UseEmojis = false,
            IncludeStatistics = false,
            IncludeTableOfContents = false,
            MaxTreeDepth = 3
        };

        /// <summary>
        /// French localized export
        /// </summary>
        public static PdfExportOptions French => new PdfExportOptions
        {
            FileSizeFormat = FileSizeFormattingOptions.French,
            CustomTitle = "Rapport de Scan de Dossiers"
        };

        /// <summary>
        /// Detailed PDF export (all sections, deeper tree)
        /// </summary>
        public static PdfExportOptions Detailed => new PdfExportOptions
        {
            IncludeFolderTree = true,
            IncludeStatistics = true,
            IncludeTableOfContents = true,
            MaxTreeDepth = 10
        };
    }

    /// <summary>
    /// Predefined color schemes for exports
    /// </summary>
    public enum ColorScheme
    {
        /// <summary>
        /// Default purple/blue gradient scheme
        /// </summary>
        Default,

        /// <summary>
        /// Blue color scheme
        /// </summary>
        Blue,

        /// <summary>
        /// Green color scheme
        /// </summary>
        Green,

        /// <summary>
        /// Red/orange color scheme
        /// </summary>
        Red,

        /// <summary>
        /// Dark theme
        /// </summary>
        Dark,

        /// <summary>
        /// Monochrome (grayscale)
        /// </summary>
        Monochrome
    }

    /// <summary>
    /// Helper for getting color scheme values
    /// </summary>
    public static class ColorSchemeHelper
    {
        public static (string Primary, string Secondary, string Accent) GetColors(ColorScheme scheme)
        {
            return scheme switch
            {
                ColorScheme.Default => ("#667eea", "#764ba2", "#667eea"),
                ColorScheme.Blue => ("#3182ce", "#2c5282", "#4299e1"),
                ColorScheme.Green => ("#38a169", "#2f855a", "#48bb78"),
                ColorScheme.Red => ("#e53e3e", "#c53030", "#fc8181"),
                ColorScheme.Dark => ("#2d3748", "#1a202c", "#4a5568"),
                ColorScheme.Monochrome => ("#4a5568", "#718096", "#a0aec0"),
                _ => ("#667eea", "#764ba2", "#667eea")
            };
        }

        public static string GetGradient(ColorScheme scheme)
        {
            var (primary, secondary, _) = GetColors(scheme);
            return $"linear-gradient(135deg, {primary} 0%, {secondary} 100%)";
        }
    }
}
