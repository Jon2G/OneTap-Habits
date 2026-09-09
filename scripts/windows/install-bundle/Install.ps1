# OneTap Habits — Windows sideload installer (release bundle).
# Expects OneTapHabits-*.msix and OneTapHabits-windows-signing.cer in the same folder.
param(
    [string]$BundleRoot = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) {
    Write-Host "==> $Message"
}

$BundleRoot = (Resolve-Path $BundleRoot).Path

$msix = Get-ChildItem -Path $BundleRoot -Filter 'OneTapHabits-*.msix' |
    Sort-Object Name -Descending |
    Select-Object -First 1
$cer = Join-Path $BundleRoot 'OneTapHabits-windows-signing.cer'

if (-not $msix) {
    throw "No OneTapHabits-*.msix found in $BundleRoot. Extract the full ZIP before installing."
}
if (-not (Test-Path $cer)) {
    throw "Certificate not found: $cer"
}

Write-Step "Installing publisher certificate (Current User → Trusted People)"
Import-Certificate -FilePath $cer -CertStoreLocation 'Cert:\CurrentUser\TrustedPeople' | Out-Null
Import-Certificate -FilePath $cer -CertStoreLocation 'Cert:\CurrentUser\Root' | Out-Null

$sig = Get-AuthenticodeSignature $msix.FullName
if ($null -eq $sig.SignerCertificate -or $sig.Status -eq 'NotSigned') {
    throw "MSIX is not signed ($($sig.Status)). Download a signed build from GitHub Releases."
}

Write-Step "Installing $($msix.Name)"
Add-AppxPackage -Path $msix.FullName -ForceUpdateFromAnyVersion

Write-Host ""
Write-Host "OneTap Habits installed. Open it from the Start menu."
Write-Host "If Windows App Runtime is missing, install it from:"
Write-Host "https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads"
