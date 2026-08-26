param(
    [string]$saPassword = "Your_password123"
)

function Write-Log { param($m) Write-Output "[fix-sqlconn] $m" }

# Old behavior: set masked SQL_CONN and create batch wrapper
$maskedConn = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=$saPassword;Encrypt=False;TrustServerCertificate=True;"
$env:SQL_CONN = $maskedConn
Write-Log "SQL_CONN (masked) set for current process."

$bat = Join-Path $PSScriptRoot "start-expense-app.bat"
# Resolve exe path
$repoRootCandidates = @(
    $PSScriptRoot,
    (Split-Path -Parent $PSScriptRoot),
    (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
)
$repoRoot = $repoRootCandidates | Where-Object { $_ -and (Test-Path (Join-Path $_ 'src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj')) } | Select-Object -First 1
$exeDir = Join-Path $repoRoot 'src\ExpenseTracker.WinForms\bin\Debug\net10.0-windows'
$exePath = Join-Path $exeDir 'ExpenseTracker.WinForms.exe'

$batContentMasked = @"
@echo off
set "SQL_CONN=$maskedConn"
start "" "$exePath"
"@

try {
    Set-Content -Path $bat -Value $batContentMasked -Encoding ASCII
    Write-Log "Masked batch wrapper created at $bat"
} catch { Write-Log "Failed to create masked batch: $_" }

# New behavior (fallback): try to use real SA password to create a usable local sqlconn.txt and batch
    $realConn = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=$saPassword;Encrypt=False;TrustServerCertificate=True;"
$canUseReal = $false
try {
    $cn = New-Object System.Data.SqlClient.SqlConnection $realConn
    $cn.Open()
    $cn.Close()
    $canUseReal = $true
    Write-Log "Successfully connected to SQL Server with provided SA password. Will write local sqlconn.txt and real batch."
} catch {
    Write-Log "Real connection test failed: $_"
}

if (-not $canUseReal) {
    # Attempt to run data/init.sql to create ExpenseDb, then retry
    $repoRootCandidates2 = $repoRootCandidates
    $repoRoot2 = $repoRootCandidates2 | Where-Object { $_ -and (Test-Path (Join-Path $_ 'data\init.sql')) } | Select-Object -First 1
    if ($repoRoot2) {
        $initPathHost = Join-Path $repoRoot2 'data\init.sql'
        $hostSqlcmdObj = Get-Command sqlcmd -ErrorAction SilentlyContinue
        if ($hostSqlcmdObj) {
            try { & $hostSqlcmdObj.Source '-S' 'localhost,1433' '-U' 'SA' '-P' $saPassword '-i' $initPathHost; Write-Log "Ran init.sql via host sqlcmd." } catch { Write-Log "Host sqlcmd failed: $_" }
        } else {
            try { & docker cp $initPathHost ("expense-mssql:/init.sql"); & docker exec -i expense-mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P $saPassword -i /init.sql; Write-Log "Ran init.sql inside container." } catch { Write-Log "Container sqlcmd failed: $_" }
        }
        # Retry connection
        try { $cn = New-Object System.Data.SqlClient.SqlConnection $realConn; $cn.Open(); $cn.Close(); $canUseReal = $true; Write-Log "Connection succeeded after running init.sql." } catch { Write-Log "Still cannot connect after init.sql: $_" }
    } else {
        Write-Log "init.sql not found; cannot auto-create DB to enable real connection."
    }
}

if ($canUseReal) {
    try {
        if (Test-Path $exeDir) {
            $localConnFile = Join-Path $exeDir 'sqlconn.txt'
            Set-Content -Path $localConnFile -Value $realConn -Encoding ASCII
            Write-Log "Wrote local connection file at $localConnFile"
        } else {
            Write-Log "Exe directory not found; skipping creation of local sqlconn.txt"
        }
    } catch { Write-Log "Failed to write local sqlconn.txt: $_" }

    try {
        $batContentReal = @"
@echo off
set "SQL_CONN=$realConn"
start "" "$exePath"
"@
        Set-Content -Path $bat -Value $batContentReal -Encoding ASCII
        Write-Log "Created real batch wrapper at $bat"
    } catch { Write-Log "Failed to create real batch wrapper: $_" }
} else {
    Write-Log "Could not create a working real connection; leaving masked batch in place."
}

Write-Log "fix-sqlconn complete."
