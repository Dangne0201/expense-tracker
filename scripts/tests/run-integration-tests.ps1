<# Run integration tests that require SQL Server
This script will start the dev DB (via setup-all.ps1) then run integration tests.
Usage:
  PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\run-integration-tests.ps1 -saPassword 'Your_password123' -Configuration Debug
#>
param(
    [Parameter(Mandatory=$true)] [string]$saPassword,
    [string]$Configuration = "Debug"
)

Write-Host "Starting dev DB and running integration tests (Configuration=$Configuration)"

# Start DB and init (do not launch GUI)
$setupScript = Join-Path $PSScriptRoot '..\setup\setup-all.ps1'
& powershell -NoProfile -ExecutionPolicy Bypass -File $setupScript -saPassword $saPassword -RunApp:$false
if ($LASTEXITCODE -ne 0) { throw "setup-all failed" }

# Give DB some time to be ready (setup-all has waits, but ensure)
Start-Sleep -Seconds 5

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Build & run integration tests
& dotnet build (Join-Path $repoRoot 'ExpenseTracker.sln') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# Run tests (if tests are categorized you can filter by Trait/Category)
# Here run all tests in the test project; integration tests should be stable and use rollback/transaction
& dotnet test (Join-Path $repoRoot 'src\ExpenseTracker.Tests') -c $Configuration --no-build --verbosity minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Integration tests finished. Consider stopping containers if you started them for this run."