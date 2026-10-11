param(
    [string]$saPassword,
    [string]$Configuration = "Debug",
    [ValidateRange(1, 65535)]
    [int]$Port = 1433
)

# Prepares or validates a local test database, builds the solution, and runs integration-tagged tests.
$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
$setupScript = Join-Path $repoRoot "scripts\setup\setup-all.ps1"
$originalTestPort = [Environment]::GetEnvironmentVariable("EXPENSE_TEST_SQL_PORT", "Process")

try {
    # Use the default setup flow on port 1433; custom ports must point at a disposable local database.
    if ($Port -eq 1433) {
        Write-Host "Starting the local Docker database and running integration tests."
        & $setupScript -saPassword $saPassword -RunApp:$false
    }
    elseif ([string]::IsNullOrWhiteSpace($env:SQL_CONN)) {
        throw "For a non-default test port, set SQL_CONN to a disposable local ExpenseDb before running this script."
    }

    $env:EXPENSE_TEST_SQL_PORT = $Port.ToString([Globalization.CultureInfo]::InvariantCulture)

    # Build once, then run only tests marked Integration.
    & dotnet build (Join-Path $repoRoot "ExpenseTracker.sln") --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Solution build failed."
    }
    & dotnet test (Join-Path $repoRoot "src\ExpenseTracker.Tests") `
        --configuration $Configuration `
        --no-build `
        --filter "Category=Integration" `
        --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Integration tests failed."
    }

    Write-Host "Integration tests passed. The Docker volume remains in place."
}
finally {
    # Restore the test port setting so it does not leak into later commands.
    if ([string]::IsNullOrEmpty($originalTestPort)) {
        Remove-Item Env:EXPENSE_TEST_SQL_PORT -ErrorAction SilentlyContinue
    }
    else {
        $env:EXPENSE_TEST_SQL_PORT = $originalTestPort
    }
}
