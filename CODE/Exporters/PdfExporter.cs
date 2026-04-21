using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using FolderVision.Models;
using FolderVision.Utils;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Layout.Borders;

namespace FolderVision.Exporters
{
    public class PdfExporter
    {
        private int _currentItem = 0;
        private int _totalItems = 0;
        private PdfFont? _regularFont;
        private PdfFont? _boldFont;
        private Document? _document;
        private readonly PdfExportOptions _options;

        // A4 content width (595pt page - 72pt margins) minus a small safety margin
        private const float TabRightEdge = 510f;
        private const float IndentPerDepth = 16f;

        private PdfFont RegularFont => _regularFont ?? throw new InvalidOperationException("PDF fonts not initialized");
        private PdfFont BoldFont    => _boldFont    ?? throw new InvalidOperationException("PDF fonts not initialized");
        private Document Document   => _document    ?? throw new InvalidOperationException("PDF document not initialized");

        public PdfExporter() : this(PdfExportOptions.Default) { }

        public PdfExporter(PdfExportOptions options)
        {
            _options = options ?? PdfExportOptions.Default;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a standalone PDF containing only the duplicate folders report.
        /// </summary>
        public async Task ExportDuplicatesAsync(string outputPath)
        {
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            await Task.Run(() =>
            {
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                var writer = new PdfWriter(fileStream);
                var pdf    = new PdfDocument(writer);
                _document  = new Document(pdf);

                _regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                _boldFont    = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                try
                {
                    Document.SetFont(_regularFont);
                    Document.SetFontSize(_options.FontSize);

                    if (_options.DuplicateGroups is { Count: > 0 })
                        AddDuplicatePage(_options.DuplicateGroups);
                    else
                        Document.Add(new Paragraph("No duplicate groups selected.")
                            .SetFont(_regularFont).SetFontSize(10)
                            .SetFontColor(ColorConstants.GRAY));
                }
                finally { Document.Close(); }
            });
        }

        public async Task ExportAsync(ScanResult scanResult, string outputPath = "")
        {
            if (string.IsNullOrEmpty(outputPath))
                outputPath = GenerateOrganizedOutputPath(scanResult, "FolderScan_Report.pdf");

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            _totalItems = scanResult.GetAllFolders().Count();
            _currentItem = 0;

            await Task.Run(() => GeneratePdf(scanResult, outputPath));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Core generation
        // ─────────────────────────────────────────────────────────────────────

        private void GeneratePdf(ScanResult scanResult, string outputPath)
        {
            // Explicit FileStream — guarantees OS handle is released after Close()
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            var writer = new PdfWriter(fileStream);
            var pdf    = new PdfDocument(writer);
            _document  = new Document(pdf);

            _regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            _boldFont    = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            try
            {
                Document.SetFont(RegularFont);
                Document.SetFontSize(_options.FontSize);

                var roots = scanResult.RootFolders;

                if (roots.Count == 0)
                {
                    // Edge case: empty scan
                    AddCompactHeader(scanResult);
                    Document.Add(new Paragraph("No folders found.")
                        .SetFont(RegularFont).SetFontSize(10)
                        .SetFontColor(ColorConstants.GRAY));
                    return;
                }

                for (var i = 0; i < roots.Count; i++)
                {
                    // ── Page 1 of section: overview ──────────────────────────
                    AddOverviewPage(scanResult, roots[i]);

                    // ── Page 2 of section: full detail tree ──────────────────
                    Document.Add(new AreaBreak());
                    AddDetailPage(roots[i]);

                    // Page break before next root section
                    if (i < roots.Count - 1)
                        Document.Add(new AreaBreak());
                }

                // ── Duplicate folders page (appended after all roots) ─────────
                if (_options.IncludeDuplicates
                    && _options.DuplicateGroups is { Count: > 0 })
                {
                    Document.Add(new AreaBreak());
                    AddDuplicatePage(_options.DuplicateGroups);
                }
            }
            finally
            {
                Document.Close();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Page 1 — Overview (compact header + root + direct children)
        // ─────────────────────────────────────────────────────────────────────

        private void AddOverviewPage(ScanResult scanResult, FolderInfo root)
        {
            AddCompactHeader(scanResult, root.FullPath);

            Document.Add(new Paragraph("Folder Structure")
                .SetFont(BoldFont).SetFontSize(13)
                .SetMarginBottom(3));

            var dur = FormatDuration(scanResult.ScanDuration);
            Document.Add(new Paragraph(
                    $"{scanResult.TotalFolders:N0} folders  |  " +
                    $"{scanResult.TotalFiles:N0} files  |  {dur}")
                .SetFont(RegularFont).SetFontSize(9)
                .SetFontColor(ColorConstants.GRAY)
                .SetMarginBottom(12));

            // Root band
            AddRootBlock(root);

            // Direct children — names only, no recursion
            foreach (var child in root.SubFolders.OrderBy(f => f.Name))
            {
                var name = string.IsNullOrWhiteSpace(child.Name) ? child.FullPath : child.Name;
                Document.Add(new Paragraph(name)
                    .SetFont(RegularFont).SetFontSize(10)
                    .SetMarginLeft(IndentPerDepth)
                    .SetMarginBottom(1f));
            }

            if (root.SubFolders.Count == 0)
            {
                Document.Add(new Paragraph("(no subfolders)")
                    .SetFont(RegularFont).SetFontSize(9)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetMarginLeft(IndentPerDepth)
                    .SetMarginBottom(1f));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Page 2 — Detail (root block + full recursive tree)
        // ─────────────────────────────────────────────────────────────────────

        private void AddDetailPage(FolderInfo root)
        {
            AddRootBlock(root);

            var effectiveMax = _options.MaxTreeDepth > 0 ? _options.MaxTreeDepth : int.MaxValue;
            foreach (var child in root.SubFolders.OrderBy(f => f.Name))
                AddFolderToPdf(child, 1, effectiveMax);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Duplicate folders page
        // ─────────────────────────────────────────────────────────────────────

        private void AddDuplicatePage(Dictionary<string, List<string>> groups)
        {
            // Page title
            Document.Add(new Paragraph("Duplicate Folders")
                .SetFont(BoldFont).SetFontSize(16)
                .SetFontColor(ColorConstants.DARK_GRAY)
                .SetMarginBottom(4));

            Document.Add(new Paragraph(
                    $"{groups.Count} duplicate group{(groups.Count > 1 ? "s" : "")} detected  " +
                    $"— folders with identical names found across different scan roots")
                .SetFont(RegularFont).SetFontSize(9)
                .SetFontColor(ColorConstants.GRAY)
                .SetMarginBottom(4));

            // Separator line
            Document.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(0.5f))
                .SetMarginBottom(14));

            // Groups sorted: largest first, then alphabetically
            foreach (var kvp in groups
                .OrderByDescending(k => k.Value.Count)
                .ThenBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var name  = kvp.Key;
                var paths = kvp.Value.OrderBy(p => p).ToList();
                var count = paths.Count;
                var label = count == 2 ? "duplicate" : count == 3 ? "triple" : $"{count} occurrences";

                // Group header: folder name + occurrence count
                Document.Add(new Paragraph($"\u25B8  {name}    ({label})")
                    .SetFont(BoldFont).SetFontSize(11)
                    .SetFontColor(new DeviceRgb(0x72, 0x3A, 0x3A))
                    .SetMarginTop(10)
                    .SetMarginBottom(3));

                // Full path for each occurrence
                foreach (var path in paths)
                {
                    Document.Add(new Paragraph(path)
                        .SetFont(RegularFont).SetFontSize(9)
                        .SetFontColor(ColorConstants.DARK_GRAY)
                        .SetMarginLeft(IndentPerDepth)
                        .SetMarginBottom(1.5f));
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Shared building blocks
        // ─────────────────────────────────────────────────────────────────────

        /// Compact header used on every overview page (no AreaBreak at end).
        private void AddCompactHeader(ScanResult scanResult, string rootPath = "")
        {
            var title = _options.CustomTitle ?? BuildReportTitle(rootPath);

            // Title row
            var titleTable = new Table(2, true)
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetBorder(Border.NO_BORDER)
                .SetMarginBottom(6);

            var titleCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph(title)
                    .SetFont(BoldFont).SetFontSize(20)
                    .SetFontColor(ColorConstants.DARK_GRAY));

            var dateCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.BOTTOM)
                .Add(new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                    .SetFont(RegularFont).SetFontSize(9)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetTextAlignment(TextAlignment.RIGHT));

            titleTable.AddCell(titleCell);
            titleTable.AddCell(dateCell);
            Document.Add(titleTable);

            // Compact metadata row
            var meta = $"Scan date: {scanResult.ScanStartTime:yyyy-MM-dd HH:mm}   " +
                       $"Duration: {FormatDuration(scanResult.ScanDuration)}   " +
                       $"Path(s): {string.Join(", ", scanResult.ScannedPaths)}";
            Document.Add(new Paragraph(meta)
                .SetFont(RegularFont).SetFontSize(8)
                .SetFontColor(ColorConstants.GRAY)
                .SetMarginBottom(10));

            // Horizontal rule
            Document.Add(new Paragraph()
                .SetBorderBottom(new SolidBorder(new DeviceRgb(0.75f, 0.75f, 0.75f), 0.75f))
                .SetMarginBottom(12));
        }

        /// Gray band: root path (bold) + total stats right-aligned.
        private void AddRootBlock(FolderInfo root)
        {
            var sub   = root.SubFolders.Count;
            var stats = $"{sub} {(sub == 1 ? "folder" : "folders")}  |  {root.FileCount} files";

            var para = new Paragraph()
                .SetMarginBottom(4f)
                .AddTabStops(new TabStop(TabRightEdge, TabAlignment.RIGHT))
                .SetBackgroundColor(new DeviceRgb(0.90f, 0.90f, 0.90f))
                .SetPaddingTop(6f).SetPaddingBottom(6f)
                .SetPaddingLeft(8f).SetPaddingRight(8f);

            para.Add(new Text(root.FullPath)
                .SetFont(BoldFont).SetFontSize(11f));
            para.Add(new Tab());
            para.Add(new Text(stats)
                .SetFont(RegularFont).SetFontSize(9f)
                .SetFontColor(new DeviceRgb(0.35f, 0.35f, 0.35f)));

            Document.Add(para);
        }

        /// Recursive detail row: "- name" indented by depth, stats right-aligned.
        private void AddFolderToPdf(FolderInfo folder, int depth, int maxDepth)
        {
            if (depth >= maxDepth)
            {
                // Show truncation hint when we hit the cap
                if (folder.SubFolders.Count > 0)
                {
                    Document.Add(new Paragraph($"- {folder.Name}  [+{folder.SubFolders.Count} subfolder(s) not shown]")
                        .SetFont(RegularFont).SetFontSize(9f)
                        .SetFontColor(ColorConstants.GRAY)
                        .SetMarginLeft(depth * IndentPerDepth)
                        .SetMarginBottom(1f));
                }
                return;
            }

            ReportProgress(folder.Name);

            var name      = string.IsNullOrWhiteSpace(folder.Name) ? folder.FullPath : folder.Name;
            var sub       = folder.SubFolders.Count;
            var stats     = $"{sub} {(sub == 1 ? "folder" : "folders")}  |  {folder.FileCount} files";
            var fontSize  = depth <= 2 ? 10f : 9f;
            var marginLeft = depth * IndentPerDepth;

            var para = new Paragraph()
                .SetMarginBottom(1f)
                .SetMarginLeft(marginLeft)
                .AddTabStops(new TabStop(TabRightEdge - marginLeft, TabAlignment.RIGHT));

            para.Add(new Text("- " + name)
                .SetFont(RegularFont).SetFontSize(fontSize));
            para.Add(new Tab());
            para.Add(new Text(stats)
                .SetFont(RegularFont).SetFontSize(9f)
                .SetFontColor(ColorConstants.GRAY));

            Document.Add(para);

            foreach (var child in folder.SubFolders.OrderBy(f => f.Name))
                AddFolderToPdf(child, depth + 1, maxDepth);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// Builds the report title from the root path.
        /// - 1 component deep (e.g. C:\Work)          → "Folder Scan Report - C:\Work"
        /// - 2+ components deep (e.g. C:\Users\Pascal) → "Folder Scan Report - C:\...\Pascal"
        private static string BuildReportTitle(string rootPath)
        {
            const string prefix = "Folder Scan Report";
            if (string.IsNullOrEmpty(rootPath))
                return prefix;

            var parts = rootPath.TrimEnd('\\', '/')
                                .Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length <= 2)
                return $"{prefix} - {rootPath}";          // e.g. C:\Work

            var drive    = parts[0];                       // "C:"
            var lastName = parts[^1];                      // last segment
            return $"{prefix} - {drive}\\...\\{lastName}";
        }

        private static string FormatDuration(TimeSpan d) =>
            d.TotalSeconds < 60 ? $"{d.TotalSeconds:F1}s" : $"{d.TotalMinutes:F1}min";

        private static string GenerateOrganizedOutputPath(ScanResult scanResult, string fileName)
        {
            var desktop    = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var folderName = FileHelper.CreateScanFolderName(scanResult.ScannedPaths);
            return Path.Combine(desktop, folderName, fileName);
        }


        private void ReportProgress(string currentItem)
        {
            _currentItem++;
            var percent = _totalItems > 0 ? (int)((_currentItem * 100.0) / _totalItems) : 0;
            ExportProgress?.Invoke(this, new ExportProgressEventArgs
            {
                PercentComplete  = Math.Min(100, percent),
                CurrentItem      = currentItem,
                ProcessedItems   = _currentItem,
                TotalItems       = _totalItems
            });
        }

        public event EventHandler<ExportProgressEventArgs>? ExportProgress;
    }
}
