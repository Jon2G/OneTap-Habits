# Creates a self-signed code-signing certificate for MSIX sideloading.
# Publisher CN must match src/Platforms/Windows/Package.appxmanifest (CN=Jon2G).
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
    -Subject 'CN=Jon2G' `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -Type CodeSigningCert `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -Provider 'Microsoft Enhanced RSA and AES Cryptographic Provider'

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $secure | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

Write-Host "Created:"
Write-Host "  PFX: $pfxPath"
Write-Host "  CER: $cerPath"
Write-Host "  Thumbprint: $($cert.Thumbprint)"
Write-Host ""
Write-Host "GitHub secret WINDOWS_SIGNING_PFX_BASE64:"
[Convert]::ToBase64String([IO.File]::ReadAllBytes($pfxPath))
Write-Host ""
Write-Host "GitHub secret WINDOWS_SIGNING_PASSWORD: $Password"
