# Creates a self-signed certificate for MSIX package signing.
# Publisher CN must match src/Platforms/Windows/Package.appxmanifest (CN=Jon2G).
# See: https://learn.microsoft.com/en-us/windows/msix/package/create-certificate-package-signing
param(
    [string]$OutputDir = (Join-Path $PSScriptRoot '..\..\src'),
    [string]$Password = 'OneTapHabitsDev'
)

$ErrorActionPreference = 'Stop'
$OutputDir = (Resolve-Path $OutputDir).Path
$pfxPath = Join-Path $OutputDir 'windows-signing.pfx'
$cerPath = Join-Path $OutputDir 'windows-signing.cer'
$secure = ConvertTo-SecureString -String $Password -Force -AsPlainText

$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject 'CN=Jon2G' `
    -KeyUsage DigitalSignature `
    -FriendlyName 'OneTap Habits MSIX' `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $secure | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

# Verify the PFX round-trips (same check CI runs before publish).
$verify = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($pfxPath, $Password, 'Exportable')
if ($verify.Thumbprint -ne $cert.Thumbprint) {
    throw 'PFX verification failed after export.'
}

Write-Host "Created:"
Write-Host "  PFX: $pfxPath"
Write-Host "  CER: $cerPath"
Write-Host "  Thumbprint: $($cert.Thumbprint)"
Write-Host ""
Write-Host "GitHub secret WINDOWS_SIGNING_PFX_BASE64:"
[Convert]::ToBase64String([IO.File]::ReadAllBytes($pfxPath))
Write-Host ""
Write-Host "GitHub secret WINDOWS_SIGNING_PASSWORD: $Password"
