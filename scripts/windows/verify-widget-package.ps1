#Requires -Version 5.1
param(
    [Parameter(Mandatory = $true)]
    [string]$MsixPath
)

$ErrorActionPreference = 'Stop'

$extractDir = Join-Path $env:TEMP "onetaphabits-widget-verify"
if (Test-Path $extractDir) {
    Remove-Item $extractDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $extractDir | Out-Null

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory((Resolve-Path $MsixPath), $extractDir)

$required = @(
    'WidgetHost\OneTapHabits.WidgetHost.exe',
    'WidgetHost\Templates\HabitsTemplate.json',
    # Widget Board resolves picker assets from package root (released builds use these names).
    'appiconStoreLogo.scale-200.png',
    'appiconLogo.scale-200.png',
    'appiconWideTile.scale-200.png'
)

$missing = @()
foreach ($relative in $required) {
    if (-not (Test-Path (Join-Path $extractDir $relative))) {
        $missing += $relative
    }
}

$manifest = Get-Content (Join-Path $extractDir 'AppxManifest.xml') -Raw
if ($manifest -match 'ProviderAssets\\widget-') {
    Write-Error 'AppxManifest still references Public\ProviderAssets\widget-* paths. Widget Board will hide the provider; use package-root appicon*.scale-200.png paths.'
}
foreach ($needle in @('appiconStoreLogo.scale-200.png', 'appiconLogo.scale-200.png', 'appiconWideTile.scale-200.png')) {
    if ($manifest -notmatch [regex]::Escape($needle)) {
        $missing += "AppxManifest reference: $needle"
    }
}

if ($missing.Count -gt 0) {
    Write-Error "Widget package verification failed. Missing:`n$($missing -join "`n")"
}

Write-Host 'Widget package verification passed.'
