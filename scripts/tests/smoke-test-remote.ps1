param(
    [string]$saPassword,
    [bool]$RunApp = $false
)

# Runs the local setup flow followed by integration tests as a lightweight environment smoke check.
$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker CLI not found. Install Docker Desktop and ensure docker is on PATH."
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK 10 is required."
}

$setupScript = Join-Path $repoRoot "scripts\setup\setup-all.ps1"
# Prepare the local SQL Server and app connection before building integration tests.
& $setupScript -saPassword $saPassword -RunApp:$RunApp

# Exercise the database integration path while preserving the Docker volume.
& dotnet build (Join-Path $repoRoot "src\ExpenseTracker.Tests") --configuration Debug
if ($LASTEXITCODE -ne 0) {
    throw "Test project build failed."
}
& dotnet test (Join-Path $repoRoot "src\ExpenseTracker.Tests") `
    --configuration Debug `
    --no-build `
    --filter "Category=Integration" `
    --logger "console;verbosity=normal"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Local smoke test passed. The Docker volume remains in place."
