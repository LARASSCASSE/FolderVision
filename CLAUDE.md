# FolderVision — Project Context for Claude Code

## What is this project?

FolderVision is a **Windows desktop app** (WPF, .NET 9) that scans folders/drives, displays the folder tree, detects duplicate folders, and exports PDF reports.

- **Language**: C# / .NET 9
- **UI**: WPF (Windows Presentation Foundation)
- **PDF**: iText7 v8.0.5
- **Platform**: Windows 10/11 x64 only (self-contained)

---

## Repository layout

```
D:\Projects\FolderVision\
├── CODE\                          ← all source code lives here
│   ├── FolderVision.sln           ← solution file (open this in VS)
│   ├── FolderVision.csproj        ← core library (scan engine, models, PDF exporter)
│   ├── Core\
│   │   ├── ScanEngine.cs          ← multi-threaded folder scanner (main logic)
│   │   ├── ThreadManager.cs       ← thread pool management
│   │   ├── ProgressTracker.cs     ← progress reporting
│   │   └── Logging\               ← ILogger, Logger, LogEntry, providers
│   ├── Models\
│   │   ├── FolderInfo.cs          ← folder node (FullPath, Name, SubFolders, Files, FileCount)
│   │   ├── ScanResult.cs          ← scan output (RootFolders, TotalFolders, TotalFiles)
│   │   ├── ScanSettings.cs        ← scan config (PathsToScan, MaxThreads, SkipHidden...)
│   │   ├── ExportOptions.cs       ← PdfExportOptions (MaxTreeDepth, FontSize, IncludeDuplicates...)
│   │   └── LoggingOptions.cs
│   ├── Exporters\
│   │   └── PdfExporter.cs         ← generates PDF from ScanResult using iText7
│   ├── Utils\
│   │   ├── FileSizeFormatter.cs   ← human-readable file sizes (KB/MB/GB)
│   │   ├── FileHelper.cs
│   │   ├── MemoryMonitor.cs
│   │   └── TimeoutHelper.cs
│   ├── FolderVision.Wpf\          ← WPF GUI project
│   │   ├── FolderVision.Wpf.csproj
│   │   ├── MainWindow.xaml/.cs    ← main window (tree view, scan controls, duplicate detection)
│   │   ├── SplashWindow.xaml/.cs  ← startup splash screen
│   │   ├── ExportPreviewWindow.xaml/.cs  ← PDF preview before export
│   │   ├── DuplicatesTabContent.xaml/.cs ← duplicate folders tab
│   │   ├── PreviewTabContent.xaml/.cs    ← preview tab
│   │   ├── Themes\
│   │   │   └── DarkTheme.xaml     ← dark theme resource dictionary
│   │   ├── Converters\
│   │   │   └── InverseBoolConverter.cs
│   │   ├── Models\
│   │   │   └── PreviewNode.cs
│   │   └── Ressources\
│   │       ├── app.ico
│   │       ├── FolderVision_Icon_NoName.png
│   │       └── FolderVision_Icon_Name.png
│   └── FolderVision.Tests\        ← xUnit test project
│       ├── ScanEngineTests.cs
│       ├── ExporterTests.cs
│       ├── PdfExporterTests.cs
│       ├── ScanResultTests.cs
│       ├── FileSizeFormatterTests.cs
│       └── LoggingSystemTests.cs
├── installer\
│   ├── FolderVision.iss           ← Inno Setup 6 script
│   ├── build-installer.cmd        ← build script (publish + ISCC)
│   └── sign.ps1                   ← self-signed code signing (optional)
├── publish_out\                   ← publish output (gitignored) — exe + native WPF DLLs
└── CLAUDE.md                      ← this file
```

---

## Key models

### FolderInfo (`Models/FolderInfo.cs`)
```csharp
public string FullPath { get; set; }
public string Name { get; set; }
public int FileCount { get; set; }           // direct files only
public List<FolderInfo> SubFolders { get; set; }
public List<(string Name, long Size)> Files { get; set; }  // direct child files with size
public int GetTotalFileCount()               // recursive
public int GetTotalSubFolderCount()          // recursive
```

### ScanResult (`Models/ScanResult.cs`)
```csharp
public List<FolderInfo> RootFolders { get; set; }
public int TotalFolders { get; set; }
public int TotalFiles { get; set; }
public TimeSpan ScanDuration { get; set; }
public IEnumerable<FolderInfo> GetAllFolders()   // flat enumeration of every node
```

### ScanSettings (`Models/ScanSettings.cs`)
```csharp
public List<string> PathsToScan { get; set; }
public bool SkipSystemFolders { get; set; }
public bool SkipHiddenFolders { get; set; }
public int MaxThreads { get; set; }          // default 8
public int MaxDepth { get; set; }            // default 500
```

### PdfExportOptions (`Models/ExportOptions.cs`)
```csharp
public bool IncludeHeader { get; set; } = true;
public int MaxTreeDepth { get; set; } = 8;
public int FontSize { get; set; } = 10;
public bool IncludeDuplicates { get; set; } = true;
public Dictionary<string, List<string>>? DuplicateGroups { get; set; }
// Presets: PdfExportOptions.Default, .Compact, .Detailed, .French
```

---

## Features implemented

1. **Multi-path scanning** — add multiple drives/folders, scan them all in one run
2. **Multi-threaded scan engine** — up to 16 threads, adaptive batching, memory limit
3. **Folder tree view** — lazy-loaded WPF TreeView, depth display with file/folder counts
4. **Duplicate folder detection** — finds folders with the same name across different locations
   - Strict mode: exact counts must match
   - Approximate mode: similarity slider 10%–100% based on file name+size overlap
   - Navigation button "⇄ N" scrolls to each occurrence with a flash animation
5. **PDF export** — iText7-based, configurable depth/font/header, optional duplicate page
6. **PDF preview** — ExportPreviewWindow shows structure before generating file
7. **Skip hidden/system folders** — checkboxes in UI
8. **Single-instance enforcement** — Mutex prevents running the app twice
9. **Splash screen** — shows on startup while main window loads

---

## Build commands

All commands run from `D:\Projects\FolderVision\CODE\` (or the worktree equivalent):

```bash
# Build the solution
dotnet build FolderVision.sln --configuration Release

# Run tests
dotnet test FolderVision.Tests --configuration Release

# Publish the WPF app (self-contained single-file for win-x64)
dotnet publish FolderVision.Wpf --configuration Release --self-contained true \
    -p:PublishSingleFile=true -o ..\publish_out

# Rename output (the exe is published as FolderVision.Wpf.exe)
# After publish: rename publish_out\FolderVision.Wpf.exe FolderVision.exe

# Build the installer (publish + Inno Setup)
cd ..
installer\build-installer.cmd
```

> **Important**: `publish_out\` contains the exe AND several native WPF DLLs
> (`wpfgfx_cor3.dll`, `D3DCompiler_47_cor3.dll`, `PenImc_cor3.dll`, etc.) — these are
> NOT bundled into the single file and must be deployed alongside FolderVision.exe.
> The installer handles this automatically via `Source: "..\publish_out\*"`.

---

## Worktree (git)

The project uses a git worktree for active development:

```
D:\Projects\FolderVision\.claude\worktrees\trusting-carson\
```

This worktree has the same CODE/ structure. After making changes there:
```bash
cd D:\Projects\FolderVision\.claude\worktrees\trusting-carson\CODE
dotnet publish FolderVision.Wpf --configuration Release --self-contained true \
    -p:PublishSingleFile=true -o publish_out
cp publish_out\* D:\Projects\FolderVision\publish_out\
```

Or use the main repo directly (`D:\Projects\FolderVision\CODE\`).

---

## Code conventions

- **Namespace**: `FolderVision` (core), `FolderVision.Models`, `FolderVision.Wpf`
- **Nullable**: enabled — use `?` and null-checks consistently
- **Async**: UI interactions use `async/await`, scan engine uses `Task.Run`
- **Thread safety**: `FolderInfo` and `ScanResult` use internal `lock(_lockObject)` guards
- **No MVVM framework**: plain WPF code-behind (no Prism, no MVVM Toolkit)
- **Theme**: single dark theme (`DarkTheme.xaml`), no light mode
- **Icons/resources**: WPF `<Resource>` items in FolderVision.Wpf.csproj

---

## Dependencies

| Package | Version | Used for |
|---------|---------|----------|
| iText7 | 8.0.5 | PDF generation |
| itext.bouncy-castle-adapter | 8.0.5 | iText7 crypto |
| xUnit | (via Tests project) | Unit tests |

No NuGet packages in the WPF project (references core via `<ProjectReference>`).

---

## Common gotchas

- **AssemblyName conflict**: Both `FolderVision.csproj` (core) and `FolderVision.Wpf.csproj` would conflict if both named "FolderVision". Solution: rename the published exe after `dotnet publish`.
- **Native DLLs**: WPF self-contained publish does NOT bundle `wpfgfx_cor3.dll` etc. They land in `publish_out/` next to the exe and must be deployed.
- **Inno Setup**: requires Inno Setup 6 at `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`. Free download: https://jrsoftware.org/isdl.php
- **Lazy tree loading**: TreeView only renders depth=0 upfront; `Expanded` event triggers deeper builds.
- **Duplicate detection**: runs after scan completes, before tree population. Requires checkbox "Detect duplicate folders" to be checked before scanning.
