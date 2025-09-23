using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FolderVision.Models;

namespace FolderVision.Exporters
{
    public class HtmlExporter
    {
        public HtmlExporter()
        {
        }

        public async Task ExportAsync(ScanResult scanResult, string outputPath)
        {
            throw new NotImplementedException();
        }

        public string GenerateHtml(ScanResult scanResult)
        {
            throw new NotImplementedException();
        }

        private string GenerateHeader(ScanResult scanResult)
        {
            throw new NotImplementedException();
        }

        private string GenerateSummary(ScanResult scanResult)
        {
            throw new NotImplementedException();
        }

        private string GenerateFolderTree(ScanResult scanResult)
        {
            throw new NotImplementedException();
        }

        private string GenerateFolderHtml(FolderInfo folder, int depth = 0)
        {
            throw new NotImplementedException();
        }

        private string GenerateStylesheet()
        {
            throw new NotImplementedException();
        }

        private string GenerateJavaScript()
        {
            throw new NotImplementedException();
        }

        private string FormatFileSize(long bytes)
        {
            throw new NotImplementedException();
        }

        private string EscapeHtml(string text)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<ExportProgressEventArgs>? ExportProgress;
    }

    public class ExportProgressEventArgs : EventArgs
    {
        public int PercentComplete { get; set; }
        public string CurrentItem { get; set; } = string.Empty;
        public long ProcessedItems { get; set; }
        public long TotalItems { get; set; }
    }
}