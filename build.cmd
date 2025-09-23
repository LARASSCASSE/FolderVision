@echo off
echo ================================
echo    FolderVision Build Script
echo ================================
echo.

REM Check if dotnet is available
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 6.0 SDK or later from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo .NET SDK Version:
dotnet --version
echo.

REM Clean previous builds
echo Cleaning previous builds...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
if exist "publish" rmdir /s /q "publish"
echo.

REM Build for Windows x64 (standalone executable)
echo Building FolderVision for Windows x64 (standalone)...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish\win-x64"

if %errorlevel% neq 0 (
    echo ERROR: Build failed for Windows x64
    pause
    exit /b 1
)

REM Build for Windows x86
echo Building FolderVision for Windows x86 (standalone)...
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish\win-x86"

REM Build for Linux x64
echo Building FolderVision for Linux x64 (standalone)...
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish\linux-x64"

REM Build for macOS x64
echo Building FolderVision for macOS x64 (standalone)...
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish\osx-x64"

REM Build for macOS ARM64 (Apple Silicon)
echo Building FolderVision for macOS ARM64 (standalone)...
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "publish\osx-arm64"

echo.
echo ================================
echo     Build Completed Successfully!
echo ================================
echo.
echo Standalone executables created in:
echo   - Windows x64: publish\win-x64\FolderVision.exe
echo   - Windows x86: publish\win-x86\FolderVision.exe
echo   - Linux x64:   publish\linux-x64\FolderVision
echo   - macOS x64:   publish\osx-x64\FolderVision
echo   - macOS ARM64: publish\osx-arm64\FolderVision
echo.

REM Show file sizes
echo File sizes:
for %%f in ("publish\win-x64\FolderVision.exe") do echo   Windows x64: %%~zf bytes
for %%f in ("publish\win-x86\FolderVision.exe") do echo   Windows x86: %%~zf bytes
for %%f in ("publish\linux-x64\FolderVision") do echo   Linux x64:   %%~zf bytes
for %%f in ("publish\osx-x64\FolderVision") do echo   macOS x64:   %%~zf bytes
for %%f in ("publish\osx-arm64\FolderVision") do echo   macOS ARM64: %%~zf bytes
echo.

echo These executables do NOT require .NET to be installed on the target machine.
echo They are completely self-contained and ready to distribute.
echo.
pause