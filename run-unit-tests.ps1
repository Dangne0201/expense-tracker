<# Run unit tests (fast, no DB required)
Usage examples:
  PowerShell -ExecutionPolicy Bypass -File .\run-unit-tests.ps1
  .\run-unit-tests.ps1 -Configuration Release
#>
param(
    [string]$Configuration = "Debug"
)

Write-Host "Running unit tests (Configuration=$Configuration)"

# Ensure build first
dotnet build "src/ExpenseTracker.sln" -c $Configuration || throw "Build failed"

# Run tests (only unit tests if test project contains both, you can filter later)
dotnet test "src/ExpenseTracker.Tests" -c $Configuration --no-build --verbosity minimal
