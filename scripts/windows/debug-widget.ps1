#Requires -Version 5.1
<#
.SYNOPSIS
  Compatibility wrapper. Prefer the dedicated scripts:

    .\install-widget-debug.ps1   # build + install + enable debug
    .\test-widget-debug.ps1      # check state / tail log
    .\widget-debug-all.ps1       # install then test (interactive)
#>
param(
    [switch]$Build,
    [switch]$Install,
    [switch]$EnableDebug,
    [switch]$DisableDebug,
    [switch]$TailLog,
    [switch]$ShowState,
    [string]$MsixPath
)

. "$PSScriptRoot\WidgetDebug.Common.ps1"

$didSomething = $false

if ($Build) {
    Build-WidgetDebugMsix | Out-Null
    $didSomething = $true
}

if ($Install) {
    $path = if ($MsixPath) { $MsixPath } else { $script:WidgetDebugMsixPath }
    Install-WidgetDebugMsix -MsixPath $path
    $didSomething = $true
}

if ($EnableDebug) {
    Enable-WidgetDebugLogging
    $didSomething = $true
}

if ($DisableDebug) {
    Disable-WidgetDebugLogging
    $didSomething = $true
}

if ($ShowState) {
    Show-WidgetDebugState | Out-Null
    $didSomething = $true
}

if ($TailLog) {
    Start-WidgetDebugLogTail
    $didSomething = $true
}

if (-not $didSomething) {
    Write-Host 'Prefer these scripts:'
    Write-Host '  .\install-widget-debug.ps1   # build + install debug MSIX'
    Write-Host '  .\test-widget-debug.ps1      # check widget state'
    Write-Host '  .\widget-debug-all.ps1       # do both (interactive)'
    Write-Host ''
    Write-Host 'Legacy flags still work, e.g.:'
    Write-Host '  .\debug-widget.ps1 -Build -Install -EnableDebug -ShowState'
}

