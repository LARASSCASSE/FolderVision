# FolderVision - Code signing script
# Usage: powershell -ExecutionPolicy Bypass -File installer\sign.ps1
#
# First run: creates & installs the certificate from FolderVision.pfx
# Next runs: reuses the existing installed certificate
param(
    [string[]]$Targets = @("..\FolderVision_Setup.exe", "..\publish_out\FolderVision.Wpf.exe"),
    [string]$PfxPassword = "FolderVision2026!"
)

$Thumbprint  = "8896F2183BE3F5D7B851255192B670CBCEC6F31C"
$PfxPath     = Join-Path $PSScriptRoot "FolderVision.pfx"

# --- 1. Load certificate ---
Write-Host ""
Write-Host "[1/3] Loading certificate..." -ForegroundColor Cyan

$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Thumbprint -eq $Thumbprint } | Select-Object -First 1

if (-not $cert) {
    if (-not (Test-Path $PfxPath)) {
        Write-Host "ERROR: FolderVision.pfx not found at $PfxPath" -ForegroundColor Red
        exit 1
    }
    Write-Host "      Importing certificate from PFX..."
    $pwd = ConvertTo-SecureString $PfxPassword -Force -AsPlainText
    $cert = Import-PfxCertificate -FilePath $PfxPath -CertStoreLocation "Cert:\CurrentUser\My" -Password $pwd
    Write-Host "      Imported: $($cert.Thumbprint)" -ForegroundColor Green
} else {
    Write-Host "      Found: $($cert.Thumbprint)" -ForegroundColor Green
}

# --- 2. Install into trust stores ---
Write-Host ""
Write-Host "[2/3] Trust stores..." -ForegroundColor Cyan

foreach ($storeName in @("Root", "TrustedPublisher")) {
    foreach ($storeScope in @("CurrentUser", "LocalMachine")) {
        try {
            $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($storeName, $storeScope)
            $store.Open("ReadWrite")
            $already = $store.Certificates | Where-Object { $_.Thumbprint -eq $cert.Thumbprint }
            if (-not $already) {
                $store.Add($cert)
                Write-Host "      + $storeScope\$storeName" -ForegroundColor Green
            } else {
                Write-Host "      = $storeScope\$storeName (already OK)" -ForegroundColor DarkGray
            }
            $store.Close()
        } catch {
            Write-Host "      ! $storeScope\$storeName (run as admin for all users)" -ForegroundColor Yellow
        }
    }
}

# --- 3. Sign files ---
Write-Host ""
Write-Host "[3/3] Signing..." -ForegroundColor Cyan

$tsServers = @(
    "http://timestamp.digicert.com",
    "http://timestamp.sectigo.com",
    "http://timestamp.globalsign.com/tsa/r6advanced1"
)

foreach ($target in $Targets) {
    $absPath = [IO.Path]::GetFullPath([IO.Path]::Combine($PSScriptRoot, $target))

    if (-not (Test-Path $absPath)) {
        Write-Host "      SKIP (not found): $absPath" -ForegroundColor DarkGray
        continue
    }

    $signed = $false
    foreach ($ts in $tsServers) {
        try {
            $r = Set-AuthenticodeSignature -FilePath $absPath -Certificate $cert -HashAlgorithm SHA256 -TimestampServer $ts -ErrorAction Stop
            if ($r.Status -eq "Valid") {
                Write-Host "      OK   $([IO.Path]::GetFileName($absPath))  (ts: $ts)" -ForegroundColor Green
                $signed = $true
                break
            }
        } catch { }
    }

    if (-not $signed) {
        # Sign without timestamp (signature expires when cert expires in 2036)
        $r = Set-AuthenticodeSignature -FilePath $absPath -Certificate $cert -HashAlgorithm SHA256
        $color = if ($r.Status -eq "Valid") { "Yellow" } else { "Red" }
        $note  = if ($r.Status -eq "Valid") { "(no timestamp - valid until 2036)" } else { $r.StatusMessage }
        Write-Host "      $($r.Status)  $([IO.Path]::GetFileName($absPath))  $note" -ForegroundColor $color
    }
}

Write-Host ""
Write-Host "Done. SmartScreen will trust these files on this machine." -ForegroundColor Cyan
Write-Host ""
