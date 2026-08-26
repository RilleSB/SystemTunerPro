param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "..\DiskCleanerGUI.Avalonia.csproj"
$root = Split-Path $project -Parent
$outRoot = Join-Path $root "release"
$singleDir = Join-Path $outRoot "single-file"
$portableDir = Join-Path $outRoot "portable"
$zipPath = Join-Path $outRoot "SystemTunerPro-$Version-win-x64-portable.zip"

if (Test-Path $outRoot) {
    Remove-Item -LiteralPath $outRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $singleDir, $portableDir | Out-Null

dotnet publish $project -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:Version=$Version `
    -o $singleDir

dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    -p:Version=$Version `
    -o $portableDir

Compress-Archive -Path (Join-Path $portableDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "Single-file EXE: $singleDir\SystemTunerPro.exe"
Write-Host "Portable ZIP:    $zipPath"
