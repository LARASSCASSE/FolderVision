# Scan D: drive and generate HTML report
Write-Host "FolderVision - Scanning D: drive..." -ForegroundColor Cyan

# Build the project first
Write-Host "Building project..." -ForegroundColor Yellow
& "C:\Program Files\dotnet\dotnet.exe" build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Create a simple test program to scan D:
$testCode = @"
using System;
using System.Threading.Tasks;
using FolderVision.Core;
using FolderVision.Models;
using FolderVision.Exporters;

var settings = ScanSettings.CreateDefault();
settings.LoggingOptions = LoggingOptions.FileOnly; // Quiet mode

var engine = new ScanEngine();
Console.WriteLine("Starting scan of D: drive...");

var result = await engine.ScanFolderAsync(@"D:\", settings);

Console.WriteLine($"Scan complete: {result.TotalFolders} folders, {result.TotalFiles} files");

var exporter = new HtmlExporter();
var outputPath = $@"D:\FolderVision_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
await exporter.ExportAsync(result, outputPath);

Console.WriteLine($"HTML report saved to: {outputPath}");
"@

# Save temp C# file
$tempFile = "temp_scan.cs"
$testCode | Out-File -FilePath $tempFile -Encoding UTF8

Write-Host "Scanning D: drive (this may take a while)..." -ForegroundColor Yellow
& "C:\Program Files\dotnet\dotnet.exe" script $tempFile

Remove-Item $tempFile -ErrorAction SilentlyContinue
Write-Host "Done!" -ForegroundColor Green
