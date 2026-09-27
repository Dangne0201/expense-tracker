<# Run UI automation tests (uses the ExpenseTracker.UiTests project which depends on FlaUI)
Notes:
 - UI tests need an interactive desktop session (they interact with Windows UI).
 - The default mode starts/preserves the local Docker database; a non-default port requires a disposable local SQL_CONN.
Usage:
  PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\run-ui-tests.ps1 -Configuration Release -Port 11433
#>
param(
    [string]$Configuration = "Debug",
    [string]$saPassword,
    [ValidateRange(1, 65535)]
    [int]$Port = 1433,
    [string]$Filter
)

$ErrorActionPreference = "Stop"
Write-Host "Building WinForms app and running UI tests (Configuration=$Configuration)"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
$previousConfiguration = $env:EXPENSE_TRACKER_CONFIGURATION
$previousConnection = [Environment]::GetEnvironmentVariable("SQL_CONN", "Process")
try {
    if ($Port -eq 1433) {
        $setupScript = Join-Path $repoRoot "scripts\setup\setup-all.ps1"
        & $setupScript -saPassword $saPassword -RunApp:$false
    }
    else {
        if ([string]::IsNullOrWhiteSpace($env:SQL_CONN)) {
            throw "For a non-default test port, set SQL_CONN to a disposable local ExpenseDb before running this script."
        }

        try {
            $connection = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($env:SQL_CONN)
        }
        catch {
            throw "SQL_CONN is not a valid SQL Server connection string."
        }

        $endpoint = ([string]$connection.DataSource -replace '^tcp:', '').Split(',', 2)
        $connectionPort = 1433
        if ($endpoint.Length -eq 2 -and
            -not [int]::TryParse($endpoint[1], [ref]$connectionPort)) {
            throw "SQL_CONN must use a numeric TCP port."
        }
        $localHost = $endpoint[0] -in @("localhost", "127.0.0.1", "(local)")
        if (-not $localHost -or
            -not [string]::Equals($connection.InitialCatalog, "ExpenseDb", [StringComparison]::OrdinalIgnoreCase) -or
            $connectionPort -ne $Port -or
            $Port -eq 1433) {
            throw "UI tests on a custom instance are restricted to disposable ExpenseDb on localhost at the specified non-default port."
        }
    }

    & dotnet build (Join-Path $repoRoot 'src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj') --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "WinForms build failed."
    }

    $env:EXPENSE_TRACKER_CONFIGURATION = $Configuration
    $testArguments = @(
        "test",
        (Join-Path $repoRoot 'src\ExpenseTracker.UiTests'),
        "--configuration", $Configuration,
        "--verbosity", "minimal"
    )
    if (-not [string]::IsNullOrWhiteSpace($Filter)) {
        $testArguments += @("--filter", $Filter)
    }

    & dotnet @testArguments
    if ($LASTEXITCODE -ne 0) {
        throw "UI tests failed with exit code $LASTEXITCODE."
    }
}
finally {
    if ($null -eq $previousConfiguration) {
        Remove-Item Env:EXPENSE_TRACKER_CONFIGURATION -ErrorAction SilentlyContinue
    }
    else {
        $env:EXPENSE_TRACKER_CONFIGURATION = $previousConfiguration
    }

    if ($null -eq $previousConnection) {
        Remove-Item Env:SQL_CONN -ErrorAction SilentlyContinue
    }
    else {
        $env:SQL_CONN = $previousConnection
    }
}
