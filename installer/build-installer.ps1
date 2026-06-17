param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$msi  = Join-Path $root "dist\ComTray-$Version.msi"

# Make sure the published exe the MSI wraps is current.
& (Join-Path $root "build.ps1")

wix build (Join-Path $PSScriptRoot "ComTray.wxs") -arch x64 -d DistDir="$root\dist" -d RepoDir="$root" -o $msi

$hash = (Get-FileHash $msi -Algorithm SHA256).Hash
Write-Host ""
Write-Host "Built: $msi"
Write-Host "SHA256: $hash"
