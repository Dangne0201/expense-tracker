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

# Ensure docker volume ownership so mssql user can initialize DB (workaround for Windows/WSL permission issues)
Write-Log "Ensuring docker volume ownership for mssql user (expense_tracker_mssqldata)..."
try {
    & docker run --rm -v expense_tracker_mssqldata:/mnt busybox chown -R 10001:0 /mnt
    Write-Log "Adjusted volume ownership (if volume existed)."
} catch {
    Write-Log "Warning: could not adjust volume ownership: $_"
}

# Wait for SQL Server to accept TCP connections on localhost:1433 (max ~2 minutes)
Write-Log "Waiting for SQL Server TCP port to become available..."
$dbReady = $false
$maxAttempts = 60
for ($i = 0; $i -lt $maxAttempts; $i++) {
    try {
        $connStr = "Server=localhost,1433;User Id=sa;Password=`$saPassword;Connection Timeout=2"
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
# Create SQL_CONN and GUI batch wrapper using the provided SA password so new machines can launch the GUI successfully
$fixScript = Join-Path $PSScriptRoot 'fix-sqlconn.ps1'
if (Test-Path $fixScript) {
    & $fixScript -saPassword $saPassword
} else {
    Write-Log "fix-sqlconn.ps1 not found; skipping automatic SQL_CONN creation. You can run scripts/setup/fix-sqlconn.ps1 -saPassword '<pwd>' manually."
}

# Set SQL_CONN so launched app inherits correct connection string
$env:SQL_CONN = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=`$saPassword;Encrypt=False;TrustServerCertificate=True;"
# Persist for the current user so GUI apps launched after this script can read it as well
# Set SQL_CONN so launched app inherits correct connection string (process-level only)
$sqlConn = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=`$saPassword;Encrypt=False;TrustServerCertificate=True;"
$env:SQL_CONN = $sqlConn
Write-Log "Set SQL_CONN for this process. Will create a batch wrapper to launch the GUI with the same value (password not echoed in logs)"
Write-Log "Set SQL_CONN for this process; a batch wrapper will be created to launch the GUI with the same value (password not echoed)"

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
            $sqlConn = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=`$saPassword;Encrypt=False;TrustServerCertificate=True;"
            $env:SQL_CONN = $sqlConn

            $bat = Join-Path $PSScriptRoot "start-expense-app.bat"
            # Create batch wrapper that sets SQL_CONN including TrustServerCertificate to avoid cert trust errors
            $batContent = @"
@echo off
set "SQL_CONN=Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=`$saPassword;Encrypt=False;TrustServerCertificate=True;"
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

Write-Log "Docker-based setup complete."





