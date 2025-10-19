# FolderVision 📁

[![CI Build and Test](https://github.com/YOUR_USERNAME/FolderVision/workflows/CI%20-%20Build%20and%20Test/badge.svg)](https://github.com/YOUR_USERNAME/FolderVision/actions)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Professional Multi-Threaded Folder Scanner with HTML and PDF Export

Development of a Folder Analysis Application for Windows. This tool allows users to analyze and map the complete folder structure across one or multiple drives, whether used simultaneously or separately, and generates detailed reports.

## Features

- **Fast Multi-Threaded Scanning** - Adaptive batch processing for optimal performance
- **HTML & PDF Export** - Beautiful, customizable reports with 6 color schemes
- **Internationalization** - Full FR/EN support with binary/decimal units
- **Structured Logging** - Enterprise-grade logging with file rotation
- **Memory Optimization** - Handles massive folder structures (>10k directories)
- **Cross-Platform Ready** - Built on .NET 9

## Quick Start

```bash
# Clone repository
git clone https://github.com/YOUR_USERNAME/FolderVision.git
cd FolderVision

# Build
dotnet build -c Release

# Run
dotnet run -- --scan-folder "C:\\MyFolder"
```

## CI/CD

This project uses **GitHub Actions** for continuous integration:

- ✅ **Automated Build** on every push/PR
- ✅ **42 Unit Tests** with xUnit
- ✅ **Release Artifacts** published automatically
- ✅ **Test Results** reported on every run

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Test Coverage:**
- FileSizeFormatter: 10 tests
- ScanEngine: 8 tests
- Logging System: 6 tests
- Exporters: 6 tests
- **Total: 42 tests**

## Project Structure

```
FolderVision/
├── Core/                    # Core engine and logging
│   ├── ScanEngine.cs       # Main scanning engine
│   └── Logging/            # Structured logging system
├── Models/                  # Data models
├── Exporters/              # HTML/PDF export
├── Utils/                   # Utilities (FileSizeFormatter, etc.)
├── Examples/               # Usage examples
├── FolderVision.Tests/     # xUnit test project
└── .github/workflows/      # CI/CD pipeline
```

## Documentation

- [Logging System Guide](LOGGING_SYSTEM.md)
- [Export Customization](Models/ExportOptions.cs)
- [Usage Examples](Examples/)

## Technologies

- .NET 9.0
- iText7 (PDF generation)
- xUnit (Testing)
- GitHub Actions (CI/CD)

## License

MIT License - See [LICENSE](LICENSE)
