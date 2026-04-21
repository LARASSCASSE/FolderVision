using System.Collections.Generic;

namespace FolderVision.Models
{
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
}
