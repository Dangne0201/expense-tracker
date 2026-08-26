param(
    [string]$saPassword = "Your_password123"
)

function Write-Log { param($m) Write-Output "[fix-sqlconn] $m" }

# Build connection string using provided SA password (do not echo password in logs)
$sqlConn = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=$saPassword;Encrypt=False;TrustServerCertificate=True;"

# Export for current process so child processes inherit it
$env:SQL_CONN = $sqlConn
Write-Log "SQL_CONN set for current process."

# Create a batch wrapper that includes the password so GUI started from Explorer can read it#$bat will live next to setup scripts$bat = Join-Path $PSScriptRoot "start-expense-app.bat"$batContent = @"
@echo off
set "SQL_CONN=$sqlConn"
@REM Launch using start so this batch returns immediately
start "" "$(Split-Path -Path $PSScriptRoot -Parent)\..\src\ExpenseTracker.WinForms\bin\Debug\net10.0-windows\ExpenseTracker.WinForms.exe"
"@
# Write ASCII to keep batch file simplenSet-Content -Path $bat -Value $batContent -Encoding ASCII
Write-Log "Batch wrapper created at $bat (contains connection string)."

# Done
Write-Log "fix-sqlconn complete."