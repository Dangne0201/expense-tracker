<# Run integration tests that require SQL Server
This script will start the dev DB (via setup-all.ps1) then run integration tests.
Usage:
  PowerShell -ExecutionPolicy Bypass -File .\run-integration-tests.ps1 -saPassword 'Your_password123' -Configuration Debug
#>
param(
    [Parameter(Mandatory=$true)] [string]$saPassword,
    [string]$Configuration = "Debug"
)

Write-Host "Starting dev DB and running integration tests (Configuration=$Configuration)"

# Start DB and init (do not launch GUI)
PowerShell -Command "& { .\setup-all.ps1 -saPassword '$saPassword' -RunApp:$false }" || throw "setup-all failed"

# Give DB some time to be ready (setup-all has waits, but ensure)
Start-Sleep -Seconds 5

# Build & run integration tests
dotnet build "src/ExpenseTracker.sln" -c $Configuration || throw "Build failed"

# Run tests (if tests are categorized you can filter by Trait/Category)
# Here run all tests in the test project; integration tests should be stable and use rollback/transaction
dotnet test "src/ExpenseTracker.Tests" -c $Configuration --no-build --verbosity minimal

Write-Host "Integration tests finished. Consider stopping containers if you started them for this run."