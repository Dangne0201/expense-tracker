<# Run unit tests (fast, no DB required)
Usage examples:
  PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\run-unit-tests.ps1
  .\scripts\tests\run-unit-tests.ps1 -Configuration Release
#>
param(
    [string]$Configuration = "Debug"
)

Write-Host "Running unit tests (Configuration=$Configuration)"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Ensure build first
dotnet build (Join-Path $repoRoot 'src\ExpenseTracker.sln') -c $Configuration || throw "Build failed"

# Run tests (only unit tests if test project contains both, you can filter later)
dotnet test (Join-Path $repoRoot 'src\ExpenseTracker.Tests') -c $Configuration --no-build --verbosity minimal
