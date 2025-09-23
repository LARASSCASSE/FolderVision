@echo off
echo ================================
echo    FolderVision Test Suite
echo ================================
echo.

REM Check if dotnet is available
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 6.0 SDK or later
    pause
    exit /b 1
)

echo .NET SDK Version:
dotnet --version
echo.

REM Build the application first
echo Building application for testing...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)
echo Build successful!
echo.

REM Create test directories if they don't exist
echo Creating test directories...
mkdir test-data 2>nul
mkdir test-data\small-folder 2>nul
mkdir test-data\small-folder\subfolder1 2>nul
mkdir test-data\small-folder\subfolder2 2>nul
mkdir test-data\large-folder 2>nul

REM Create test files
echo Creating test files...
echo Test file 1 > test-data\small-folder\file1.txt
echo Test file 2 > test-data\small-folder\file2.txt
echo Test file 3 > test-data\small-folder\subfolder1\file3.txt
echo Test file 4 > test-data\small-folder\subfolder2\file4.txt

REM Create larger test structure
for /L %%i in (1,1,50) do (
    mkdir test-data\large-folder\folder%%i 2>nul
    echo Large test file %%i > test-data\large-folder\folder%%i\file%%i.txt
)

echo Test data created successfully!
echo.

REM Test 1: Basic functionality
echo ================================
echo Test 1: Basic Functionality Test
echo ================================
echo Testing basic scanning functionality...
echo This will test scanning a small folder structure.
echo.
echo Starting FolderVision in test mode...
echo Note: You'll need to manually test the console interface
echo.
pause
start "FolderVision Test" dotnet run --configuration Release

echo.
echo While FolderVision is running, please test:
echo 1. Scan the test-data\small-folder directory
echo 2. Export results to HTML
echo 3. Open the HTML report
echo 4. Verify all folders and files are counted correctly
echo.
echo Expected results:
echo - Root folder: test-data\small-folder
echo - Subfolders: 2 (subfolder1, subfolder2)
echo - Files: 4 total
echo.
pause

REM Test 2: Performance test
echo ================================
echo Test 2: Performance Test
echo ================================
echo Testing with larger folder structure...
echo This will test scanning the large-folder with 50 subdirectories.
echo.
echo Please test scanning: test-data\large-folder
echo Expected results:
echo - Root folder: test-data\large-folder
echo - Subfolders: 50
echo - Files: 50 total
echo.
pause

REM Test 3: HTML output test
echo ================================
echo Test 3: HTML Output Test
echo ================================
echo Opening test HTML file to verify browser compatibility...
start test-html-output.html
echo.
echo Please verify in your browser:
echo 1. Gradient header displays correctly
echo 2. Statistics cards are properly formatted
echo 3. Folder tree structure is expandable/collapsible
echo 4. Expand All / Collapse All buttons work
echo 5. Responsive design works (try resizing window)
echo 6. Print preview looks good (Ctrl+P)
echo.
pause

REM Test 4: Error handling test
echo ================================
echo Test 4: Error Handling Test
echo ================================
echo Testing error handling with inaccessible directories...
echo.
echo Please test the following scenarios:
echo 1. Try to scan a non-existent directory
echo 2. Try to scan a directory without permissions (if possible)
echo 3. Cancel a scan in progress (press ESC)
echo 4. Test with very long file paths (if possible)
echo.
echo Verify that:
echo - Application doesn't crash
echo - Error messages are clear and helpful
echo - Application recovers gracefully
echo.
pause

REM Test 5: Drive scanning test
echo ================================
echo Test 5: Drive Scanning Test
echo ================================
echo Testing drive detection and scanning...
echo.
echo Please test:
echo 1. Run FolderVision and select "Scan Drives"
echo 2. Verify all available drives are detected
echo 3. Check drive information (size, free space, type)
echo 4. Test scanning a small drive or partition
echo.
echo Verify that:
echo - Drive types are correctly identified (Fixed, Removable, Network, etc.)
echo - Drive sizes and free space are accurate
echo - Network drives show appropriate warnings
echo - Empty or inaccessible drives are handled properly
echo.
pause

REM Test 6: Threading and cancellation test
echo ================================
echo Test 6: Threading and Cancellation Test
echo ================================
echo Testing multi-threading and cancellation...
echo.
echo Please test:
echo 1. Start a scan of a large directory
echo 2. Observe the progress display
echo 3. Press ESC to cancel the scan
echo 4. Verify graceful cancellation
echo 5. Try different thread count settings
echo.
pause

REM Cleanup
echo ================================
echo Test Cleanup
echo ================================
echo.
set /p cleanup="Remove test data? (y/n): "
if /i "%cleanup%"=="y" (
    echo Removing test data...
    rmdir /s /q test-data 2>nul
    echo Test data removed.
) else (
    echo Test data preserved in test-data folder.
)

echo.
echo ================================
echo Test Suite Complete
echo ================================
echo.
echo Manual Testing Checklist:
echo [ ] Basic folder scanning works
echo [ ] HTML export generates correctly
echo [ ] HTML report displays properly in browser
echo [ ] Drive detection and scanning works
echo [ ] Error handling is robust
echo [ ] Multi-threading performs well
echo [ ] Cancellation works gracefully
echo [ ] Application doesn't crash under normal use
echo [ ] Memory usage is reasonable
echo [ ] File counts are accurate
echo.
echo If all tests pass, the application is ready for release!
echo.
pause