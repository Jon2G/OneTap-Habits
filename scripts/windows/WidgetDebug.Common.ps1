# Shared helpers for OneTap Habits Windows widget debug scripts.
# Dot-source only: . "$PSScriptRoot\WidgetDebug.Common.ps1"

$ErrorActionPreference = 'Stop'

$script:WidgetDebugRepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$script:WidgetDebugProjectPath = Join-Path $script:WidgetDebugRepoRoot 'src\OneTapHabits.csproj'
$script:WidgetDebugArtifactsDir = Join-Path $script:WidgetDebugRepoRoot 'artifacts'
$script:WidgetDebugMsixPath = Join-Path $script:WidgetDebugArtifactsDir 'OneTapHabits-debug.msix'
$script:WidgetDebugSecretsDir = [System.IO.Path]::GetFullPath((Join-Path $script:WidgetDebugRepoRoot '..\..\.cursor\projects\OneTap-Habits\secrets'))

function Get-WidgetDebugSecretsDir {
    if (Test-Path $script:WidgetDebugSecretsDir) {
        return $script:WidgetDebugSecretsDir
    }

    return $null
}

function Get-WidgetDebugWebClientSecret {
    param([string]$ExplicitSecret)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitSecret)) {
        return $ExplicitSecret.Trim()
    }

    if (-not [string]::IsNullOrWhiteSpace($env:GOOGLE_OAUTH_CLIENT_SECRET)) {
        Write-Host 'Using GOOGLE_OAUTH_CLIENT_SECRET from environment.'
        return $env:GOOGLE_OAUTH_CLIENT_SECRET.Trim()
    }

    $secretsDir = Get-WidgetDebugSecretsDir
    if (-not $secretsDir) {
        return $null
    }

    $clientSecretFile = Get-ChildItem -Path $secretsDir -Filter 'client_secret_*.json' -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $clientSecretFile) {
        return $null
    }

    $json = Get-Content $clientSecretFile.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    $secret = $null
    if ($json.web -and $json.web.client_secret) {
        $secret = [string]$json.web.client_secret
    }
    elseif ($json.installed -and $json.installed.client_secret) {
        $secret = [string]$json.installed.client_secret
    }
    elseif ($json.client_secret) {
        $secret = [string]$json.client_secret
    }

    if ([string]::IsNullOrWhiteSpace($secret)) {
        Write-Host "Found $($clientSecretFile.Name) but no client_secret field." -ForegroundColor Yellow
        return $null
    }

    Write-Host "Using web client secret from secrets\$($clientSecretFile.Name)"
    return $secret.Trim()
}

function Sync-WidgetDebugSecretFiles {
    $secretsDir = Get-WidgetDebugSecretsDir
    if (-not $secretsDir) {
        Write-Host "Secrets dir not found: $($script:WidgetDebugSecretsDir)" -ForegroundColor Yellow
        return
    }

    Write-Section 'Sync secrets into src'
    Write-Host "Secrets: $secretsDir"
    $srcDir = Join-Path $script:WidgetDebugRepoRoot 'src'

    $gsSecrets = Join-Path $secretsDir 'google-services.json'
    $gsSrc = Join-Path $srcDir 'google-services.json'
    if (Test-Path $gsSecrets) {
        Copy-Item $gsSecrets $gsSrc -Force
        Write-Host 'Synced google-services.json'
    }

    foreach ($name in @('windows-signing.pfx', 'windows-signing.cer')) {
        $from = Join-Path $secretsDir $name
        $to = Join-Path $srcDir $name
        if (Test-Path $from) {
            Copy-Item $from $to -Force
            Write-Host "Synced $name"
        }
    }
}

function Get-OneTapPackage {
    Get-AppxPackage -Name '*OneTap*' -ErrorAction SilentlyContinue | Select-Object -First 1
}

function Get-LocalStatePath {
    $package = Get-OneTapPackage
    if (-not $package) {
        throw 'OneTap Habits is not installed. Run install-widget-debug.ps1 first.'
    }

    Join-Path $env:LOCALAPPDATA "Packages\$($package.PackageFamilyName)\LocalState"
}

function Write-Section([string]$Title) {
    Write-Host ''
    Write-Host "== $Title ==" -ForegroundColor Cyan
}

function Ensure-DebugSigningCertificate {
    $srcDir = Join-Path $script:WidgetDebugRepoRoot 'src'
    $pfxPath = Join-Path $srcDir 'windows-signing.pfx'
    $cerPath = Join-Path $srcDir 'windows-signing.cer'
    $secure = ConvertTo-SecureString -String 'OneTapHabitsDev' -Force -AsPlainText

    if (-not (Test-Path $pfxPath) -or -not (Test-Path $cerPath)) {
        Write-Host 'Creating local debug signing certificate...'
        & (Join-Path $PSScriptRoot 'create-signing-cert.ps1') -OutputDir $srcDir | Out-Null
    }

    # Always sign with the cert that matches windows-signing.cer (not an older CN=Jon2G leftover).
    $cer = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $cerPath
    $thumbprint = $cer.Thumbprint
    Write-Host "Using signing thumbprint: $thumbprint"

    $inMy = Get-ChildItem Cert:\CurrentUser\My -ErrorAction SilentlyContinue |
        Where-Object { $_.Thumbprint -eq $thumbprint } |
        Select-Object -First 1
    if (-not $inMy) {
        Write-Host 'Importing debug PFX into personal certificate store...'
        Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation Cert:\CurrentUser\My -Password $secure | Out-Null
    }

    foreach ($storePath in @('Cert:\CurrentUser\TrustedPeople', 'Cert:\CurrentUser\Root')) {
        $trusted = Get-ChildItem $storePath -ErrorAction SilentlyContinue |
            Where-Object { $_.Thumbprint -eq $thumbprint } |
            Select-Object -First 1
        if (-not $trusted) {
            Write-Host "Trusting debug certificate in $storePath..."
            Import-Certificate -FilePath $cerPath -CertStoreLocation $storePath | Out-Null
        }
    }

    return $thumbprint
}

function Get-WidgetDebugVersion {
    # csproj still says 1.3.4; releases override via CI. Local debug should track latest tag + 1.
    $latest = & git -C $script:WidgetDebugRepoRoot tag -l 'v1.3.*' 2>$null |
        ForEach-Object { $_.TrimStart('v') } |
        Where-Object { $_ -match '^\d+\.\d+\.\d+$' } |
        Sort-Object { [version]$_ } |
        Select-Object -Last 1

    if (-not $latest) {
        return @{ Display = '1.3.17'; Code = '10317' }
    }

    $parts = $latest.Split('.')
    $nextPatch = [int]$parts[2] + 1
    $display = "$($parts[0]).$($parts[1]).$nextPatch"
    $code = ([int]$parts[0] * 10000 + [int]$parts[1] * 100 + $nextPatch).ToString()
    return @{ Display = $display; Code = $code }
}

function Prepare-WidgetDebugFirebaseConfig {
    param([string]$WebClientSecret)

    Write-Section 'Prepare firebase-config.json'
    Sync-WidgetDebugSecretFiles

    $resolvedSecret = Get-WidgetDebugWebClientSecret -ExplicitSecret $WebClientSecret
    $prepare = Join-Path $PSScriptRoot 'prepare-firebase-config.ps1'
    $srcDir = Join-Path $script:WidgetDebugRepoRoot 'src'
    $prepareArgs = @{
        GoogleServicesPath = (Join-Path $srcDir 'google-services.json')
        OutputPath = (Join-Path $srcDir 'firebase-config.json')
        ExamplePath = (Join-Path $srcDir 'firebase-config.json.example')
    }
    if (-not [string]::IsNullOrWhiteSpace($resolvedSecret)) {
        $prepareArgs['WebClientSecret'] = $resolvedSecret
    }
    else {
        Write-Host 'No OAuth client secret found (secrets/client_secret_*.json, -WebClientSecret, or GOOGLE_OAUTH_CLIENT_SECRET). Google Sign-In will stay disabled.' -ForegroundColor Yellow
    }

    & $prepare @prepareArgs
    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "prepare-firebase-config.ps1 failed with exit code $LASTEXITCODE"
    }

    $cfgPath = $prepareArgs['OutputPath']
    $cfg = Get-Content $cfgPath -Raw | ConvertFrom-Json
    if ($cfg.apiKey -like 'YOUR_*' -or $cfg.webClientId -like 'YOUR_*') {
        throw 'firebase-config.json still has placeholders. Ensure secrets/google-services.json (or src/google-services.json) exists and re-run.'
    }

    $hasSecret = -not [string]::IsNullOrWhiteSpace([string]$cfg.webClientSecret)
    Write-Host "firebase-config ready (webClientSecret present: $hasSecret)"
    if (-not $hasSecret) {
        Write-Host 'Hint: add client_secret_*.json under .cursor/projects/OneTap-Habits/secrets' -ForegroundColor Yellow
    }
}

function Build-WidgetDebugMsix {
    param(
        [string]$WebClientSecret,
        [string]$DisplayVersion,
        [string]$VersionCode
    )

    Write-Section 'Build Windows MSIX'
    Push-Location $script:WidgetDebugRepoRoot
    try {
        if (Test-Path (Join-Path $script:WidgetDebugRepoRoot 'global.maui.ci.json')) {
            Copy-Item (Join-Path $script:WidgetDebugRepoRoot 'global.maui.ci.json') (Join-Path $script:WidgetDebugRepoRoot 'global.json') -Force
        }

        Prepare-WidgetDebugFirebaseConfig -WebClientSecret $WebClientSecret

        $ver = Get-WidgetDebugVersion
        if ([string]::IsNullOrWhiteSpace($DisplayVersion)) { $DisplayVersion = $ver.Display }
        if ([string]::IsNullOrWhiteSpace($VersionCode)) { $VersionCode = $ver.Code }
        Write-Host "Package version: $DisplayVersion ($VersionCode)"

        $thumbprint = Ensure-DebugSigningCertificate

        dotnet workload restore $script:WidgetDebugProjectPath -p:TargetFramework=net9.0-windows10.0.22000.0
        if ($LASTEXITCODE -ne 0) { throw "dotnet workload restore failed with exit code $LASTEXITCODE" }

        dotnet publish $script:WidgetDebugProjectPath `
            -f net9.0-windows10.0.22000.0 `
            -c Debug `
            -p:WindowsPackageType=MSIX `
            -p:AppxPackageSigningEnabled=true `
            -p:PackageCertificateThumbprint=$thumbprint `
            -p:ApplicationDisplayVersion=$DisplayVersion `
            -p:ApplicationVersion=$VersionCode
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

        New-Item -ItemType Directory -Force -Path $script:WidgetDebugArtifactsDir | Out-Null
        $msix = Get-ChildItem -Path (Join-Path $script:WidgetDebugRepoRoot 'src\bin\Debug\net9.0-windows10.0.22000.0') -Filter *.msix -Recurse |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        if (-not $msix) {
            throw 'MSIX not found after publish.'
        }

        Copy-Item $msix.FullName $script:WidgetDebugMsixPath -Force
        Write-Host "Built: $($script:WidgetDebugMsixPath)"
        return $script:WidgetDebugMsixPath
    }
    finally {
        Pop-Location
    }
}

function Ensure-LocalMachinePublisherTrust {
    param([Parameter(Mandatory = $true)][string]$CerPath)

    $cer = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $CerPath
    $thumb = $cer.Thumbprint
    $needsTrust = $false
    foreach ($storePath in @('Cert:\LocalMachine\TrustedPeople', 'Cert:\LocalMachine\Root')) {
        $trusted = Get-ChildItem $storePath -ErrorAction SilentlyContinue |
            Where-Object { $_.Thumbprint -eq $thumb } |
            Select-Object -First 1
        if (-not $trusted) { $needsTrust = $true }
    }

    if (-not $needsTrust) {
        Write-Host "Publisher $thumb already trusted in LocalMachine stores."
        return
    }

    Write-Host "Publisher $thumb needs LocalMachine trust - requesting elevation (approve UAC)..."
    $elevScript = Join-Path $PSScriptRoot 'trust-msix-publisher-elevated.ps1'
    $logPath = Join-Path $env:TEMP "onetap-trust-msix-$thumb.log"
    Remove-Item $logPath -Force -ErrorAction SilentlyContinue
    $p = Start-Process -FilePath powershell.exe -Verb RunAs -Wait -PassThru -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $elevScript,
        '-CerPath', $CerPath,
        '-LogPath', $logPath
    )
    if ($p.ExitCode -ne 0) {
        throw "Elevated certificate trust failed (exit $($p.ExitCode)). Trust $CerPath into LocalMachine\TrustedPeople and Root, then retry."
    }
    if (Test-Path $logPath) {
        Get-Content $logPath | ForEach-Object { Write-Host $_ }
    }
}

function Install-WidgetDebugMsix {
    param(
        [string]$MsixPath = $script:WidgetDebugMsixPath
    )

    Write-Section 'Install MSIX'
    if (-not (Test-Path $MsixPath)) {
        throw "MSIX not found at $MsixPath. Run install-widget-debug.ps1 (or build first)."
    }

    $cerPath = Join-Path $script:WidgetDebugRepoRoot 'src\windows-signing.cer'
    if (Test-Path $cerPath) {
        $expectedThumb = (New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $cerPath).Thumbprint
        foreach ($storePath in @('Cert:\CurrentUser\TrustedPeople', 'Cert:\CurrentUser\Root')) {
            $trusted = Get-ChildItem $storePath -ErrorAction SilentlyContinue |
                Where-Object { $_.Thumbprint -eq $expectedThumb } |
                Select-Object -First 1
            if (-not $trusted) {
                Write-Host "Trusting debug publisher certificate ($expectedThumb) in $storePath..."
                Import-Certificate -FilePath $cerPath -CertStoreLocation $storePath | Out-Null
            }
        }

        Ensure-LocalMachinePublisherTrust -CerPath $cerPath

        $sig = Get-AuthenticodeSignature $MsixPath
        if ($sig.SignerCertificate -and $sig.SignerCertificate.Thumbprint -ne $expectedThumb) {
            throw "MSIX signer thumbprint $($sig.SignerCertificate.Thumbprint) does not match trusted cert $expectedThumb. Rebuild first."
        }
        if ($sig.Status -eq 'NotSigned') {
            throw 'MSIX is not signed. Rebuild first.'
        }
        Write-Host "Signature status: $($sig.Status); signer: $($sig.SignerCertificate.Thumbprint)"
    }

    $existing = Get-OneTapPackage
    if ($existing) {
        Write-Host "Removing existing package $($existing.PackageFullName)"
        Remove-AppxPackage -Package $existing.PackageFullName
    }

    Add-AppxPackage -Path $MsixPath
    Write-Host 'Installed. Unpin/repin the widget after install.'
}

function Enable-WidgetDebugLogging {
    Write-Section 'Enable widget debug logging'
    $localState = Get-LocalStatePath
    New-Item -ItemType Directory -Force -Path $localState | Out-Null
    $flag = Join-Path $localState 'widget_debug.enable'
    Set-Content -Path $flag -Value (Get-Date).ToString('O')
    Write-Host "Created $flag"
    Write-Host 'Unpin and re-pin the widget (or reopen Widget Board) to restart WidgetHost with logging.'
}

function Disable-WidgetDebugLogging {
    Write-Section 'Disable widget debug logging'
    $localState = Get-LocalStatePath
    $flag = Join-Path $localState 'widget_debug.enable'
    if (Test-Path $flag) {
        Remove-Item $flag -Force
    }
    Write-Host 'Debug flag removed.'
}

function Show-WidgetDebugState {
    Write-Section 'Widget debug state'
    $package = Get-OneTapPackage
    if (-not $package) {
        Write-Host 'Package: not installed'
        return $false
    }

    Write-Host "Package: $($package.Name)"
    Write-Host "Family:  $($package.PackageFamilyName)"
    Write-Host "Version: $($package.Version)"
    Write-Host "Location: $($package.InstallLocation)"

    $localState = Get-LocalStatePath
    Write-Host "LocalState: $localState"

    $files = @(
        'widget_debug.log',
        'widget_debug.enable',
        'widget_snapshot.json',
        'widget_last_template.json',
        'widget_last_data.json',
        'widget_refresh.signal'
    )
    foreach ($file in $files) {
        $path = Join-Path $localState $file
        if (Test-Path $path) {
            $item = Get-Item $path
            Write-Host "[found] $file ($($item.Length) bytes, $($item.LastWriteTime))"
        }
        else {
            Write-Host "[missing] $file"
        }
    }

    $ok = $true
    $widgetHost = Join-Path $package.InstallLocation 'WidgetHost\OneTapHabits.WidgetHost.exe'
    if (Test-Path $widgetHost) {
        Write-Host "WidgetHost: $widgetHost"
    }
    else {
        Write-Host 'WidgetHost: MISSING from install location' -ForegroundColor Red
        $ok = $false
    }

    $publicAssets = Join-Path $package.InstallLocation 'Public\ProviderAssets'
    if (Test-Path $publicAssets) {
        Write-Host 'Public assets:'
        Get-ChildItem $publicAssets | ForEach-Object { Write-Host "  - $($_.Name) ($($_.Length) bytes)" }
    }
    else {
        Write-Host 'Public assets: MISSING (widget icons will not render)' -ForegroundColor Red
        $ok = $false
    }

    $hostProc = Get-Process -Name 'OneTapHabits.WidgetHost' -ErrorAction SilentlyContinue
    if ($hostProc) {
        Write-Host "WidgetHost process: running (PID $($hostProc.Id))"
    }
    else {
        Write-Host 'WidgetHost process: not running (pin the widget to start it)'
    }

    return $ok
}

function Show-WidgetTestChecklist {
    Write-Section 'Manual test checklist'
    Write-Host '1. Open OneTap Habits -> Today (writes widget_snapshot.json).'
    Write-Host '2. Win+W -> unpin old widget -> pin "Today''s habits" again.'
    Write-Host '3. Confirm icon in picker, habit grid visible, tap +/- works.'
    Write-Host '4. Re-run: .\test-widget-debug.ps1'
    Write-Host '5. If blank: .\test-widget-debug.ps1 -TailLog'
}

function Start-WidgetDebugLogTail {
    Write-Section 'Tail widget debug log'
    $localState = Get-LocalStatePath
    $logPath = Join-Path $localState 'widget_debug.log'
    if (-not (Test-Path $logPath)) {
        Write-Host "Log not found yet: $logPath"
        Write-Host 'Enable debug (install script does this), then unpin/repin the widget.'
        return
    }

    Write-Host "Tailing $logPath (Ctrl+C to stop)"
    Get-Content $logPath -Wait
}

