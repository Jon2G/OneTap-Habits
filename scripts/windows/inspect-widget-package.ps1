#Requires -Version 5.1
param(
    [Parameter(Mandatory = $true)]
    [string]$MsixPath,
    [string]$OutputDir = (Join-Path $env:TEMP "onetaphabits-widget-inspect")
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $MsixPath)) {
    throw "MSIX not found: $MsixPath"
}

if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory((Resolve-Path $MsixPath), $OutputDir)

Write-Host "Extracted to $OutputDir"
Write-Host ''
Write-Host 'Widget-related paths:'
$paths = @(
    'WidgetHost\OneTapHabits.WidgetHost.exe',
    'WidgetHost\Templates\HabitsTemplate.json',
    'appiconStoreLogo.scale-200.png',
    'appiconLogo.scale-200.png',
    'appiconWideTile.scale-200.png',
    'Public\ProviderAssets\widget-store-logo.png',
    'Public\ProviderAssets\widget-icon.png',
    'Public\ProviderAssets\widget-screenshot.png'
)
foreach ($relative in $paths) {
    $full = Join-Path $OutputDir $relative
    if (Test-Path $full) {
        $item = Get-Item $full
        Write-Host "[ok] $relative ($($item.Length) bytes)"
    }
    else {
        Write-Host "[missing] $relative"
    }
}

Write-Host ''
Write-Host 'Manifest widget icon references:'
Select-String -Path (Join-Path $OutputDir 'AppxManifest.xml') -Pattern 'appicon.*scale-200|ProviderAssets|WidgetHost|widgets' |
    ForEach-Object { $_.Line.Trim() }
