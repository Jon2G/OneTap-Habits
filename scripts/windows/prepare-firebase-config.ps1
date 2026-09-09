param(
    [string]$GoogleServicesPath = "src/google-services.json",
    [string]$OutputPath = "src/firebase-config.json",
    [string]$ExamplePath = "src/firebase-config.json.example"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $GoogleServicesPath)) {
    if (Test-Path $ExamplePath) {
        Copy-Item $ExamplePath $OutputPath -Force
        Write-Host "Using $ExamplePath (google-services.json not found)."
        exit 0
    }

    throw "Neither $GoogleServicesPath nor $ExamplePath exists."
}

$json = Get-Content $GoogleServicesPath -Raw -Encoding UTF8
$root = $json | ConvertFrom-Json

$projectId = $root.project_info.project_id
$apiKey = $root.client[0].api_key[0].current_key
$webClientId = $root.client[0].oauth_client |
    Where-Object { $_.client_type -eq 3 } |
    Select-Object -First 1 -ExpandProperty client_id

if ([string]::IsNullOrWhiteSpace($projectId) -or
    [string]::IsNullOrWhiteSpace($apiKey) -or
    [string]::IsNullOrWhiteSpace($webClientId)) {
    throw "google-services.json is missing project_id, api_key, or web oauth client (type 3)."
}

$config = [ordered]@{
    projectId = $projectId
    apiKey = $apiKey
    webClientId = $webClientId
}

$config | ConvertTo-Json | Set-Content $OutputPath -Encoding UTF8
Write-Host "Created $OutputPath from $GoogleServicesPath."
