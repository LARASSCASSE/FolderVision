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
        private PdfFont _regularFont;
        private PdfFont _boldFont;
        private Document _document;

        public PdfExporter()
        {
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
            using var writer = new PdfWriter(outputPath);
            using var pdf = new PdfDocument(writer);
            _document = new Document(pdf);

            _regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            _boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            _document.SetFont(_regularFont);
            _document.SetFontSize(10);

            AddHeader(scanResult);
            AddSummary(scanResult);
            AddTableOfContents(scanResult);
            AddFolderTree(scanResult);

            _document.Close();
        }

        private void AddHeader(ScanResult scanResult)
        {
            var headerTable = new Table(2, true);
            headerTable.SetWidth(UnitValue.CreatePercentValue(100));
            headerTable.SetBorder(Border.NO_BORDER);

            var titleCell = new Cell().Add(new Paragraph("📁 Folder Scan Report")
                .SetFont(_boldFont)
                .SetFontSize(24)
                .SetFontColor(ColorConstants.DARK_GRAY));
            titleCell.SetBorder(Border.NO_BORDER);
            titleCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);

            var dateCell = new Cell().Add(new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                .SetFont(_regularFont)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT));
            dateCell.SetBorder(Border.NO_BORDER);
            dateCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);

            headerTable.AddCell(titleCell);
            headerTable.AddCell(dateCell);

            _document.Add(headerTable);

            var infoTable = new Table(3, true);
            infoTable.SetWidth(UnitValue.CreatePercentValue(100));
            infoTable.SetMarginTop(10);
            infoTable.SetMarginBottom(20);

            AddInfoRow(infoTable, "Scan Date:", scanResult.ScanStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            AddInfoRow(infoTable, "Duration:", $"{scanResult.ScanDuration.TotalSeconds:F2} seconds");
            AddInfoRow(infoTable, "Scanned Paths:", string.Join(", ", scanResult.ScannedPaths));

            _document.Add(infoTable);
            _document.Add(new AreaBreak());
        }

        private void AddInfoRow(Table table, string label, string value)
        {
            table.AddCell(new Cell().Add(new Paragraph(label)
                .SetFont(_boldFont)
                .SetFontSize(10))
                .SetBorder(Border.NO_BORDER)
                .SetPadding(2));

            table.AddCell(new Cell().Add(new Paragraph(value)
                .SetFont(_regularFont)
                .SetFontSize(10))
                .SetBorder(Border.NO_BORDER)
                .SetPadding(2));

            table.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER));
        }

        private void AddSummary(ScanResult scanResult)
        {
            _document.Add(new Paragraph("📊 Scan Statistics")
                .SetFont(_boldFont)
                .SetFontSize(18)
                .SetMarginBottom(15));

            var statsTable = new Table(3, true);
            statsTable.SetWidth(UnitValue.CreatePercentValue(100));
            statsTable.SetMarginBottom(20);

            AddStatCard(statsTable, "📁 Total Folders", $"{scanResult.TotalFolders:N0}");
            AddStatCard(statsTable, "📄 Total Files", $"{scanResult.TotalFiles:N0}");
            AddStatCard(statsTable, "⏱️ Scan Duration", $"{scanResult.ScanDuration.TotalSeconds:F1}s");

            _document.Add(statsTable);
        }

        private void AddStatCard(Table table, string label, string value)
        {
            var cell = new Cell();
            cell.SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
            cell.SetBackgroundColor(ColorConstants.LIGHT_GRAY);
            cell.SetPadding(10);
            cell.SetTextAlignment(TextAlignment.CENTER);

            cell.Add(new Paragraph(label)
                .SetFont(_boldFont)
                .SetFontSize(12)
                .SetMarginBottom(5));

            cell.Add(new Paragraph(value)
                .SetFont(_boldFont)
                .SetFontSize(16)
                .SetFontColor(new DeviceRgb(0.4f, 0.49f, 0.92f)));

            table.AddCell(cell);
        }

        private void AddTableOfContents(ScanResult scanResult)
        {
            _document.Add(new Paragraph("📋 Table of Contents")
                .SetFont(_boldFont)
                .SetFontSize(18)
                .SetMarginBottom(15));

            var tocList = new List();
            tocList.SetMarginBottom(20);

            tocList.Add(new ListItem("Scan Statistics"));
            tocList.Add(new ListItem("Folder Structure"));

            foreach (var rootFolder in scanResult.RootFolders)
            {
                var listItem = new ListItem($"{rootFolder.FullPath} (📁{rootFolder.SubFolderCount} | 📄{rootFolder.FileCount})");
                tocList.Add(listItem);
            }

            _document.Add(tocList);
            _document.Add(new AreaBreak());
        }

        private void AddTocSubfolders(List parentList, FolderInfo folder, int currentDepth, int maxDepth)
        {
            // Simplified implementation to avoid nested list issues
            // For now, just add the folder to the main list
        }

        private void AddFolderTree(ScanResult scanResult)
        {
            _document.Add(new Paragraph("🌳 Folder Structure")
                .SetFont(_boldFont)
                .SetFontSize(18)
                .SetMarginBottom(15));

            foreach (var rootFolder in scanResult.RootFolders)
            {
                AddFolderToPdf(rootFolder, 0, true);
            }
        }

        private void AddFolderToPdf(FolderInfo folder, int depth = 0, bool isRoot = false)
        {
            ReportProgress(folder.Name);

            var indent = new string(' ', depth * 4);
            var displayName = isRoot ? folder.FullPath : folder.Name;
            var folderText = $"{indent}📁 {displayName} (📁{folder.SubFolderCount} | 📄{folder.FileCount})";

            var paragraph = new Paragraph(folderText)
                .SetFont(depth == 0 ? _boldFont : _regularFont)
                .SetFontSize(depth == 0 ? 12 : 10)
                .SetMarginBottom(2);

            if (depth > 0)
            {
                paragraph.SetMarginLeft(depth * 10);
            }

            _document.Add(paragraph);

            if (folder.SubFolders.Count > 0 && depth < 5)
            {
                foreach (var subFolder in folder.SubFolders.OrderBy(f => f.Name))
                {
                    AddFolderToPdf(subFolder, depth + 1);
                }
            }
            else if (folder.SubFolders.Count > 0)
            {
                var moreText = $"{new string(' ', (depth + 1) * 4)}... {folder.SubFolders.Count} subfolders (max depth reached)";
                _document.Add(new Paragraph(moreText)
                    .SetFont(_regularFont)
                    .SetFontSize(9)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetMarginLeft((depth + 1) * 10)
                    .SetMarginBottom(2));
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