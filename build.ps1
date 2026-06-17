param(
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$proj = Join-Path $PSScriptRoot "src\ComTray\ComTray.csproj"
$out  = Join-Path $PSScriptRoot "dist"

dotnet publish $proj -c Release -r $Runtime --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $out

Write-Host ""
Write-Host "Built: $(Join-Path $out 'ComTray.exe')"
