param(
    [string]$saPassword
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
. (Join-Path $PSScriptRoot "credential-protection.ps1")
if ([string]::IsNullOrWhiteSpace($saPassword)) {
    $saPassword = [Environment]::GetEnvironmentVariable("SA_PASSWORD", "Process")
}
if ([string]::IsNullOrWhiteSpace($saPassword)) {
    throw "Set SA_PASSWORD or run setup-all.ps1, which securely generates and stores the local admin credential."
}
$sqlPortValue = [Environment]::GetEnvironmentVariable("EXPENSE_TRACKER_SQL_PORT", "Process")
if ([string]::IsNullOrWhiteSpace($sqlPortValue)) {
    $sqlPortValue = "1433"
}
$sqlPort = 0
if (-not [int]::TryParse($sqlPortValue, [ref]$sqlPort) -or $sqlPort -lt 1 -or $sqlPort -gt 65535) {
    throw "EXPENSE_TRACKER_SQL_PORT must be a valid TCP port between 1 and 65535."
}

$docker = Get-Command docker -ErrorAction SilentlyContinue
if (-not $docker) {
    throw "Docker CLI not found. Install Docker Desktop and ensure docker is on PATH."
}

$originalSaPassword = [Environment]::GetEnvironmentVariable("SA_PASSWORD", "Process")
$originalSqlcmdPassword = [Environment]::GetEnvironmentVariable("SQLCMDPASSWORD", "Process")
$env:SA_PASSWORD = $saPassword
$env:SQLCMDPASSWORD = $saPassword
try {
& $docker.Source compose --project-directory $repoRoot up -d
if ($LASTEXITCODE -ne 0) {
    throw "docker compose up failed."
}

$ready = $false
$sqlcmd = $null
for ($attempt = 0; $attempt -lt 60; $attempt++) {
    foreach ($candidate in @("/opt/mssql-tools/bin/sqlcmd", "/opt/mssql-tools18/bin/sqlcmd")) {
        try {
            & $docker.Source compose --project-directory $repoRoot exec -T mssql test -x $candidate 2>$null
            if ($LASTEXITCODE -eq 0) {
                $sqlcmd = $candidate
                break
            }
        }
        catch {
            $sqlcmd = $null
        }
    }

    if ($sqlcmd) {
        try {
            & $docker.Source compose --project-directory $repoRoot exec -T `
                -e SQLCMDPASSWORD mssql $sqlcmd -S localhost -U sa -Q "SELECT 1" -C -b 2>$null
            if ($LASTEXITCODE -eq 0) {
                $ready = $true
                break
            }
        }
        catch {
            $ready = $false
        }
    }
    Start-Sleep -Seconds 2
}
if (-not $ready) {
    throw "SQL Server did not become ready. Check 'docker compose logs mssql'; existing volumes are left untouched."
}

& $docker.Source compose --project-directory $repoRoot exec -T `
    -e SQLCMDPASSWORD mssql $sqlcmd -S localhost -U sa `
    -i /init/init.sql -C -b
if ($LASTEXITCODE -ne 0) {
    throw "Database initialization or migration failed. Existing volume data was not deleted; review the SQL error before retrying."
}
Write-Host "Database schema and safe migrations applied from data/init.sql; existing rows were preserved."

$credentialFile = Get-ExpenseCredentialFile
$saveAppCredential = $false
if (Test-Path $credentialFile) {
    try {
        $appPassword = Read-ExpenseProtectedSecret $credentialFile
    }
    catch {
        Write-Warning "Could not decrypt the saved ExpenseApp credential; replacing it after successful SQL Server admin authentication."
        $appPassword = New-ExpensePassword
        $saveAppCredential = $true
    }
}
else {
    $appPassword = New-ExpensePassword
    $saveAppCredential = $true
}

$provisionSql = @"
IF SUSER_ID(N'ExpenseApp') IS NULL
    CREATE LOGIN [ExpenseApp] WITH PASSWORD = N'$appPassword', CHECK_POLICY = ON;
ELSE
    ALTER LOGIN [ExpenseApp] WITH PASSWORD = N'$appPassword', CHECK_POLICY = ON;
GO
USE [ExpenseDb];
IF USER_ID(N'ExpenseApp') IS NULL
    CREATE USER [ExpenseApp] FOR LOGIN [ExpenseApp];
ELSE
    ALTER USER [ExpenseApp] WITH LOGIN = [ExpenseApp];
GO
IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members drm
    JOIN sys.database_principals rolePrincipal ON rolePrincipal.principal_id = drm.role_principal_id
    JOIN sys.database_principals memberPrincipal ON memberPrincipal.principal_id = drm.member_principal_id
    WHERE rolePrincipal.name = N'db_datareader' AND memberPrincipal.name = N'ExpenseApp')
    ALTER ROLE [db_datareader] ADD MEMBER [ExpenseApp];
IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members drm
    JOIN sys.database_principals rolePrincipal ON rolePrincipal.principal_id = drm.role_principal_id
    JOIN sys.database_principals memberPrincipal ON memberPrincipal.principal_id = drm.member_principal_id
    WHERE rolePrincipal.name = N'db_datawriter' AND memberPrincipal.name = N'ExpenseApp')
    ALTER ROLE [db_datawriter] ADD MEMBER [ExpenseApp];
GO
"@
$provisionSql | & $docker.Source compose --project-directory $repoRoot exec -T `
    -e SQLCMDPASSWORD mssql $sqlcmd -S localhost -U sa -C -b
if ($LASTEXITCODE -ne 0) {
    throw "Could not configure the restricted ExpenseApp database login."
}
if ($saveAppCredential) {
    Save-ExpenseProtectedSecret $credentialFile $appPassword
}

$connectionBuilder = New-Object System.Data.Common.DbConnectionStringBuilder
$connectionBuilder["Data Source"] = "127.0.0.1,$sqlPort"
$connectionBuilder["Initial Catalog"] = "ExpenseDb"
$connectionBuilder["User ID"] = "ExpenseApp"
$connectionBuilder["Password"] = $appPassword
$connectionBuilder["Encrypt"] = $false
$connectionBuilder["TrustServerCertificate"] = $true
$env:SQL_CONN = $connectionBuilder.ConnectionString
Write-Host "Configured the ExpenseApp login with database reader/writer permissions."
}
finally {
    if ([string]::IsNullOrEmpty($originalSaPassword)) {
        Remove-Item Env:SA_PASSWORD -ErrorAction SilentlyContinue
    }
    else {
        $env:SA_PASSWORD = $originalSaPassword
    }
    if ([string]::IsNullOrEmpty($originalSqlcmdPassword)) {
        Remove-Item Env:SQLCMDPASSWORD -ErrorAction SilentlyContinue
    }
    else {
        $env:SQLCMDPASSWORD = $originalSqlcmdPassword
    }
}
