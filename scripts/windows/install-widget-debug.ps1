#Requires -Version 5.1
<#
.SYNOPSIS
  Build, sign, and install the local OneTap Habits debug MSIX (with widget packaging + debug logging).

.DESCRIPTION
  Reads secrets from .cursor/projects/OneTap-Habits/secrets by default:
  - client_secret_*.json  → Google OAuth web client secret
  - google-services.json  → synced into src for firebase-config
  - windows-signing.pfx/.cer → optional local signing material

.EXAMPLE
  .\install-widget-debug.ps1

.EXAMPLE
  .\install-widget-debug.ps1 -SkipBuild
#>
param(
    [switch]$SkipBuild,
    [string]$MsixPath,
    [string]$WebClientSecret,
    [string]$DisplayVersion,
    [string]$VersionCode
)

. "$PSScriptRoot\WidgetDebug.Common.ps1"

if (-not $SkipBuild) {
    Build-WidgetDebugMsix -WebClientSecret $WebClientSecret -DisplayVersion $DisplayVersion -VersionCode $VersionCode | Out-Null
}

$path = if ($MsixPath) { $MsixPath } else { $script:WidgetDebugMsixPath }

$verify = Join-Path $PSScriptRoot 'verify-widget-package.ps1'
if (Test-Path $verify) {
    Write-Section 'Verify widget package contents'
    & $verify -MsixPath $path
}

Install-WidgetDebugMsix -MsixPath $path
Enable-WidgetDebugLogging
Show-WidgetDebugState | Out-Null

Write-Host ''
Write-Host 'Install complete. Next: open the app, then run .\test-widget-debug.ps1' -ForegroundColor Green
