#Requires -Version 5.1
<#
.SYNOPSIS
  Build + install debug MSIX, enable logging, then run the widget test check — all in one.

.EXAMPLE
  .\widget-debug-all.ps1
#>
param(
    [switch]$SkipBuild,
    [switch]$TailLog,
    [string]$WebClientSecret
)

$ErrorActionPreference = 'Stop'

Write-Host 'OneTap Habits — widget debug all-in-one' -ForegroundColor Cyan
Write-Host '1) install  2) test'

& "$PSScriptRoot\install-widget-debug.ps1" -SkipBuild:$SkipBuild -WebClientSecret $WebClientSecret
if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) {
    throw "install-widget-debug.ps1 failed with exit code $LASTEXITCODE"
}

Write-Host ''
Write-Host 'Open OneTap Habits -> Today, then Win+W unpin/repin the widget.' -ForegroundColor Yellow
Write-Host 'Press Enter when ready to continue the test check...' -ForegroundColor Yellow
$null = Read-Host

& "$PSScriptRoot\test-widget-debug.ps1" -TailLog:$TailLog
exit $LASTEXITCODE

