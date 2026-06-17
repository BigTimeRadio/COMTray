# Regenerates the winget manifests from a built MSI, filling in the version,
# download URL, SHA256 and ProductCode so they always match the actual file.
# Run installer\build-installer.ps1 first, then this.
param(
    [string]$Owner = "BigTimeRadio",
    [string]$Repo = "COMTray",
    [string]$Identifier = "W1BTR.COMTray",
    [string]$Publisher = "W1BTR",
    [string]$Msi
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

if (-not $Msi) {
    $Msi = Get-ChildItem (Join-Path $root "dist") -Filter "ComTray-*.msi" |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $Msi -or -not (Test-Path $Msi)) {
    throw "No MSI found. Run installer\build-installer.ps1 first."
}

function Get-MsiProperty($path, $name) {
    $wi = New-Object -ComObject WindowsInstaller.Installer
    $db = $wi.GetType().InvokeMember("OpenDatabase", "InvokeMethod", $null, $wi, @($path, 0))
    $view = $db.GetType().InvokeMember("OpenView", "InvokeMethod", $null, $db, @("SELECT Value FROM Property WHERE Property='$name'"))
    [void]$view.GetType().InvokeMember("Execute", "InvokeMethod", $null, $view, $null)
    $rec = $view.GetType().InvokeMember("Fetch", "InvokeMethod", $null, $view, $null)
    ($rec.GetType().InvokeMember("StringData", "GetProperty", $null, $rec, @(1))).Trim()
}

$version = Get-MsiProperty $Msi "ProductVersion"
$product = Get-MsiProperty $Msi "ProductCode"
$hash = (Get-FileHash $Msi -Algorithm SHA256).Hash
$fileName = Split-Path $Msi -Leaf
$url = "https://github.com/$Owner/$Repo/releases/download/v$version/$fileName"

$letter = $Identifier.Substring(0, 1).ToLower()
$dir = Join-Path $root "winget\manifests\$letter\$($Identifier -replace '\.', '\')\$version"
New-Item -ItemType Directory -Force -Path $dir | Out-Null

@"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.6.0.schema.json
PackageIdentifier: $Identifier
PackageVersion: $version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.6.0
"@ | Set-Content (Join-Path $dir "$Identifier.yaml")

@"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.6.0.schema.json
PackageIdentifier: $Identifier
PackageVersion: $version
InstallerType: wix
Scope: machine
InstallModes:
  - interactive
  - silent
  - silentWithProgress
UpgradeBehavior: install
Installers:
  - Architecture: x64
    InstallerUrl: $url
    InstallerSha256: $hash
    ProductCode: '$product'
ManifestType: installer
ManifestVersion: 1.6.0
"@ | Set-Content (Join-Path $dir "$Identifier.installer.yaml")

Write-Host "Wrote manifests to $dir"
Write-Host "Version $version  Product $product"
Write-Host "Upload $fileName to the GitHub release tagged v$version so the URL resolves:"
Write-Host "  $url"
Write-Host ""
Write-Host "NOTE: the locale manifest ($Identifier.locale.en-US.yaml) is kept by hand; copy it forward for new versions."
