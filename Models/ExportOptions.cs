using System;
using System.Collections.Generic;
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
    /// Configuration options for PDF export customization.
    /// Only contains properties actually consumed by PdfExporter.
    /// </summary>
    public class PdfExportOptions
    {
        /// <summary>Whether to include the scan-info header on each page.</summary>
        public bool IncludeHeader { get; set; } = true;

        /// <summary>Custom title for the report (null uses auto-generated title).</summary>
        public string? CustomTitle { get; set; }

        /// <summary>Maximum depth to show in the folder tree (0 = unlimited).</summary>
        public int MaxTreeDepth { get; set; } = 8;

        /// <summary>Font size for body text.</summary>
        public int FontSize { get; set; } = 10;

        /// <summary>Whether to append a dedicated "Duplicate Folders" page.</summary>
        public bool IncludeDuplicates { get; set; } = true;

        /// <summary>
        /// Duplicate folder groups for the duplicate page.
        /// Key = folder name, Value = sorted list of full paths.
        /// Null or empty = no duplicate page rendered.
        /// </summary>
        public Dictionary<string, List<string>>? DuplicateGroups { get; set; }

        /// <summary>Default options.</summary>
        public static PdfExportOptions Default => new PdfExportOptions();

        /// <summary>Compact options: limited tree depth.</summary>
        public static PdfExportOptions Compact => new PdfExportOptions { MaxTreeDepth = 3 };

        /// <summary>French localized report title.</summary>
        public static PdfExportOptions French => new PdfExportOptions
        {
            CustomTitle = "Rapport de Scan de Dossiers"
        };

        /// <summary>Detailed options: deeper tree.</summary>
        public static PdfExportOptions Detailed => new PdfExportOptions { MaxTreeDepth = 10 };
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
