# FolderVision - Project Structure

## 📁 Directory Organization

```
FolderVision/
│
├── 📂 Core/                        # Core Application Logic
│   ├── ScanEngine.cs              # Main scanning engine with threading
│   ├── ProgressTracker.cs         # Progress tracking utilities
│   ├── ThreadManager.cs           # Thread pool management
│   └── Logging/                   # Structured logging system
│       ├── ILogger.cs             # Logger interface
│       ├── Logger.cs              # Main logger implementation
│       ├── LogLevel.cs            # Log severity levels
│       ├── LogEntry.cs            # Structured log entry
│       ├── ILogProvider.cs        # Provider interface
│       └── Providers/
│           ├── ConsoleLogProvider.cs  # Console output with colors
│           └── FileLogProvider.cs     # File logging with rotation
│
├── 📂 Models/                      # Data Models
│   ├── ScanResult.cs              # Scan results container
│   ├── ScanSettings.cs            # Configuration settings
│   ├── FolderInfo.cs              # Folder metadata
│   ├── ExportOptions.cs           # HTML/PDF export options
│   └── LoggingOptions.cs          # Logging configuration
│
├── 📂 Exporters/                   # Export Functionality
│   ├── HtmlExporter.cs            # HTML report generation
│   └── PdfExporter.cs             # PDF report generation
│
├── 📂 Utils/                       # Utility Classes
│   ├── FileSizeFormatter.cs       # Internationalized file size formatting
│   ├── FileHelper.cs              # File operations utilities
│   ├── TimeoutHelper.cs           # Timeout management
│   └── MemoryMonitor.cs           # Memory usage monitoring
│
├── 📂 Examples/                    # Usage Examples
│   ├── LoggingExamples.cs         # Logging system examples
│   └── ExportCustomizationExamples.cs  # Export customization examples
│
├── 📂 Tests/                       # Testing
│   ├── Manual/                    # Manual test scripts
│   │   ├── TestScanEngine.cs      # Engine testing
│   │   ├── TestMultiThreaded.cs   # Threading tests
│   │   ├── TestPdfIntegration.cs  # PDF export tests
│   │   ├── TestFileHelper.cs      # Utility tests
│   │   ├── TestConsoleUI.cs       # UI tests
│   │   ├── TestDirectScan.cs      # Direct scan tests
│   │   ├── TestPlatform.cs        # Platform compatibility tests
│   │   └── TestWorkflow.cs        # Workflow tests
│   └── (automated tests in FolderVision.Tests/ project)
│
├── 📂 FolderVision.Tests/          # xUnit Unit Tests
│   ├── FileSizeFormatterTests.cs  # Formatter tests (10 tests)
│   ├── ScanEngineTests.cs         # Engine tests (8 tests)
│   ├── LoggingSystemTests.cs      # Logging tests (6 tests)
│   ├── ExporterTests.cs           # Export tests (6 tests)
│   └── FolderVision.Tests.csproj  # Test project file
│
├── 📂 FolderScanner_Release/       # Release Build Output
│   ├── FolderVision.exe           # Compiled executable
│   ├── README.txt                 # Release notes
│   └── Run_FolderScanner.bat      # Quick launcher
│
├── 📂 docs/                        # Documentation
│   ├── BUILD.md                   # Build instructions
│   ├── DEPLOYMENT.md              # Deployment guide
│   ├── LOGGING_SYSTEM.md          # Logging documentation
│   ├── COMPILATION_ANALYSIS.md    # Build analysis
│   ├── GIT_COMMIT_GUIDE.md        # Git workflow
│   ├── CHANGELOG_PROBLEMS_10-12.md # Problem resolutions
│   ├── SUMMARY_PROBLEMS_10-12.md  # Resolution summary
│   ├── STARTUP_TEST_REPORT.md     # Test reports
│   ├── RESOLUTION_COMPLETE.txt    # Completion notes
│   └── expected-output.txt        # Expected behavior
│
├── 📂 .github/workflows/           # CI/CD Pipeline
│   └── ci.yml                     # GitHub Actions workflow
│
├── 📄 Program.cs                   # Application entry point
├── 📄 FolderVision.csproj          # Main project file
├── 📄 FolderVision.sln             # Solution file
├── 📄 README.md                    # Project overview
├── 📄 .gitignore                   # Git ignore rules
├── 📄 .gitattributes               # Git attributes
├── 📄 build.sh                     # Linux build script
└── 📄 build.cmd                    # Windows build script
```

## 🎯 Component Responsibilities

### Core Components
- **ScanEngine**: Multi-threaded folder scanning with adaptive batching
- **Logging System**: Enterprise-grade structured logging (5 levels, 2 providers)
- **ProgressTracker**: Real-time progress reporting
- **ThreadManager**: Thread pool optimization

### Models
- **ScanSettings**: Configurable scan parameters (depth, threads, timeouts)
- **ScanResult**: Aggregated scan results with statistics
- **ExportOptions**: 6 color schemes, full customization

### Utilities
- **FileSizeFormatter**: i18n support (EN/FR), binary/decimal units
- **MemoryMonitor**: Prevents OOM with adaptive cleanup
- **TimeoutHelper**: Network/local drive timeout management

### Export System
- **HtmlExporter**: Responsive HTML with collapsible trees
- **PdfExporter**: Professional PDF reports with iText7

## 🔧 Key Features by Component

| Component | Features | Lines of Code |
|-----------|----------|---------------|
| ScanEngine | Adaptive batching, timeout handling, memory optimization | ~540 |
| Logging System | Multi-provider, rotation, correlation IDs | ~450 |
| FileSizeFormatter | i18n, binary/decimal, parsing | ~235 |
| HtmlExporter | 6 themes, responsive, customizable | ~400 |
| PdfExporter | Professional layout, TOC, page numbers | ~500 |

## 📊 Test Coverage

| Test Suite | Tests | Coverage |
|------------|-------|----------|
| FileSizeFormatterTests | 10 | 100% |
| ScanEngineTests | 8 | Core paths |
| LoggingSystemTests | 6 | All presets |
| ExporterTests | 6 | Color schemes |
| **Total** | **42** | **High** |

## 🚀 Build & Deploy

- **Development**: `dotnet build`
- **Release**: `dotnet publish -c Release`
- **Tests**: `dotnet test`
- **CI/CD**: Automated via GitHub Actions

## 📝 Code Standards

- ✅ XML documentation on all public APIs
- ✅ Nullable reference types enabled
- ✅ Consistent naming conventions
- ✅ SOLID principles applied
- ✅ Async/await patterns
- ✅ Thread-safe implementations

## 🔗 Dependencies

- **.NET 9.0** - Runtime framework
- **iText7** - PDF generation
- **xUnit** - Unit testing
- **coverlet** - Code coverage

---

*Last updated: 2025-10-19*
*Version: 2.1 - Complete Testing & CI/CD*
