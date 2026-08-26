param(
    [string]$saPassword = "Your_password123",
    [Parameter()] [object]$RunApp = $true
)

# Normalize RunApp so the script accepts calling styles like:
#  .\scripts\setup\setup-all.ps1 -RunApp:$false
#  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup-all.ps1 -saPassword 'Your_password123' -RunApp:$false
$RunAppBool = $true
if ($RunApp -is [System.Management.Automation.SwitchParameter]) {
    $RunAppBool = [bool]$RunApp
} elseif ($RunApp -is [string]) {
    $s = $RunApp.ToString().Trim().ToLower()
    if ($s -in @('true','1','yes','$true')) { $RunAppBool = $true } else { $RunAppBool = $false }
} else {
    try { $RunAppBool = [bool]$RunApp } catch { $RunAppBool = $false }
}

function Write-Log { param($m) Write-Output "[setup-all-docker] $m" }

Write-Log "Starting Docker-based setup (DB container + init, optional build & run app)"

# Call start-dev-fixed.ps1 (should be in same folder)
$startScript = Join-Path $PSScriptRoot 'start-dev-fixed.ps1'
if (-not (Test-Path $startScript)) { Write-Log "Cannot find $startScript"; exit 1 }

# Run start-dev-fixed to ensure DB container is up and init.sql applied
& $startScript -saPassword $saPassword
if (-not $?) { Write-Log "start-dev-fixed.ps1 failed; aborting"; exit 1 }

# Wait for SQL Server to accept TCP connections on localhost:1433 (max ~2 minutes)
Write-Log "Waiting for SQL Server TCP port to become available..."
$dbReady = $false
$maxAttempts = 60
for ($i = 0; $i -lt $maxAttempts; $i++) {
    try {
        $connStr = "Server=localhost,1433;User Id=sa;Password=$saPassword;Connection Timeout=2"
        $cn = New-Object System.Data.SqlClient.SqlConnection $connStr
        $cn.Open()
        $cn.Close()
        $dbReady = $true
        break
    } catch {
        Start-Sleep -Seconds 2
    }
}
if (-not $dbReady) {
    Write-Log "Timed out waiting for SQL Server to accept connections. Check 'docker logs expense-mssql' for details. Aborting launch.";
    exit 1
}
Write-Log "SQL Server is accepting connections. (TCP)"

# Set SQL_CONN so launched app inherits correct connection string
$env:SQL_CONN = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=$saPassword;Encrypt=False;TrustServerCertificate=True;"
# Persist for the current user so GUI apps launched after this script can read it as well
# Set SQL_CONN so launched app inherits correct connection string (process-level only)
$sqlConn = $env:SQL_CONN
$env:SQL_CONN = $sqlConn
Write-Log "Set SQL_CONN for this process (masked in logs)."

# Test whether current SQL_CONN can connect to ExpenseDb; if not, attempt fallback using provided SA password
$canConnect = $false
try {
    $testCn = New-Object System.Data.SqlClient.SqlConnection $sqlConn
    $testCn.Open()
    $cmd = $testCn.CreateCommand()
    $cmd.CommandText = "SET NOCOUNT ON; IF DB_ID('ExpenseDb') IS NOT NULL SELECT 1 ELSE SELECT 0"
    $res = $cmd.ExecuteScalar()
    $testCn.Close()
    if ($res -eq 1) { $canConnect = $true }
} catch {
    Write-Log "Validation of masked SQL_CONN failed: $_"
}

if (-not $canConnect) {
    Write-Log "Masked SQL_CONN cannot connect. Attempting fallback with provided SA password."
    # Build real connection string with password
    $realConn = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=$saPassword;Encrypt=False;TrustServerCertificate=True;"
    # Try to create ExpenseDb if missing
    $repoRootCandidates2 = @($PSScriptRoot, (Split-Path -Parent $PSScriptRoot), (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
    $repoRoot2 = $repoRootCandidates2 | Where-Object { $_ -and (Test-Path (Join-Path $_ 'data\init.sql')) } | Select-Object -First 1
    if ($repoRoot2) {
        $initPathHost = Join-Path $repoRoot2 'data\init.sql'
        $hostSqlcmdObj = Get-Command sqlcmd -ErrorAction SilentlyContinue
        if ($hostSqlcmdObj) {
            try { & $hostSqlcmdObj.Source '-S' 'localhost,1433' '-U' 'SA' '-P' $saPassword '-i' $initPathHost; Write-Log "Ran init.sql via host sqlcmd." } catch { Write-Log "Host sqlcmd failed: $_" }
        } else {
            try { & docker cp $initPathHost ("expense-mssql:/init.sql"); & docker exec -i expense-mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P $saPassword -i /init.sql; Write-Log "Ran init.sql inside container." } catch { Write-Log "Container sqlcmd failed: $_" }
        }
    } else {
        Write-Log "init.sql not found; cannot auto-create database."
    }

    # Determine exeDir to write local sqlconn.txt
    $repoRootCandidatesExe = @($PSScriptRoot, (Split-Path -Parent $PSScriptRoot), (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
    $repoRootExe = $repoRootCandidatesExe | Where-Object { $_ -and (Test-Path (Join-Path $_ 'src\\ExpenseTracker.WinForms\\ExpenseTracker.WinForms.csproj')) } | Select-Object -First 1
    $exeDir = Join-Path $repoRootExe 'src\\ExpenseTracker.WinForms\\bin\\Debug\\net10.0-windows'
    $exePath = Join-Path $exeDir 'ExpenseTracker.WinForms.exe'
    try {
        $bat = Join-Path $PSScriptRoot "start-expense-app.bat"
        $batContent = @"
@echo off
set "SQL_CONN=$realConn"
start "" "$exePath"
"@
        Set-Content -Path $bat -Value $batContent -Encoding ASCII
        Write-Log "Generated batch wrapper at $bat (contains real connection string)."
    } catch {
        Write-Log "Failed to create batch wrapper: $_"
    }
    try {
        if (Test-Path $exeDir) { $localConnFile = Join-Path $exeDir 'sqlconn.txt'; Set-Content -Path $localConnFile -Value $realConn -Encoding ASCII; Write-Log "Wrote local sqlconn.txt at $localConnFile" } else { Write-Log "Exe directory not found; skipping creation of local sqlconn.txt" }
    } catch { Write-Log "Failed to write local sqlconn.txt: $_" }

    # Update env for this process
    $env:SQL_CONN = $realConn
    $sqlConn = $realConn
    Write-Log "Fallback SQL_CONN applied for this process."
} else {
    Write-Log "Existing masked SQL_CONN appears usable; continuing with existing behavior."
# Optionally build and run the WinForms app (will inherit SQL_CONN)
# Resolve project path robustly depending on where this script is located (repo root vs scripts/setup)
$repoRootCandidates = @(
    $PSScriptRoot,
    (Split-Path -Parent $PSScriptRoot),
    (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
)
$repoRoot = $repoRootCandidates | Where-Object { $_ -and (Test-Path (Join-Path $_ 'src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj')) } | Select-Object -First 1
$projCandidates = @(
    "$repoRoot\src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj",
    "$PSScriptRoot\..\..\src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj"
)
$proj = $projCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $proj) {
    Write-Log "WinForms project file not found at expected locations; skipping build/run. Searched: $($projCandidates -join ', ')"
} elseif (Get-Command dotnet -ErrorAction SilentlyContinue) {
    Write-Log "Building WinForms project..."
    $projPath = (Resolve-Path $proj).ProviderPath
    dotnet build $projPath
    if ($LASTEXITCODE -ne 0) { Write-Log "dotnet build failed. Skipping run."; return }
    Write-Log "Build succeeded."
    $exe = Join-Path (Split-Path $projPath -Parent) 'bin\Debug\net10.0-windows\ExpenseTracker.WinForms.exe'
    if (Test-Path $exe) {
        if ($RunAppBool) {
            Write-Log "Starting application exe: $exe"
            # Ensure sqlConn includes provided password
            $sqlConn = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=$saPassword;Encrypt=False;TrustServerCertificate=True;"
            $env:SQL_CONN = $sqlConn

            $bat = Join-Path $PSScriptRoot "start-expense-app.bat"
            # Create batch wrapper that sets SQL_CONN including TrustServerCertificate to avoid cert trust errors
            $batContent = @"
@echo off
set "SQL_CONN=Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=$saPassword;Encrypt=False;TrustServerCertificate=True;"
start "" "$exe"
"@
            Set-Content -Path $bat -Value $batContent -Encoding ASCII
            Start-Process -FilePath $bat -WorkingDirectory (Split-Path $exe -Parent)
            Write-Log "Application started."
        } else {
            Write-Log "RunApp not specified; skipping launching the application."
        }
    } else {
        Write-Log "Built exe not found at expected path: $exe. You can run 'dotnet run --project src/ExpenseTracker.WinForms/ExpenseTracker.WinForms.csproj'"
    }
} else {
    Write-Log "dotnet CLI not available; skipping build/run. You can start the app manually; ensure SQL_CONN is set in environment."
}
}

Write-Log "Docker-based setup complete."

