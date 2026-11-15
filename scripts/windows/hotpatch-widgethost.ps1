#Requires -Version 5.1
<#
.SYNOPSIS
  Fast WidgetHost test without full MAUI publish.

  Builds WidgetHost only, patches it into a loose package layout from the last MSIX,
  then re-registers (dev deploy). Much faster than install-widget-debug.ps1.

.EXAMPLE
  .\hotpatch-widgethost.ps1
  # Win+W unpin/repin, then:
  powershell -File c:\dev\tmp\diagnose-blank-now.ps1
#>
param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$repo = 'c:\dev\repos\OneTap-Habits'
Set-Location $repo
if (Test-Path 'global.maui.ci.json') {
    Copy-Item 'global.maui.ci.json' 'global.json' -Force
}

$outDir = Join-Path $repo 'artifacts\widgethost\Debug\win-x64'
if (-not $SkipBuild) {
    Write-Host '== Build WidgetHost only ==' -ForegroundColor Cyan
    dotnet build 'src\OneTapHabits.WidgetHost\OneTapHabits.WidgetHost.csproj' `
        -c Debug -r win-x64 `
        -p:OutputPath=$outDir `
        -p:WindowsPackageType=None `
        -p:GenerateProjectPriFile=false `
        -p:EnableMsixTooling=false `
        -p:EnableCoreMrtTooling=false `
        -p:EnableDefaultPriItems=false
    if ($LASTEXITCODE -ne 0) { throw "WidgetHost build failed: $LASTEXITCODE" }
}

$msix = Get-ChildItem (Join-Path $repo 'src\bin\Debug\net9.0-windows10.0.22000.0\win10-x64\AppPackages') -Recurse -Filter 'OneTapHabits_*_x64_Debug.msix' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $msix) {
    $msix = Get-Item (Join-Path $repo 'artifacts\OneTapHabits-debug.msix') -ErrorAction SilentlyContinue
}
if (-not $msix) { throw 'No debug MSIX found. Run install-widget-debug.ps1 once first.' }

$loose = 'c:\dev\tmp\onetap-loose-layout'
Write-Host "Source MSIX: $($msix.FullName)"
Write-Host "Loose layout: $loose"

# Unregister BEFORE deleting the loose layout — that folder is the live InstallLocation.
# Prefer in-place replace when already registered to this layout (avoids wiping LocalState).
Get-Process -Name 'OneTapHabits.WidgetHost','OneTapHabits','WidgetBoard' -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

$existing = @(Get-AppxPackage | Where-Object { $_.Name -like '*onetap*' })
$alreadyHere = $existing | Where-Object {
    $_.InstallLocation -and ($_.InstallLocation.TrimEnd('\') -ieq $loose.TrimEnd('\'))
} | Select-Object -First 1

if ($alreadyHere) {
    Write-Host "In-place patch at $loose (keeps LocalState)"
    $hostDir = Join-Path $loose 'WidgetHost'
    New-Item -ItemType Directory -Path $hostDir -Force | Out-Null
    Copy-Item -Path (Join-Path $outDir '*') -Destination $hostDir -Recurse -Force
    $manifest = Join-Path $loose 'AppxManifest.xml'
    $raw = Get-Content $manifest -Raw
    if ($raw -notmatch 'RegisterProcessAsComServer') {
        Write-Host 'WARNING: manifest missing RegisterProcessAsComServer' -ForegroundColor Yellow
    } else {
        Write-Host 'Manifest has RegisterProcessAsComServer'
    }
} else {
    foreach ($pkg in $existing) {
        Write-Host "Removing $($pkg.PackageFullName) before layout replace"
        Remove-AppxPackage -Package $pkg.PackageFullName
    }
    Start-Sleep -Seconds 1

    if (Test-Path $loose) {
        Remove-Item $loose -Recurse -Force
    }
    New-Item -ItemType Directory -Path $loose | Out-Null

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($msix.FullName, $loose)

    # Drop signature so -Register works in Developer Mode
    Remove-Item (Join-Path $loose 'AppxSignature.p7x') -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $loose 'AppxBlockMap.xml') -Force -ErrorAction SilentlyContinue

    $hostDir = Join-Path $loose 'WidgetHost'
    New-Item -ItemType Directory -Path $hostDir -Force | Out-Null
    Write-Host '== Patch WidgetHost into loose layout ==' -ForegroundColor Cyan
    Copy-Item -Path (Join-Path $outDir '*') -Destination $hostDir -Recurse -Force

    $manifest = Join-Path $loose 'AppxManifest.xml'
    $raw = Get-Content $manifest -Raw
    if ($raw -notmatch 'RegisterProcessAsComServer') {
        Write-Host 'WARNING: manifest missing RegisterProcessAsComServer' -ForegroundColor Yellow
    } else {
        Write-Host 'Manifest has RegisterProcessAsComServer'
    }

    Write-Host '== Register loose layout ==' -ForegroundColor Cyan
    Add-AppxPackage -Register $manifest
    Write-Host 'Registered.'
}

$pkg = Get-AppxPackage | Where-Object { $_.Name -like '*onetap*' } | Select-Object -First 1
Write-Host "InstallLocation: $($pkg.InstallLocation)"
Write-Host "SignatureKind: $($pkg.SignatureKind)"

$ls = Join-Path $env:LOCALAPPDATA "Packages\$($pkg.PackageFamilyName)\LocalState"
New-Item -ItemType Directory -Path $ls -Force | Out-Null
Set-Content -Path (Join-Path $ls 'widget_debug.enable') -Value "enabled $(Get-Date -Format o)" -Encoding UTF8
foreach ($f in @('widget_host_boot.log', 'widget_debug.log', 'widget_last_data.json', 'widget_last_template.json')) {
    Remove-Item (Join-Path $ls $f) -Force -ErrorAction SilentlyContinue
}

Write-Host ''
Write-Host 'Hotpatch registered. Next:' -ForegroundColor Green
Write-Host '  1) Open OneTap Habits (writes snapshot)'
Write-Host '  2) Win+W -> unpin/repin Today''s habits'
Write-Host "  3) Check $ls\widget_host_boot.log"
Write-Host '  4) powershell -File c:\dev\tmp\diagnose-blank-now.ps1'
