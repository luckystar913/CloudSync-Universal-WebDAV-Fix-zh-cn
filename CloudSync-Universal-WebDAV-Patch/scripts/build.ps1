# ============================================================
#  CloudSync build script (PowerShell)
#  Usage: .\build.ps1 [-Configuration Release|Debug]
# ============================================================
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

# Check dependencies
$requiredDlls = @(
    "StardewModdingAPI.dll",
    "0Harmony.dll",
    "MonoGame.Framework.dll",
    "Stardew Valley.dll",
    "SMAPI.Toolkit.CoreInterfaces.dll"
)

$missing = @()
foreach ($dll in $requiredDlls) {
    if (-not (Test-Path $dll)) {
        $missing += $dll
    }
}

if ($missing.Count -gt 0) {
    Write-Host "[ERROR] Missing dependency DLLs in project root:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  $_" }
    Write-Host "Please copy them from your Stardew Valley game folder. See README.md." -ForegroundColor Yellow
    exit 1
}

Write-Host "Building CloudSync ($Configuration)..." -ForegroundColor Cyan
dotnet build CloudSync.csproj -c $Configuration -v minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAILED] Build failed." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "[OK] Build succeeded." -ForegroundColor Green
Write-Host "Output: bin\$Configuration\net6.0\CloudSync.dll"
