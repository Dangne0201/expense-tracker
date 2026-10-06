param(
    [string]$saPassword,
    [bool]$RunApp = $true
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
. (Join-Path $PSScriptRoot "credential-protection.ps1")
$originalSaPassword = [Environment]::GetEnvironmentVariable("SA_PASSWORD", "Process")

function Set-AppConnectionFromProtectedCredential {
    $credentialFile = Get-ExpenseCredentialFile
    if (-not (Test-Path $credentialFile)) {
        throw "The existing SQL Server has no saved ExpenseApp credential. Do not remove its volume; provide its SA password with -saPassword to repair setup."
    }

    try {
        $appPassword = Read-ExpenseProtectedSecret $credentialFile
    }
    catch {
        throw "Could not decrypt the saved ExpenseApp credential for this Windows user. Preserve the database volume and credential file; use the original Windows profile or provide the existing SA password with -saPassword."
    }

    $portValue = [Environment]::GetEnvironmentVariable("EXPENSE_TRACKER_SQL_PORT", "Process")
    if ([string]::IsNullOrWhiteSpace($portValue)) {
        $portValue = "1433"
    }
    $port = 0
    if (-not [int]::TryParse($portValue, [ref]$port) -or $port -lt 1 -or $port -gt 65535) {
        throw "EXPENSE_TRACKER_SQL_PORT must be a valid TCP port between 1 and 65535."
    }

    $sqlReady = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        $client = New-Object System.Net.Sockets.TcpClient
        try {
            $connection = $client.BeginConnect("127.0.0.1", $port, $null, $null)
            if ($connection.AsyncWaitHandle.WaitOne(1000)) {
                $client.EndConnect($connection)
                $sqlReady = $true
                break
            }
        }
        catch [System.Net.Sockets.SocketException] {
        }
        finally {
            $client.Close()
        }
        Start-Sleep -Seconds 1
    }
    if (-not $sqlReady) {
        throw "SQL Server did not become reachable at 127.0.0.1:$port. The existing database volume was left untouched."
    }

    $connectionBuilder = New-Object System.Data.Common.DbConnectionStringBuilder
    $connectionBuilder["Data Source"] = "127.0.0.1,$port"
    $connectionBuilder["Initial Catalog"] = "ExpenseDb"
    $connectionBuilder["User ID"] = "ExpenseApp"
    $connectionBuilder["Password"] = $appPassword
    $connectionBuilder["Encrypt"] = $false
    $connectionBuilder["TrustServerCertificate"] = $true
    $env:SQL_CONN = $connectionBuilder.ConnectionString
}

function Build-And-LaunchApp {
    if ([string]::IsNullOrWhiteSpace($env:SQL_CONN)) {
        throw "SQL Server setup did not produce an app connection."
    }

    if (-not $RunApp) {
        return
    }

    Remove-Item Env:SA_PASSWORD -ErrorAction SilentlyContinue
    $project = Join-Path $repoRoot "src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj"
    & dotnet build $project
    if ($LASTEXITCODE -ne 0) {
        throw "WinForms build failed."
    }

    $exe = Join-Path $repoRoot "src\ExpenseTracker.WinForms\bin\Debug\net10.0-windows\ExpenseTracker.WinForms.exe"
    if (-not (Test-Path $exe)) {
        throw "Built app executable was not found at $exe."
    }
    Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe -Parent)
}

function Get-ConfiguredContainerSaPassword([string]$Name) {
    $environment = & $docker.Source inspect --format '{{range .Config.Env}}{{println .}}{{end}}' $Name 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Could not read the existing SQL Server container configuration."
    }

    $saLine = $environment | Where-Object {
        $_.StartsWith("SA_PASSWORD=", [StringComparison]::Ordinal)
    } | Select-Object -First 1
    if ($null -eq $saLine) {
        return $null
    }

    $password = $saLine.Substring("SA_PASSWORD=".Length)
    if ([string]::IsNullOrWhiteSpace($password)) {
        return $null
    }
    return $password
}

function Read-InteractiveSaPassword {
    if (-not [Environment]::UserInteractive -or [Console]::IsInputRedirected) {
        return $null
    }

    $securePassword = Read-Host "Enter the current SQL Server SA password (input hidden)" -AsSecureString
    if ($securePassword.Length -eq 0) {
        return $null
    }

    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }
}

try {
    if ([string]::IsNullOrWhiteSpace($saPassword)) {
        $saPassword = $originalSaPassword
    }

    $containerName = [Environment]::GetEnvironmentVariable("EXPENSE_TRACKER_CONTAINER_NAME", "Process")
    if ([string]::IsNullOrWhiteSpace($containerName)) {
        $containerName = "expense-mssql"
    }

    $docker = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $docker) {
        throw "Docker CLI not found. Install Docker Desktop and ensure docker is on PATH."
    }

    $containerExists = $false
    $containerRunning = $false
    $containers = & $docker.Source container ls --all --format "{{.Names}}" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Could not list Docker containers: $($containers -join ' ')"
    }
    $containerExists = @($containers) -contains $containerName
    if ($containerExists) {
        $runningState = & $docker.Source inspect --format '{{.State.Running}}' $containerName
        if ($LASTEXITCODE -ne 0) {
            throw "Could not inspect the existing SQL Server container '$containerName'."
        }
        $containerRunning = $runningState -eq "true"
    }

    $adminCredentialFile = Get-ExpenseSqlAdminCredentialFile
    $hasSavedAdminPassword = Test-Path $adminCredentialFile
    $hasSuppliedAdminPassword = -not [string]::IsNullOrWhiteSpace($saPassword)

    if (-not $containerExists -and -not $hasSavedAdminPassword -and -not $hasSuppliedAdminPassword) {
        $composeProjectName = [Environment]::GetEnvironmentVariable("COMPOSE_PROJECT_NAME", "Process")
        if ([string]::IsNullOrWhiteSpace($composeProjectName)) {
            $composeProjectName = [IO.Path]::GetFileName($repoRoot).ToLowerInvariant() -replace '[^a-z0-9_-]', ''
        }
        $volumeName = & $docker.Source volume ls `
            --filter "label=com.docker.compose.project=$composeProjectName" `
            --filter "label=com.docker.compose.volume=mssqldata" `
            --format '{{.Name}}'
        if ($LASTEXITCODE -ne 0) {
            throw "Could not check for an existing SQL Server volume. No credentials or database data were changed."
        }
        if (-not [string]::IsNullOrWhiteSpace(($volumeName -join ""))) {
            $saPassword = Read-InteractiveSaPassword
            $hasSuppliedAdminPassword = -not [string]::IsNullOrWhiteSpace($saPassword)
            if (-not $hasSuppliedAdminPassword) {
                throw "An existing SQL Server data volume was found without a saved local admin credential. Enter its current SA password when prompted, or rerun with -saPassword; setup will not guess a password or modify the volume."
            }
        }
    }

    if (-not $hasSuppliedAdminPassword -and $hasSavedAdminPassword) {
        try {
            $saPassword = Read-ExpenseProtectedSecret $adminCredentialFile
        }
        catch {
            if ($containerExists) {
                Write-Warning "Could not decrypt the saved admin credential; checking the existing container for its configured SA password."
                $saPassword = Get-ConfiguredContainerSaPassword $containerName
            }
            if ([string]::IsNullOrWhiteSpace($saPassword)) {
                $saPassword = Read-InteractiveSaPassword
            }
            if ([string]::IsNullOrWhiteSpace($saPassword)) {
                throw "Could not decrypt the saved local SQL Server admin credential. Preserve the database volume and credential file; use the original Windows profile or enter the current SA password."
            }
        }
        $hasSuppliedAdminPassword = -not [string]::IsNullOrWhiteSpace($saPassword)
    }

    if (-not $hasSuppliedAdminPassword -and $containerExists) {
        $appCredentialFile = Get-ExpenseCredentialFile
        $canReuseAppCredential = $false
        if (Test-Path $appCredentialFile) {
            try {
                $null = Read-ExpenseProtectedSecret $appCredentialFile
                $canReuseAppCredential = $true
            }
            catch {
                Write-Warning "Could not decrypt the saved ExpenseApp credential; attempting to repair it with SQL Server admin access."
            }
        }

        if (-not $canReuseAppCredential) {
            $saPassword = Get-ConfiguredContainerSaPassword $containerName
            if ([string]::IsNullOrWhiteSpace($saPassword)) {
                $saPassword = Read-InteractiveSaPassword
            }
            $hasSuppliedAdminPassword = -not [string]::IsNullOrWhiteSpace($saPassword)
            if (-not $hasSuppliedAdminPassword) {
                throw "The existing SQL Server has no usable saved ExpenseApp credential or recoverable SA password. Enter the current SA password when prompted, or rerun with -saPassword. The database volume was left untouched."
            }
        }
    }

    if (-not $hasSuppliedAdminPassword -and $containerExists) {
        if (-not $containerRunning) {
            & $docker.Source start $containerName
            if ($LASTEXITCODE -ne 0) {
                throw "Could not start the existing SQL Server container '$containerName'. The database volume was left untouched."
            }
        }
        Set-AppConnectionFromProtectedCredential
        Write-Host "Reusing the saved ExpenseApp login; no SQL Server admin password is needed for normal startup."
    }
    else {
        if (-not $hasSuppliedAdminPassword) {
            $saPassword = New-ExpensePassword
            if ($saPassword.Length -lt 12 -or
                $saPassword -notmatch '[A-Z]' -or
                $saPassword -notmatch '[a-z]' -or
                $saPassword -notmatch '[0-9]' -or
                $saPassword -notmatch '[^a-zA-Z0-9]') {
                throw "The generated SQL Server admin password must have at least 12 characters, including uppercase, lowercase, a number, and a symbol."
            }
        }

        $env:SA_PASSWORD = $saPassword
        $startScript = Join-Path $PSScriptRoot "start-dev-fixed.ps1"
        & $startScript -saPassword $saPassword
        if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
            throw "SQL Server setup failed."
        }

        if (-not $hasSavedAdminPassword -or $hasSuppliedAdminPassword) {
            Save-ExpenseProtectedSecret $adminCredentialFile $saPassword
        }
    }

    Build-And-LaunchApp
}
finally {
    if ([string]::IsNullOrEmpty($originalSaPassword)) {
        Remove-Item Env:SA_PASSWORD -ErrorAction SilentlyContinue
    }
    else {
        $env:SA_PASSWORD = $originalSaPassword
    }
}
