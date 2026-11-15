#Requires -Version 5.1
<#
.SYNOPSIS
  Check widget debug state after install (package contents, LocalState files, process).

.EXAMPLE
  .\test-widget-debug.ps1

.EXAMPLE
  .\test-widget-debug.ps1 -TailLog
#>
param(
    [switch]$TailLog,
    [switch]$DisableDebug
)

. "$PSScriptRoot\WidgetDebug.Common.ps1"

if ($DisableDebug) {
    Disable-WidgetDebugLogging
}

$ok = Show-WidgetDebugState
Show-WidgetTestChecklist

if ($TailLog) {
    Start-WidgetDebugLogTail
    exit 0
}

if (-not $ok) {
    Write-Host ''
    Write-Host 'FAIL: WidgetHost or Public assets missing from install. Re-run .\install-widget-debug.ps1' -ForegroundColor Red
    exit 1
}

$package = Get-OneTapPackage
$localState = Get-LocalStatePath
$snapshot = Join-Path $localState 'widget_snapshot.json'
$hostProc = Get-Process -Name 'OneTapHabits.WidgetHost' -ErrorAction SilentlyContinue

Write-Section 'Result'
if ((Test-Path $snapshot) -and $hostProc) {
    Write-Host 'PASS-ish: snapshot exists and WidgetHost is running. Confirm the widget UI looks correct.' -ForegroundColor Green
}
elseif (Test-Path $snapshot) {
    Write-Host 'PARTIAL: snapshot exists, but WidgetHost is not running yet. Pin/repin the widget (Win+W).' -ForegroundColor Yellow
}
else {
    Write-Host 'WAITING: open the app to Today, then pin/repin the widget, then re-run this script.' -ForegroundColor Yellow
}
