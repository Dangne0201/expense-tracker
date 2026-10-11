<# Run unit tests (fast, no DB required)
Usage examples:
  PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\run-unit-tests.ps1
  .\scripts\tests\run-unit-tests.ps1 -Configuration Release
#>
# Builds the solution and runs database-independent unit tests.
param(
    [string]$Configuration = "Debug"
)

Write-Host "Running unit tests (Configuration=$Configuration)"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Build the solution before invoking the test runner.
& dotnet build (Join-Path $repoRoot 'ExpenseTracker.sln') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# Exclude integration tests so this path does not require SQL Server.
& dotnet test (Join-Path $repoRoot 'src\ExpenseTracker.Tests') -c $Configuration --no-build --filter "Category!=Integration" --verbosity minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
