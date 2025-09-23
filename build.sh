#!/bin/bash

echo "================================"
echo "    FolderVision Build Script"
echo "================================"
echo

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK is not installed or not in PATH"
    echo "Please install .NET 6.0 SDK or later from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo ".NET SDK Version:"
dotnet --version
echo

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf bin obj publish
echo

# Build for Linux x64 (standalone executable)
echo "Building FolderVision for Linux x64 (standalone)..."
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish/linux-x64"

if [ $? -ne 0 ]; then
    echo "ERROR: Build failed for Linux x64"
    exit 1
fi

# Build for Windows x64
echo "Building FolderVision for Windows x64 (standalone)..."
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish/win-x64"

# Build for macOS x64
echo "Building FolderVision for macOS x64 (standalone)..."
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish/osx-x64"

# Build for macOS ARM64 (Apple Silicon)
echo "Building FolderVision for macOS ARM64 (standalone)..."
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish/osx-arm64"

echo
echo "================================"
echo "     Build Completed Successfully!"
echo "================================"
echo
echo "Standalone executables created in:"
echo "  - Linux x64:   publish/linux-x64/FolderVision"
echo "  - Windows x64: publish/win-x64/FolderVision.exe"
echo "  - macOS x64:   publish/osx-x64/FolderVision"
echo "  - macOS ARM64: publish/osx-arm64/FolderVision"
echo

# Show file sizes
echo "File sizes:"
if [ -f "publish/linux-x64/FolderVision" ]; then
    echo "  Linux x64:   $(stat -f%z publish/linux-x64/FolderVision 2>/dev/null || stat -c%s publish/linux-x64/FolderVision) bytes"
fi
if [ -f "publish/win-x64/FolderVision.exe" ]; then
    echo "  Windows x64: $(stat -f%z publish/win-x64/FolderVision.exe 2>/dev/null || stat -c%s publish/win-x64/FolderVision.exe) bytes"
fi
if [ -f "publish/osx-x64/FolderVision" ]; then
    echo "  macOS x64:   $(stat -f%z publish/osx-x64/FolderVision 2>/dev/null || stat -c%s publish/osx-x64/FolderVision) bytes"
fi
if [ -f "publish/osx-arm64/FolderVision" ]; then
    echo "  macOS ARM64: $(stat -f%z publish/osx-arm64/FolderVision 2>/dev/null || stat -c%s publish/osx-arm64/FolderVision) bytes"
fi
echo

echo "These executables do NOT require .NET to be installed on the target machine."
echo "They are completely self-contained and ready to distribute."
echo

# Make executables executable on Unix systems
chmod +x publish/*/FolderVision 2>/dev/null

echo "Build complete! Press Enter to continue..."
read