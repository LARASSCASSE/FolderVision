@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0.."

echo.
echo  ====================================================
echo   FolderVision — Build installeur
echo  ====================================================
echo.

:: ── 1. Publier l'application ─────────────────────────────
echo  [1/2] Publication de l'application...
dotnet publish CODE\FolderVision.Wpf ^
    --configuration Release ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -o publish_out ^
    --nologo

if %errorlevel% neq 0 (
    echo.
    echo  ERREUR : la publication a echoue.
    pause & exit /b 1
)
echo  OK : publish_out\FolderVision.Wpf.exe genere.
echo.

:: ── 2. Compiler l'installeur Inno Setup ──────────────────
echo  [2/2] Compilation de l'installeur...

set ISCC=
for %%P in (
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    "C:\Program Files\Inno Setup 6\ISCC.exe"
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
) do (
    if exist %%P set ISCC=%%~P
)

if "!ISCC!"=="" (
    echo.
    echo  ERREUR : Inno Setup introuvable.
    echo  Telecharger gratuitement : https://jrsoftware.org/isdl.php
    echo.
    pause & exit /b 1
)

"!ISCC!" installer\FolderVision.iss

if %errorlevel% neq 0 (
    echo.
    echo  ERREUR : compilation Inno Setup echouee.
    pause & exit /b 1
)

:: ── 3. Signer les executables ────────────────────────────
echo  [3/3] Signature du code (suppression SmartScreen)...
powershell -ExecutionPolicy Bypass -File installer\sign.ps1

echo.
echo  ====================================================
echo   Succes !  ->  FolderVision_Setup.exe  (signe)
echo  ====================================================
echo.
pause
