#Requires -Version 5.1
param(
    [Parameter(Mandatory = $true)]
    [string]$CerPath,
    [Parameter(Mandatory = $true)]
    [string]$LogPath
)

$ErrorActionPreference = 'Stop'
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $CerPath
$thumb = $cert.Thumbprint

foreach ($storePath in @('Cert:\LocalMachine\TrustedPeople', 'Cert:\LocalMachine\Root')) {
    $existing = Get-ChildItem $storePath -ErrorAction SilentlyContinue |
        Where-Object { $_.Thumbprint -eq $thumb } |
        Select-Object -First 1
    if (-not $existing) {
        Import-Certificate -FilePath $CerPath -CertStoreLocation $storePath | Out-Null
        "Imported $thumb into $storePath" | Tee-Object -FilePath $LogPath -Append
    }
    else {
        "Already $thumb in $storePath" | Tee-Object -FilePath $LogPath -Append
    }
}

