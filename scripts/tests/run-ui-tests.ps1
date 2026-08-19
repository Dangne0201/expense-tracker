<# Run UI automation tests (uses the ExpenseTracker.UiTests project which depends on FlaUI)
Notes:
 - UI tests need an interactive desktop session (they interact with Windows UI).
 - This script builds the WinForms app, then runs the UI test project.
Usage:
  PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\run-ui-tests.ps1 -Configuration Debug
#>
param(
    [string]$Configuration = "Debug"
)

Write-Host "Building WinForms app and running UI tests (Configuration=$Configuration)"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Build the WinForms app in Debug (so the exe is available where tests expect it)
dotnet build (Join-Path $repoRoot 'src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj') -c $Configuration || throw "WinForms build failed"

# Build and run UI tests
dotnet test (Join-Path $repoRoot 'src\ExpenseTracker.UiTests') -c $Configuration --no-build --verbosity minimal
