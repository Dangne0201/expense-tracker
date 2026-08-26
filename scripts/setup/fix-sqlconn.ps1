param(
    [string]\$saPassword = "Your_password123"
)

function Write-Log { param($m) Write-Output "[fix-sqlconn] $m" }

# Build connection string using provided SA password (do not echo password in logs)
$sqlConn = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=\$saPassword;Encrypt=False;TrustServerCertificate=True;"

# Export for current process so child processes inherit it
$env:SQL_CONN = $sqlConn
Write-Log "SQL_CONN set for current process."

# Create a batch wrapper that includes the password so GUI started from Explorer can read it
$bat = Join-Path $PSScriptRoot "start-expense-app.bat"
# Resolve exe path relative to repo (match setup-all behavior)
$repoRootCandidates = @(
    $PSScriptRoot,
    (Split-Path -Parent $PSScriptRoot),
    (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
)
$repoRoot = $repoRootCandidates | Where-Object { $_ -and (Test-Path (Join-Path $_ 'src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj')) } | Select-Object -First 1
$exePath = Join-Path (Join-Path $repoRoot 'src\ExpenseTracker.WinForms\bin\Debug\net10.0-windows') 'ExpenseTracker.WinForms.exe'

$batContent = @"
@echo off
set "SQL_CONN=$sqlConn"
start "" "$exePath"
"@

Set-Content -Path $bat -Value $batContent -Encoding ASCII
Write-Log "Batch wrapper created at $bat (contains connection string)."

# Done
Write-Log "fix-sqlconn complete."





