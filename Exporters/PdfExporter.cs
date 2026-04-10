using System;
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
        private PdfExportOptions _options;

        private PdfFont RegularFont => _regularFont ?? throw new InvalidOperationException("PDF fonts not initialized");
        private PdfFont BoldFont => _boldFont ?? throw new InvalidOperationException("PDF fonts not initialized");
        private Document Document => _document ?? throw new InvalidOperationException("PDF document not initialized");

        public PdfExporter() : this(PdfExportOptions.Default)
        {
        }

        public PdfExporter(PdfExportOptions options)
        {
            _options = options ?? PdfExportOptions.Default;
        }

        public async Task ExportAsync(ScanResult scanResult, string outputPath = "")
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = GenerateOrganizedOutputPath(scanResult, "FolderScan_Report.pdf");
            }

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            _totalItems = scanResult.GetAllFolders().Count();
            _currentItem = 0;

            await Task.Run(() => GeneratePdf(scanResult, outputPath));
        }

        private static string GenerateOrganizedOutputPath(ScanResult scanResult, string fileName)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var folderName = FileHelper.CreateScanFolderName(scanResult.ScannedPaths);
            var outputFolder = Path.Combine(desktop, folderName);
            return Path.Combine(outputFolder, fileName);
        }

        private void GeneratePdf(ScanResult scanResult, string outputPath)
        {
            // Use an explicit FileStream so we own the OS handle.
            // Document.Close() flushes iText8 content; the `using` on fileStream
            // then guarantees the handle is released even if iText8 close is incomplete.
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            var writer = new PdfWriter(fileStream);
            var pdf = new PdfDocument(writer);
            _document = new Document(pdf);

            _regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            _boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            try
            {
                Document.SetFont(RegularFont);
                Document.SetFontSize(_options.FontSize);

                if (_options.IncludeHeader)
                    AddHeader(scanResult);
                if (_options.IncludeStatistics)
                    AddSummary(scanResult);
                if (_options.IncludeTableOfContents)
                    AddTableOfContents(scanResult);
                if (_options.IncludeFolderTree)
                    AddFolderTree(scanResult);
            }
            finally
            {
                Document.Close(); // flushes PDF content and closes writer
                // fileStream disposed by `using` — OS handle guaranteed released
            }
        }

        private void AddHeader(ScanResult scanResult)
        {
            var headerTable = new Table(2, true);
            headerTable.SetWidth(UnitValue.CreatePercentValue(100));
            headerTable.SetBorder(Border.NO_BORDER);

            var icon = _options.UseEmojis ? "📁 " : "";
            var title = _options.CustomTitle ?? "Folder Scan Report";
            var titleCell = new Cell().Add(new Paragraph($"{icon}{title}")
                .SetFont(BoldFont)
                .SetFontSize(24)
                .SetFontColor(ColorConstants.DARK_GRAY));
            titleCell.SetBorder(Border.NO_BORDER);
            titleCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);

            var dateCell = new Cell().Add(new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                .SetFont(RegularFont)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT));
            dateCell.SetBorder(Border.NO_BORDER);
            dateCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);

            headerTable.AddCell(titleCell);
            headerTable.AddCell(dateCell);

            Document.Add(headerTable);

            var infoTable = new Table(3, true);
            infoTable.SetWidth(UnitValue.CreatePercentValue(100));
            infoTable.SetMarginTop(10);
            infoTable.SetMarginBottom(20);

            AddInfoRow(infoTable, "Scan Date:", scanResult.ScanStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            AddInfoRow(infoTable, "Duration:", $"{scanResult.ScanDuration.TotalSeconds:F2} seconds");
            AddInfoRow(infoTable, "Scanned Paths:", string.Join(", ", scanResult.ScannedPaths));

            Document.Add(infoTable);
            Document.Add(new AreaBreak());
        }

        private void AddInfoRow(Table table, string label, string value)
        {
            table.AddCell(new Cell().Add(new Paragraph(label)
                .SetFont(BoldFont)
                .SetFontSize(10))
                .SetBorder(Border.NO_BORDER)
                .SetPadding(2));

            table.AddCell(new Cell().Add(new Paragraph(value)
                .SetFont(RegularFont)
                .SetFontSize(10))
                .SetBorder(Border.NO_BORDER)
                .SetPadding(2));

            table.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER));
        }

        private void AddSummary(ScanResult scanResult)
        {
            var icon = _options.UseEmojis ? "📊 " : "";
            Document.Add(new Paragraph($"{icon}Scan Statistics")
                .SetFont(BoldFont)
                .SetFontSize(18)
                .SetMarginBottom(15));

            var statsTable = new Table(3, true);
            statsTable.SetWidth(UnitValue.CreatePercentValue(100));
            statsTable.SetMarginBottom(20);

            var folderLabel = _options.UseEmojis ? "📁 Total Folders" : "Total Folders";
            var fileLabel   = _options.UseEmojis ? "📄 Total Files"   : "Total Files";
            var durLabel    = _options.UseEmojis ? "⏱ Scan Duration"  : "Scan Duration";

            AddStatCard(statsTable, folderLabel, $"{scanResult.TotalFolders:N0}");
            AddStatCard(statsTable, fileLabel,   $"{scanResult.TotalFiles:N0}");
            AddStatCard(statsTable, durLabel,    $"{scanResult.ScanDuration.TotalSeconds:F1}s");

            Document.Add(statsTable);
        }

        private void AddStatCard(Table table, string label, string value)
        {
            var cell = new Cell();
            cell.SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
            cell.SetBackgroundColor(ColorConstants.LIGHT_GRAY);
            cell.SetPadding(10);
            cell.SetTextAlignment(TextAlignment.CENTER);

            cell.Add(new Paragraph(label)
                .SetFont(BoldFont)
                .SetFontSize(12)
                .SetMarginBottom(5));

            var (primary, _, _) = ColorSchemeHelper.GetColors(_options.ColorScheme);
            var color = ParseHexColor(primary);

            cell.Add(new Paragraph(value)
                .SetFont(BoldFont)
                .SetFontSize(16)
                .SetFontColor(color));

            table.AddCell(cell);
        }

        private DeviceRgb ParseHexColor(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length != 6) return new DeviceRgb(0.4f, 0.49f, 0.92f); // Default color

            try
            {
                var r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
                var g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
                var b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
                return new DeviceRgb(r, g, b);
            }
            catch
            {
                return new DeviceRgb(0.4f, 0.49f, 0.92f); // Default color on error
            }
        }

        private void AddTableOfContents(ScanResult scanResult)
        {
            var icon = _options.UseEmojis ? "📋 " : "";
            Document.Add(new Paragraph($"{icon}Table of Contents")
                .SetFont(BoldFont)
                .SetFontSize(18)
                .SetMarginBottom(15));

            var tocList = new List();
            tocList.SetMarginBottom(20);

            tocList.Add(new ListItem("Scan Statistics"));
            tocList.Add(new ListItem("Folder Structure"));

            foreach (var rootFolder in scanResult.RootFolders)
            {
                var tocLine = _options.UseEmojis
                    ? $"{rootFolder.FullPath} (📁{rootFolder.SubFolderCount} | 📄{rootFolder.FileCount})"
                    : $"{rootFolder.FullPath} ({rootFolder.SubFolderCount} folders | {rootFolder.FileCount} files)";
                tocList.Add(new ListItem(tocLine));
            }

            Document.Add(tocList);
            Document.Add(new AreaBreak());
        }

        private void AddFolderTree(ScanResult scanResult)
        {
            // Page 2 header — compact, leaves room for the tree
            Document.Add(new Paragraph("Folder Structure")
                .SetFont(BoldFont)
                .SetFontSize(14)
                .SetMarginBottom(3));

            // One-line summary (replaces the removed statistics page)
            var dur = scanResult.ScanDuration.TotalSeconds < 60
                ? $"{scanResult.ScanDuration.TotalSeconds:F1}s"
                : $"{scanResult.ScanDuration.TotalMinutes:F1}min";
            Document.Add(new Paragraph(
                    $"{scanResult.TotalFolders:N0} folders  |  {scanResult.TotalFiles:N0} files  |  {dur}")
                .SetFont(RegularFont)
                .SetFontSize(9)
                .SetFontColor(ColorConstants.GRAY)
                .SetMarginBottom(10));

            var roots = scanResult.RootFolders;
            for (var i = 0; i < roots.Count; i++)
            {
                AddFolderToPdf(roots[i], 0, true);

                if (i < roots.Count - 1)
                {
                    // Thin separator between root folders
                    Document.Add(new Paragraph()
                        .SetBorderBottom(new SolidBorder(new DeviceRgb(0.82f, 0.82f, 0.82f), 0.5f))
                        .SetMarginTop(8)
                        .SetMarginBottom(10));
                }
            }
        }

        private void AddFolderToPdf(FolderInfo folder, int depth = 0, bool isRoot = false)
        {
            if (_options.MaxTreeDepth > 0 && depth >= _options.MaxTreeDepth)
                return;

            ReportProgress(folder.Name);

            var displayName = isRoot
                ? folder.FullPath
                : (string.IsNullOrEmpty(folder.Name) ? folder.FullPath : folder.Name);

            var sub = folder.SubFolders.Count;
            var stats = $"{sub} {(sub == 1 ? "folder" : "folders")}  |  {folder.FileCount} files";

            // Paragraph with tab-stop to right-align stats
            var para = new Paragraph()
                .SetMarginBottom(isRoot ? 5f : 1f)
                .AddTabStops(new TabStop(510f, TabAlignment.RIGHT));

            var nameIndent = depth == 0 ? "" : new string(' ', depth * 4);
            var nameFontSize = isRoot ? 11f : (depth <= 2 ? 10f : 9f);

            para.Add(new Text(nameIndent + displayName)
                .SetFont(isRoot ? BoldFont : RegularFont)
                .SetFontSize(nameFontSize));

            para.Add(new Tab());

            para.Add(new Text(stats)
                .SetFont(RegularFont)
                .SetFontSize(9f)
                .SetFontColor(ColorConstants.GRAY));

            if (isRoot)
            {
                para.SetBackgroundColor(new DeviceRgb(0.93f, 0.93f, 0.93f))
                    .SetPaddingTop(5f)
                    .SetPaddingBottom(5f)
                    .SetPaddingLeft(8f)
                    .SetPaddingRight(8f);
            }

            Document.Add(para);

            var effectiveMax = _options.MaxTreeDepth > 0 ? _options.MaxTreeDepth : 8;
            if (folder.SubFolders.Count > 0 && depth < effectiveMax)
            {
                foreach (var child in folder.SubFolders.OrderBy(f => f.Name))
                    AddFolderToPdf(child, depth + 1);
            }
            else if (folder.SubFolders.Count > 0 && depth >= effectiveMax)
            {
                var moreText = new string(' ', (depth + 1) * 4)
                    + $"... {folder.SubFolders.Count} more subfolder(s)";
                Document.Add(new Paragraph(moreText)
                    .SetFont(RegularFont)
                    .SetFontSize(9f)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetMarginBottom(1f));
            }
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
}