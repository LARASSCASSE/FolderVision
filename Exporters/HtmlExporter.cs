using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using FolderVision.Models;

namespace FolderVision.Exporters
{
    public class HtmlExporter
    {
        private int _currentItem = 0;
        private int _totalItems = 0;

        public HtmlExporter()
        {
        }

        public async Task ExportAsync(ScanResult scanResult, string outputPath = "")
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var folderName = scanResult.RootFolders.FirstOrDefault()?.Name ?? "Unknown";
                if (folderName.Contains(':'))
                {
                    folderName = folderName.Replace(":", "").Replace("\\", "");
                }
                outputPath = Path.Combine(desktop, $"FolderScan_{timestamp}_{folderName}.html");
            }

            _totalItems = scanResult.GetAllFolders().Count();
            _currentItem = 0;

            var html = GenerateHtml(scanResult);
            await File.WriteAllTextAsync(outputPath, html, Encoding.UTF8);
        }

        public string GenerateHtml(ScanResult scanResult)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine("    <title>Folder Scan Report</title>");
            html.AppendLine("    <style>");
            html.AppendLine(GenerateStylesheet());
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine(GenerateHeader(scanResult));
            html.AppendLine(GenerateSummary(scanResult));
            html.AppendLine(GenerateFolderTree(scanResult));

            html.AppendLine("    <script>");
            html.AppendLine(GenerateJavaScript());
            html.AppendLine("    </script>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private string GenerateHeader(ScanResult scanResult)
        {
            var header = new StringBuilder();
            header.AppendLine("    <header class=\"header\">");
            header.AppendLine("        <h1>📁 Folder Scan Report</h1>");
            header.AppendLine($"        <div class=\"scan-info\">");
            header.AppendLine($"            <div class=\"info-item\">");
            header.AppendLine($"                <strong>Scan Date:</strong> {scanResult.ScanStartTime:yyyy-MM-dd HH:mm:ss}");
            header.AppendLine($"            </div>");
            header.AppendLine($"            <div class=\"info-item\">");
            header.AppendLine($"                <strong>Duration:</strong> {scanResult.ScanDuration.TotalSeconds:F2} seconds");
            header.AppendLine($"            </div>");
            header.AppendLine($"            <div class=\"info-item\">");
            header.AppendLine($"                <strong>Scanned Paths:</strong> {string.Join(", ", scanResult.ScannedPaths)}");
            header.AppendLine($"            </div>");
            header.AppendLine($"        </div>");
            header.AppendLine("    </header>");
            return header.ToString();
        }

        private string GenerateSummary(ScanResult scanResult)
        {
            var summary = new StringBuilder();
            summary.AppendLine("    <section class=\"summary\">");
            summary.AppendLine("        <h2>📊 Scan Statistics</h2>");
            summary.AppendLine("        <div class=\"stats-grid\">");
            summary.AppendLine("            <div class=\"stat-card\">");
            summary.AppendLine("                <div class=\"stat-icon\">📁</div>");
            summary.AppendLine("                <div class=\"stat-content\">");
            summary.AppendLine($"                    <div class=\"stat-number\">{scanResult.TotalFolders:N0}</div>");
            summary.AppendLine("                    <div class=\"stat-label\">Total Folders</div>");
            summary.AppendLine("                </div>");
            summary.AppendLine("            </div>");
            summary.AppendLine("            <div class=\"stat-card\">");
            summary.AppendLine("                <div class=\"stat-icon\">📄</div>");
            summary.AppendLine("                <div class=\"stat-content\">");
            summary.AppendLine($"                    <div class=\"stat-number\">{scanResult.TotalFiles:N0}</div>");
            summary.AppendLine("                    <div class=\"stat-label\">Total Files</div>");
            summary.AppendLine("                </div>");
            summary.AppendLine("            </div>");
            summary.AppendLine("            <div class=\"stat-card\">");
            summary.AppendLine("                <div class=\"stat-icon\">⏱️</div>");
            summary.AppendLine("                <div class=\"stat-content\">");
            summary.AppendLine($"                    <div class=\"stat-number\">{scanResult.ScanDuration.TotalSeconds:F1}s</div>");
            summary.AppendLine("                    <div class=\"stat-label\">Scan Duration</div>");
            summary.AppendLine("                </div>");
            summary.AppendLine("            </div>");
            summary.AppendLine("        </div>");
            summary.AppendLine("    </section>");
            return summary.ToString();
        }

        private string GenerateFolderTree(ScanResult scanResult)
        {
            var tree = new StringBuilder();
            tree.AppendLine("    <section class=\"folder-tree\">");
            tree.AppendLine("        <h2>🌳 Folder Structure</h2>");
            tree.AppendLine("        <div class=\"tree-container\">");

            foreach (var rootFolder in scanResult.RootFolders)
            {
                tree.AppendLine(GenerateFolderHtml(rootFolder, 0, true));
            }

            tree.AppendLine("        </div>");
            tree.AppendLine("    </section>");
            return tree.ToString();
        }

        private string GenerateFolderHtml(FolderInfo folder, int depth = 0, bool isRoot = false)
        {
            ReportProgress(folder.Name);

            var html = new StringBuilder();
            var indent = new string(' ', depth * 4);
            var hasSubFolders = folder.SubFolders.Count > 0;
            var folderId = $"folder_{folder.FullPath.GetHashCode()}".Replace("-", "n");

            html.AppendLine($"{indent}<div class=\"folder-item\" data-depth=\"{depth}\">");

            if (hasSubFolders)
            {
                html.AppendLine($"{indent}    <span class=\"toggle\" onclick=\"toggleFolder('{folderId}')\" id=\"toggle_{folderId}\">▶</span>");
            }
            else
            {
                html.AppendLine($"{indent}    <span class=\"no-toggle\"></span>");
            }

            var displayName = isRoot ? folder.FullPath : folder.Name;
            html.AppendLine($"{indent}    <span class=\"folder-icon\">📁</span>");
            html.AppendLine($"{indent}    <span class=\"folder-name\" title=\"{EscapeHtml(folder.FullPath)}\">{EscapeHtml(displayName)}</span>");
            html.AppendLine($"{indent}    <span class=\"folder-stats\">(📁{folder.SubFolderCount} | 📄{folder.FileCount})</span>");

            if (hasSubFolders)
            {
                html.AppendLine($"{indent}    <div class=\"subfolder-container\" id=\"{folderId}\" style=\"display: none;\">");
                foreach (var subFolder in folder.SubFolders.OrderBy(f => f.Name))
                {
                    html.AppendLine(GenerateFolderHtml(subFolder, depth + 1));
                }
                html.AppendLine($"{indent}    </div>");
            }

            html.AppendLine($"{indent}</div>");

            return html.ToString();
        }

        private string GenerateStylesheet()
        {
            return @"
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f8f9fa;
            margin: 0;
            padding: 20px;
        }

        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 2rem;
            border-radius: 12px;
            margin-bottom: 2rem;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }

        .header h1 {
            font-size: 2.5rem;
            margin-bottom: 1rem;
            font-weight: 700;
        }

        .scan-info {
            display: flex;
            flex-wrap: wrap;
            gap: 1.5rem;
            margin-top: 1rem;
        }

        .info-item {
            background: rgba(255, 255, 255, 0.1);
            padding: 0.75rem 1rem;
            border-radius: 8px;
            backdrop-filter: blur(10px);
        }

        .summary {
            background: white;
            padding: 2rem;
            border-radius: 12px;
            margin-bottom: 2rem;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        }

        .summary h2 {
            font-size: 1.8rem;
            margin-bottom: 1.5rem;
            color: #2d3748;
        }

        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 1.5rem;
        }

        .stat-card {
            display: flex;
            align-items: center;
            background: #f7fafc;
            padding: 1.5rem;
            border-radius: 10px;
            border-left: 4px solid #667eea;
            transition: transform 0.2s ease;
        }

        .stat-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
        }

        .stat-icon {
            font-size: 2rem;
            margin-right: 1rem;
        }

        .stat-number {
            font-size: 2rem;
            font-weight: 700;
            color: #667eea;
        }

        .stat-label {
            font-size: 0.9rem;
            color: #718096;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        .folder-tree {
            background: white;
            padding: 2rem;
            border-radius: 12px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        }

        .folder-tree h2 {
            font-size: 1.8rem;
            margin-bottom: 1.5rem;
            color: #2d3748;
        }

        .tree-container {
            font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
            font-size: 14px;
            line-height: 1.6;
        }

        .folder-item {
            display: flex;
            align-items: center;
            padding: 4px 0;
            border-radius: 4px;
            transition: background-color 0.2s ease;
        }

        .folder-item:hover {
            background-color: #f7fafc;
        }

        .toggle {
            cursor: pointer;
            width: 20px;
            text-align: center;
            user-select: none;
            transition: transform 0.2s ease;
            color: #667eea;
            font-weight: bold;
        }

        .toggle:hover {
            color: #553c9a;
        }

        .no-toggle {
            width: 20px;
        }

        .folder-icon {
            margin: 0 8px;
            font-size: 16px;
        }

        .folder-name {
            color: #2d3748;
            font-weight: 500;
            margin-right: 8px;
        }

        .folder-stats {
            color: #718096;
            font-size: 0.9em;
            margin-left: auto;
        }

        .subfolder-container {
            width: 100%;
        }

        .subfolder-container .folder-item {
            margin-left: 20px;
            position: relative;
        }

        .subfolder-container .folder-item::before {
            content: '';
            position: absolute;
            left: -20px;
            top: 0;
            bottom: 50%;
            width: 1px;
            background-color: #e2e8f0;
        }

        .subfolder-container .folder-item:last-child::before {
            bottom: 100%;
        }

        .subfolder-container .folder-item::after {
            content: '';
            position: absolute;
            left: -20px;
            top: 50%;
            width: 16px;
            height: 1px;
            background-color: #e2e8f0;
        }

        @media print {
            body {
                background: white;
                padding: 0;
            }

            .header {
                background: #667eea !important;
                -webkit-print-color-adjust: exact;
                color-adjust: exact;
            }

            .stat-card {
                break-inside: avoid;
            }

            .folder-item {
                break-inside: avoid;
            }

            .toggle {
                display: none;
            }

            .subfolder-container {
                display: block !important;
            }
        }

        @media (max-width: 768px) {
            body {
                padding: 10px;
            }

            .header {
                padding: 1.5rem;
            }

            .header h1 {
                font-size: 2rem;
            }

            .scan-info {
                flex-direction: column;
                gap: 1rem;
            }

            .stats-grid {
                grid-template-columns: 1fr;
            }

            .tree-container {
                font-size: 12px;
            }
        }
            ";
        }

        private string GenerateJavaScript()
        {
            return @"
        function toggleFolder(folderId) {
            const container = document.getElementById(folderId);
            const toggle = document.getElementById('toggle_' + folderId);

            if (container.style.display === 'none' || container.style.display === '') {
                container.style.display = 'block';
                toggle.textContent = '▼';
                toggle.style.transform = 'rotate(90deg)';
            } else {
                container.style.display = 'none';
                toggle.textContent = '▶';
                toggle.style.transform = 'rotate(0deg)';
            }
        }

        function expandAll() {
            const containers = document.querySelectorAll('.subfolder-container');
            const toggles = document.querySelectorAll('.toggle');

            containers.forEach(container => {
                container.style.display = 'block';
            });

            toggles.forEach(toggle => {
                toggle.textContent = '▼';
                toggle.style.transform = 'rotate(90deg)';
            });
        }

        function collapseAll() {
            const containers = document.querySelectorAll('.subfolder-container');
            const toggles = document.querySelectorAll('.toggle');

            containers.forEach(container => {
                container.style.display = 'none';
            });

            toggles.forEach(toggle => {
                toggle.textContent = '▶';
                toggle.style.transform = 'rotate(0deg)';
            });
        }

        document.addEventListener('DOMContentLoaded', function() {
            const header = document.querySelector('.folder-tree h2');
            if (header) {
                header.innerHTML += `
                    <div style='float: right; margin-top: -5px;'>
                        <button onclick='expandAll()' style='margin-right: 10px; padding: 5px 10px; background: #667eea; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 12px;'>Expand All</button>
                        <button onclick='collapseAll()' style='padding: 5px 10px; background: #718096; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 12px;'>Collapse All</button>
                    </div>
                `;
            }
        });
            ";
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";

            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1} {suffixes[suffixIndex]}";
        }

        private string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        private void ReportProgress(string currentItem)
        {
            _currentItem++;
            var percent = _totalItems > 0 ? (_currentItem * 100) / _totalItems : 0;
            ExportProgress?.Invoke(this, new ExportProgressEventArgs
            {
                PercentComplete = percent,
                CurrentItem = currentItem,
                ProcessedItems = _currentItem,
                TotalItems = _totalItems
            });
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