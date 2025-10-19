# FolderVision Deployment Guide

## Release Checklist

### Pre-Release Testing
- [ ] **Unit Tests Pass** - All automated tests pass
- [ ] **Manual Testing Complete** - Run `test-application.cmd` successfully
- [ ] **Browser Compatibility** - HTML reports work in Chrome, Firefox, Safari, Edge
- [ ] **Platform Testing** - Test on Windows, Linux, macOS (if possible)
- [ ] **Performance Testing** - Test with large directory structures (>10,000 files)
- [ ] **Error Handling** - Test edge cases (network drives, permissions, cancellation)

### Build Verification
- [ ] **Clean Build** - Fresh build from source without errors
- [ ] **Release Configuration** - Built with Release configuration
- [ ] **Self-Contained** - Executables don't require .NET installation
- [ ] **File Sizes Reasonable** - Each executable under 30MB
- [ ] **All Platforms Built** - Windows (x64, x86), Linux (x64), macOS (x64, ARM64)

### Documentation
- [ ] **BUILD.md Updated** - Build instructions are current
- [ ] **README.md Exists** - User documentation available
- [ ] **Version Numbers** - Assembly version updated in .csproj
- [ ] **Change Log** - Document new features and fixes

## Distribution Package

### File Structure
```
FolderVision-v1.0.0/
├── README.md
├── LICENSE
├── CHANGELOG.md
├── Windows/
│   ├── FolderVision-x64.exe
│   └── FolderVision-x86.exe
├── Linux/
│   └── FolderVision-linux-x64
├── macOS/
│   ├── FolderVision-macos-x64
│   └── FolderVision-macos-arm64
└── Documentation/
    ├── BUILD.md
    ├── DEPLOYMENT.md
    └── test-html-output.html
```

### Release Commands

```bash
# 1. Clean previous builds
dotnet clean
rm -rf bin obj publish

# 2. Build all platforms
./build.sh  # or build.cmd on Windows

# 3. Create distribution folder
mkdir -p FolderVision-v1.0.0/{Windows,Linux,macOS,Documentation}

# 4. Copy executables
cp publish/win-x64/FolderVision.exe FolderVision-v1.0.0/Windows/FolderVision-x64.exe
cp publish/win-x86/FolderVision.exe FolderVision-v1.0.0/Windows/FolderVision-x86.exe
cp publish/linux-x64/FolderVision FolderVision-v1.0.0/Linux/FolderVision-linux-x64
cp publish/osx-x64/FolderVision FolderVision-v1.0.0/macOS/FolderVision-macos-x64
cp publish/osx-arm64/FolderVision FolderVision-v1.0.0/macOS/FolderVision-macos-arm64

# 5. Copy documentation
cp *.md FolderVision-v1.0.0/Documentation/
cp test-html-output.html FolderVision-v1.0.0/Documentation/

# 6. Create archives
zip -r FolderVision-v1.0.0-Windows.zip FolderVision-v1.0.0/Windows/
tar -czf FolderVision-v1.0.0-Linux.tar.gz FolderVision-v1.0.0/Linux/
tar -czf FolderVision-v1.0.0-macOS.tar.gz FolderVision-v1.0.0/macOS/
zip -r FolderVision-v1.0.0-Complete.zip FolderVision-v1.0.0/
```

## Platform-Specific Instructions

### Windows Deployment

#### Simple Distribution
1. **Single Executable** - Just distribute `FolderVision.exe`
2. **No Installation Required** - Users can run directly
3. **Desktop Shortcut** - Users can create shortcuts manually

#### Advanced Distribution (Optional)
```nsis
; Inno Setup script example
[Setup]
AppName=FolderVision
AppVersion=1.0.0
DefaultDirName={pf}\FolderVision
DefaultGroupName=FolderVision
OutputBaseFilename=FolderVision-Setup-v1.0.0
Compression=lzma2
SolidCompression=yes

[Files]
Source: "FolderVision.exe"; DestDir: "{app}"

[Icons]
Name: "{group}\FolderVision"; Filename: "{app}\FolderVision.exe"
Name: "{commondesktop}\FolderVision"; Filename: "{app}\FolderVision.exe"
```

### Linux Deployment

#### Package Structure
```bash
# Create .deb package structure
mkdir -p foldervision-1.0.0/DEBIAN
mkdir -p foldervision-1.0.0/usr/local/bin
mkdir -p foldervision-1.0.0/usr/share/applications
mkdir -p foldervision-1.0.0/usr/share/doc/foldervision

# Copy executable
cp FolderVision-linux-x64 foldervision-1.0.0/usr/local/bin/foldervision
chmod +x foldervision-1.0.0/usr/local/bin/foldervision

# Create control file
cat > foldervision-1.0.0/DEBIAN/control << EOF
Package: foldervision
Version: 1.0.0
Section: utils
Priority: optional
Architecture: amd64
Maintainer: FolderVision Team <contact@example.com>
Description: Professional folder scanning tool
 Multi-threaded folder scanner with beautiful HTML reports.
 Scans directory structures and generates detailed reports.
EOF

# Build package
dpkg-deb --build foldervision-1.0.0
```

#### AppImage Distribution
```bash
# Download AppImageTool
wget https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x appimagetool-x86_64.AppImage

# Create AppDir structure
mkdir -p FolderVision.AppDir/usr/bin
cp FolderVision-linux-x64 FolderVision.AppDir/usr/bin/foldervision

# Create desktop file
cat > FolderVision.AppDir/FolderVision.desktop << EOF
[Desktop Entry]
Type=Application
Name=FolderVision
Exec=foldervision
Icon=foldervision
Categories=Utility;
EOF

# Create AppRun
cat > FolderVision.AppDir/AppRun << 'EOF'
#!/bin/bash
cd "$(dirname "$0")"
exec ./usr/bin/foldervision "$@"
EOF
chmod +x FolderVision.AppDir/AppRun

# Build AppImage
./appimagetool-x86_64.AppImage FolderVision.AppDir FolderVision-x86_64.AppImage
```

### macOS Deployment

#### Simple Distribution
```bash
# Create application bundle structure
mkdir -p FolderVision.app/Contents/{MacOS,Resources}

# Copy executable
cp FolderVision-macos-x64 FolderVision.app/Contents/MacOS/FolderVision
chmod +x FolderVision.app/Contents/MacOS/FolderVision

# Create Info.plist
cat > FolderVision.app/Contents/Info.plist << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>FolderVision</string>
    <key>CFBundleIdentifier</key>
    <string>com.foldervision.app</string>
    <key>CFBundleName</key>
    <string>FolderVision</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
</dict>
</plist>
EOF

# Create DMG
hdiutil create -volname "FolderVision" -srcfolder FolderVision.app -ov -format UDZO FolderVision-v1.0.0.dmg
```

## Security Considerations

### Code Signing

#### Windows
```powershell
# Sign with certificate (if available)
signtool sign /fd SHA256 /tr http://timestamp.comodoca.com /td SHA256 /a FolderVision.exe
```

#### macOS
```bash
# Sign with Apple Developer Certificate (if available)
codesign --force --verify --verbose --sign "Developer ID Application: Your Name" FolderVision.app
```

### Virus Scanner Compatibility
- **Test with major antivirus software**
- **Submit to VirusTotal** for verification
- **Consider code signing** to reduce false positives

## Quality Assurance

### Automated Testing
```bash
# Run all tests
dotnet test --configuration Release --verbosity normal

# Generate test coverage report (if tests exist)
dotnet test --collect:"XPlat Code Coverage"
```

### Performance Benchmarks
- **Small folder** (< 100 files): < 1 second
- **Medium folder** (1,000 files): < 10 seconds
- **Large folder** (10,000 files): < 60 seconds
- **Huge folder** (100,000+ files): < 300 seconds

### Memory Usage
- **Baseline**: ~15-30 MB
- **Small scan**: +5-10 MB
- **Large scan**: +50-100 MB
- **Maximum**: Should not exceed 1GB for normal use

## Distribution Channels

### Direct Download
- **GitHub Releases** - Primary distribution
- **Official Website** - Secondary distribution
- **Documentation** - Include download links

### Package Managers

#### Windows
```powershell
# Chocolatey package (if published)
choco install foldervision

# Winget package (if published)
winget install FolderVision.FolderVision
```

#### Linux
```bash
# Snap package (if published)
sudo snap install foldervision

# AppImage (direct download)
wget https://github.com/user/repo/releases/download/v1.0.0/FolderVision-x86_64.AppImage
chmod +x FolderVision-x86_64.AppImage
```

#### macOS
```bash
# Homebrew (if published)
brew install --cask foldervision
```

## Post-Release

### Monitoring
- [ ] **Download Statistics** - Track download counts
- [ ] **User Feedback** - Monitor issues and feature requests
- [ ] **Performance Reports** - Collect performance data from users
- [ ] **Error Reports** - Monitor crash reports and error logs

### Updates
- [ ] **Version Tracking** - Maintain version history
- [ ] **Changelog** - Document all changes
- [ ] **Migration Guide** - If breaking changes exist
- [ ] **Deprecation Notice** - For old versions

### Support
- [ ] **User Documentation** - Keep up to date
- [ ] **FAQ** - Common questions and solutions
- [ ] **Issue Tracking** - GitHub Issues or similar
- [ ] **Community** - Discord, Reddit, or forums

## Rollback Plan

### If Issues Are Discovered
1. **Immediate Actions**
   - Remove download links
   - Post notice on distribution channels
   - Document the issue

2. **Investigation**
   - Reproduce the issue
   - Identify root cause
   - Develop fix

3. **Resolution**
   - Create patch version
   - Test thoroughly
   - Re-release with version bump

4. **Communication**
   - Notify affected users
   - Provide upgrade instructions
   - Document lessons learned

---

## Final Release Command

```bash
# Complete release build and packaging
./build.sh
./test-application.cmd  # Verify everything works
# Create distribution packages
# Upload to distribution channels
# Update documentation
# Announce release
```

**The application is now ready for professional distribution!** 🚀