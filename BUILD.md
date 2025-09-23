# FolderVision Build and Deployment Guide

## Table of Contents
- [Prerequisites](#prerequisites)
- [Quick Build](#quick-build)
- [Detailed Build Instructions](#detailed-build-instructions)
- [Creating Standalone Executables](#creating-standalone-executables)
- [Platform-Specific Instructions](#platform-specific-instructions)
- [Distribution](#distribution)
- [Troubleshooting](#troubleshooting)
- [Performance Optimizations](#performance-optimizations)

## Prerequisites

### Required Software
- **.NET 6.0 SDK or later**
  - Download from: https://dotnet.microsoft.com/download
  - Verify installation: `dotnet --version`

### Optional Tools
- **Visual Studio 2022** (Windows) - For advanced debugging
- **Visual Studio Code** (Cross-platform) - Lightweight development
- **JetBrains Rider** (Cross-platform) - Full-featured IDE

## Quick Build

### Windows
```cmd
# Clone and build
git clone <repository-url>
cd FolderVision
build.cmd
```

### Linux/macOS
```bash
# Clone and build
git clone <repository-url>
cd FolderVision
chmod +x build.sh
./build.sh
```

## Detailed Build Instructions

### 1. Development Build (Debug)
```bash
# Restore dependencies
dotnet restore

# Build in debug mode
dotnet build --configuration Debug

# Run the application
dotnet run
```

### 2. Release Build
```bash
# Build optimized release version
dotnet build --configuration Release

# Run release version
dotnet run --configuration Release
```

### 3. Publishing (Framework-Dependent)
```bash
# Publish for current platform (requires .NET on target)
dotnet publish --configuration Release --output ./publish
```

## Creating Standalone Executables

### Automatic Build (Recommended)

**Windows:**
```cmd
build.cmd
```

**Linux/macOS:**
```bash
./build.sh
```

### Manual Build Commands

#### Windows x64 (Self-Contained)
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish/win-x64"
```

#### Windows x86 (Self-Contained)
```bash
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish/win-x86"
```

#### Linux x64 (Self-Contained)
```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish/linux-x64"
```

#### macOS x64 (Self-Contained)
```bash
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish/osx-x64"
```

#### macOS ARM64 (Apple Silicon)
```bash
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish/osx-arm64"
```

## Platform-Specific Instructions

### Windows
1. **No additional requirements** - Executables will work on Windows 10+
2. **File size:** ~15-25 MB (trimmed)
3. **Dependencies:** None (self-contained)

### Linux
1. **Executable permissions:** `chmod +x FolderVision`
2. **File size:** ~20-30 MB (trimmed)
3. **Dependencies:** None (self-contained)
4. **Distribution compatibility:** Works on most modern Linux distributions

### macOS
1. **Executable permissions:** `chmod +x FolderVision`
2. **File size:** ~20-30 MB (trimmed)
3. **Dependencies:** None (self-contained)
4. **Gatekeeper:** May need to allow execution in Security & Privacy settings

## Distribution

### File Structure
```
publish/
├── win-x64/
│   └── FolderVision.exe     # Windows 64-bit executable
├── win-x86/
│   └── FolderVision.exe     # Windows 32-bit executable
├── linux-x64/
│   └── FolderVision         # Linux 64-bit executable
├── osx-x64/
│   └── FolderVision         # macOS Intel executable
└── osx-arm64/
    └── FolderVision         # macOS Apple Silicon executable
```

### Ready-to-Distribute Files
Each executable is completely self-contained and includes:
- ✅ .NET 6.0 runtime
- ✅ All application dependencies
- ✅ Native libraries
- ✅ No installation required

### Distribution Methods
1. **Direct Download** - Share the executable file
2. **ZIP Archive** - Package with documentation
3. **Installer** - Use tools like Inno Setup (Windows) or create .pkg (macOS)

## Troubleshooting

### Common Build Issues

#### "dotnet command not found"
```bash
# Install .NET SDK
# Windows: Download from Microsoft
# macOS: brew install dotnet
# Linux: Follow distribution-specific instructions
```

#### "Project could not be restored"
```bash
# Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore
```

#### "Insufficient disk space"
```bash
# Clean previous builds
dotnet clean
rm -rf bin obj publish
```

#### "Access denied" errors
```bash
# Windows: Run as Administrator
# Linux/macOS: Check file permissions
chmod +x build.sh
```

### Runtime Issues

#### "Application failed to start"
1. Verify target platform compatibility
2. Check antivirus software (may block executable)
3. Ensure sufficient disk space for temporary files

#### "Access denied to folders"
1. Run as Administrator (Windows)
2. Run with sudo (Linux/macOS) - **not recommended for normal use**
3. Adjust folder permissions

## Performance Optimizations

### Build Optimizations (Already Configured)
- **ReadyToRun (R2R)** - Faster startup
- **Assembly trimming** - Smaller file size
- **Single file publishing** - Easier distribution
- **Ahead-of-time compilation** - Better performance

### Runtime Optimizations
- **Server GC** - Better for large datasets
- **Concurrent GC** - Reduced pause times
- **Thread pool tuning** - Optimized for I/O operations

### Custom Build Options

#### Maximum Performance Build
```bash
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true \
  -p:TieredCompilation=true \
  -p:TieredPGO=true
```

#### Minimum Size Build
```bash
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:TrimMode=Link \
  -p:EnableCompressionInSingleFile=true
```

## Testing

### Automated Testing
```bash
# Run unit tests (if implemented)
dotnet test

# Run integration tests
dotnet test --filter Category=Integration
```

### Manual Testing Checklist
- [ ] **Scan small folder** (< 100 files)
- [ ] **Scan large folder** (> 10,000 files)
- [ ] **Scan network drive** (if available)
- [ ] **Scan removable drive** (USB, etc.)
- [ ] **Test HTML export**
- [ ] **Test HTML browser compatibility**
- [ ] **Test application on different OS versions**

### Browser Compatibility Testing
Open `test-html-output.html` in:
- [ ] Chrome/Chromium
- [ ] Firefox
- [ ] Safari
- [ ] Microsoft Edge
- [ ] Internet Explorer 11 (if legacy support needed)

## Build Configuration Reference

### Project Properties
```xml
<!-- Performance -->
<ServerGarbageCollection>true</ServerGarbageCollection>
<ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
<RetainVMGarbageCollection>true</RetainVMGarbageCollection>

<!-- Publishing -->
<PublishSingleFile>true</PublishSingleFile>
<PublishTrimmed>true</PublishTrimmed>
<PublishReadyToRun>true</PublishReadyToRun>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

### Environment Variables
```bash
# Increase memory limits for large scans
export DOTNET_GCHeapHardLimit=4000000000  # 4GB limit
export DOTNET_GCHighMemPercent=90         # Use 90% of available memory
```

## Support

### Getting Help
1. **Documentation** - Check this BUILD.md file
2. **Logs** - Check error logs in desktop folder
3. **Issues** - Report problems with detailed error messages
4. **Performance** - Include system specifications and scan details

### System Requirements
- **Minimum:** 1GB RAM, 100MB disk space
- **Recommended:** 4GB+ RAM, 500MB+ disk space
- **Large scans:** 8GB+ RAM recommended for folders with >100,000 files

---

**Build completed successfully? The executable is ready for distribution without any dependencies!**