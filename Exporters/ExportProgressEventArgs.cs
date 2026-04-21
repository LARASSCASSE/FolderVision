using System;

namespace FolderVision.Exporters
{
    /// <summary>
    /// Progress information for export operations.
    /// </summary>
    public class ExportProgressEventArgs : EventArgs
    {
        public int    PercentComplete { get; init; }
        public string CurrentItem    { get; init; } = string.Empty;
        public int    ProcessedItems { get; init; }
        public int    TotalItems     { get; init; }
    }
}
