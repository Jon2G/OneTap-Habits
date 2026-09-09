# Trust the OneTap Habits sideload certificate and install a signed MSIX.
param(
    [Parameter(Mandatory = $true)]
    [string]$MsixPath,
    [string]$CertificatePath = (Join-Path $PSScriptRoot '..\..\src\windows-signing.cer')
)

$ErrorActionPreference = 'Stop'
$MsixPath = (Resolve-Path $MsixPath).Path

if (-not (Test-Path $CertificatePath)) {
    throw "Certificate not found at $CertificatePath. Download OneTapHabits-windows-signing.cer from the release."
}

Import-Certificate -FilePath $CertificatePath -CertStoreLocation 'Cert:\CurrentUser\TrustedPeople' | Out-Null
Import-Certificate -FilePath $CertificatePath -CertStoreLocation 'Cert:\CurrentUser\Root' | Out-Null

$sig = Get-AuthenticodeSignature $MsixPath
if ($null -eq $sig.SignerCertificate -or $sig.Status -eq 'NotSigned') {
    throw "MSIX is not signed ($($sig.Status)). Use a signed build from GitHub Releases v1.3.1+."
}

Add-AppxPackage -Path $MsixPath -ForceUpdateFromAnyVersion
Write-Host "Installed $MsixPath"
