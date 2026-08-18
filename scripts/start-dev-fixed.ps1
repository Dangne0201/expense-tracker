param(
    [string]$saPassword = "Your_password123"
)

Write-Output "Starting development SQL Server container (docker compose up -d) using best available docker command..."

# Determine docker command (PowerShell 5-compatible)
$dockerCmd = $null
$dockerCmdObj = Get-Command docker -ErrorAction SilentlyContinue
if ($dockerCmdObj) {
    $dockerCmd = $dockerCmdObj.Source
} else {
    $candidate = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe'
    if (Test-Path $candidate) { $dockerCmd = $candidate }
}
if (-not $dockerCmd) { Write-Error "Docker CLI not found. Please install Docker Desktop or add docker.exe to PATH."; exit 1 }

# Determine compose command (prefer 'docker compose' if supported)
$composeCmd = $null
try { & $dockerCmd 'compose' 'version' > $null 2>&1; $composeCmd = @($dockerCmd, 'compose') } catch {
    $composeExe = 'C:\Program Files\Docker\Docker\resources\bin\docker-compose.exe'
    if (Test-Path $composeExe) { $composeCmd = @($composeExe) } else { $composeCmd = @($dockerCmd, 'compose') }
}

Write-Output "Running: docker compose up -d"
if ($composeCmd -is [array] -and $composeCmd.Count -eq 1) {
    & $composeCmd[0] 'up' '-d' '--force-recreate'
} else {
    & $composeCmd[0] $composeCmd[1] 'up' '-d' '--force-recreate'
}

$container = "expense-mssql"
Write-Output "Waiting for SQL Server container '$container' to be ready..."
$max = 60; $i = 0

# prefer host sqlcmd if available (PowerShell 5-compatible)
$hostSqlcmd = $null
$hostSqlcmdObj = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($hostSqlcmdObj) { $hostSqlcmd = $hostSqlcmdObj.Source }

while ($i -lt $max) {
    if ($hostSqlcmd) {
        try {
            & $hostSqlcmd -S "localhost,1433" -U SA -P $saPassword -Q "SELECT 1" > $null 2>&1
            Write-Output "SQL Server is ready (host sqlcmd)."
            break
        } catch {
            Start-Sleep -Seconds 2
            $i++
            continue
        }
    } else {
        try {
            & $dockerCmd 'exec' $container '/opt/mssql-tools/bin/sqlcmd' '-S' 'localhost' '-U' 'SA' '-P' $saPassword '-Q' 'SELECT 1' > $null 2>&1
            Write-Output "SQL Server is ready (container sqlcmd)."
            break
        } catch {
            Start-Sleep -Seconds 2
            $i++
            continue
        }
    }
}

if ($i -ge $max) { Write-Error "SQL Server did not become ready in time (waited $($max*2) seconds)."; exit 1 }

Write-Output "Initializing database (checking current state)..."
$repoRoot = Split-Path -Parent $PSScriptRoot
$initPathHost = Join-Path $repoRoot 'data\init.sql'
try { $initPathHost = (Resolve-Path $initPathHost -ErrorAction Stop).Path } catch { Write-Error "Cannot resolve init.sql path: $initPathHost"; exit 1 }

# Helper to run a SQL command and return trimmed stdout
function Invoke-SqlQuery([string]$query) {
    if ($hostSqlcmd) {
        $out = & $hostSqlcmd '-S' 'localhost,1433' '-U' 'SA' '-P' $saPassword '-Q' $query 2>&1
    } else {
        $out = & $dockerCmd 'exec' $container '/opt/mssql-tools/bin/sqlcmd' '-S' 'localhost' '-U' 'SA' '-P' $saPassword '-Q' $query 2>&1
    }
    return ($out -join "`n").Trim()
}

# Check if database already exists
$checkDbQuery = "SET NOCOUNT ON; IF DB_ID('ExpenseDb') IS NOT NULL SELECT 1 ELSE SELECT 0"
$checkOut = Invoke-SqlQuery $checkDbQuery
if ($checkOut -match '1') {
    Write-Output "Database 'ExpenseDb' already exists on server; skipping init."
} else {
    # If mdf exists in repo data folder, attempt to attach rather than create to avoid file-exists errors
    $mdfHostPath = Join-Path $repoRoot 'data\ExpenseDb.mdf'
    if (Test-Path $mdfHostPath) {
        Write-Output "Detected existing data/ExpenseDb.mdf on host; attempting to attach it to SQL Server in container..."
        $attachQuery = "CREATE DATABASE ExpenseDb ON (FILENAME = '/var/opt/mssql/data/ExpenseDb.mdf') FOR ATTACH"
        $attachOut = Invoke-SqlQuery $attachQuery
        if ($attachOut -match 'Msg') {
            Write-Warning "Attach attempt returned messages: $attachOut"
            Write-Output "Falling back to running init.sql script."
            if ($hostSqlcmd) {
                Write-Output "Using host sqlcmd to run init.sql: $initPathHost"
                & $hostSqlcmd '-S' 'localhost,1433' '-U' 'SA' '-P' $saPassword '-i' $initPathHost
            } else {
                Write-Output "Copying init.sql into container and executing via container sqlcmd"
                & $dockerCmd 'cp' $initPathHost ("${container}:/init.sql")
                & $dockerCmd 'exec' '-i' $container '/opt/mssql-tools/bin/sqlcmd' '-S' 'localhost' '-U' 'SA' '-P' $saPassword '-i' '/init.sql'
            }
        } else {
            Write-Output "Attach succeeded or produced no error messages: $attachOut"
        }
    } else {
        # No mdf to attach; run init.sql as usual
        if ($hostSqlcmd) {
            Write-Output "Using host sqlcmd to run init.sql: $initPathHost"
            & $hostSqlcmd '-S' 'localhost,1433' '-U' 'SA' '-P' $saPassword '-i' $initPathHost
        } else {
            Write-Output "Copying init.sql into container and executing via container sqlcmd"
            & $dockerCmd 'cp' $initPathHost ("${container}:/init.sql")
            & $dockerCmd 'exec' '-i' $container '/opt/mssql-tools/bin/sqlcmd' '-S' 'localhost' '-U' 'SA' '-P' $saPassword '-i' '/init.sql'
        }
    }
}

Write-Output "Database initialization complete."
Write-Output "You can now run the WinForms app. If you want the app to use this SQL Server, set the environment variable SQL_CONN with a connection string, for example:"
Write-Output "  $env:SQL_CONN = 'Server=localhost,1433;Database=ExpenseDb;User Id=sa;******;'"
