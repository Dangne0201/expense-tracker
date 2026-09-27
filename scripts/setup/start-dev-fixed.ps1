param(
    [string]$saPassword
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
if ([string]::IsNullOrWhiteSpace($saPassword)) {
    $saPassword = [Environment]::GetEnvironmentVariable("SA_PASSWORD", "Process")
}
if ([string]::IsNullOrWhiteSpace($saPassword)) {
    throw "Set SA_PASSWORD or run setup-all.ps1, which securely prompts for it."
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

if (-not ("ExpenseTracker.Security.DpapiUserScope" -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ExpenseTracker.Security
{
    public static class DpapiUserScope
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public int Length;
            public IntPtr Data;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CryptProtectData(
            ref DataBlob input,
            string description,
            IntPtr optionalEntropy,
            IntPtr reserved,
            IntPtr prompt,
            uint flags,
            out DataBlob output);

        [DllImport("crypt32.dll", SetLastError = true)]
        private static extern bool CryptUnprotectData(
            ref DataBlob input,
            IntPtr description,
            IntPtr optionalEntropy,
            IntPtr reserved,
            IntPtr prompt,
            uint flags,
            out DataBlob output);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr memory);

        public static byte[] Protect(byte[] input)
        {
            return Transform(input, true);
        }

        public static byte[] Unprotect(byte[] input)
        {
            return Transform(input, false);
        }

        private static byte[] Transform(byte[] input, bool protect)
        {
            var pinned = GCHandle.Alloc(input, GCHandleType.Pinned);
            var inputBlob = new DataBlob { Length = input.Length, Data = pinned.AddrOfPinnedObject() };
            DataBlob outputBlob;
            try
            {
                var succeeded = protect
                    ? CryptProtectData(ref inputBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out outputBlob)
                    : CryptUnprotectData(ref inputBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out outputBlob);
                if (!succeeded)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var output = new byte[outputBlob.Length];
                try
                {
                    Marshal.Copy(outputBlob.Data, output, 0, output.Length);
                }
                finally
                {
                    LocalFree(outputBlob.Data);
                }
                return output;
            }
            finally
            {
                pinned.Free();
            }
        }
    }
}
'@
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

$credentialFile = [Environment]::GetEnvironmentVariable("EXPENSE_TRACKER_CREDENTIAL_FILE", "Process")
if ([string]::IsNullOrWhiteSpace($credentialFile)) {
    $credentialFolder = Join-Path ([Environment]::GetFolderPath("LocalApplicationData")) "ExpenseTracker"
    $credentialFile = Join-Path $credentialFolder "app-db-password.bin"
}
else {
    $credentialFile = [IO.Path]::GetFullPath($credentialFile)
    $credentialFolder = [IO.Path]::GetDirectoryName($credentialFile)
    if ([string]::IsNullOrWhiteSpace($credentialFolder)) {
        throw "EXPENSE_TRACKER_CREDENTIAL_FILE must include a directory path."
    }
}
if (Test-Path $credentialFile) {
    try {
        $protectedPassword = [IO.File]::ReadAllBytes($credentialFile)
        $appPasswordBytes = [ExpenseTracker.Security.DpapiUserScope]::Unprotect($protectedPassword)
        $appPassword = [Text.Encoding]::UTF8.GetString($appPasswordBytes)
    }
    catch {
        throw "Could not read this Windows user's protected app credential. Preserve the database volume and credential file; investigate the local Windows profile before recovery."
    }
}
else {
    $randomBytes = New-Object byte[] 24
    $randomGenerator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $randomGenerator.GetBytes($randomBytes)
    }
    finally {
        $randomGenerator.Dispose()
    }
    $passwordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789"
    $appPassword = "Aa7" + (-join ($randomBytes | ForEach-Object {
        $passwordAlphabet[[int]$_ % $passwordAlphabet.Length]
    }))
    $protectedPassword = [ExpenseTracker.Security.DpapiUserScope]::Protect(
        [Text.Encoding]::UTF8.GetBytes($appPassword))
    New-Item $credentialFolder -ItemType Directory -Force | Out-Null
    [IO.File]::WriteAllBytes($credentialFile, $protectedPassword)
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
