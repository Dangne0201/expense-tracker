param(
    [string]$saPassword = "Your_password123",
    [switch]$RunApp = $true
)

function Check-CommandExists([string]$name) {
    $c = Get-Command $name -ErrorAction SilentlyContinue
    return $null -ne $c
}

Write-Output "== ExpenseTracker: Full setup script (fixed) =="

# Detect Docker CLI (allow explicit install path if not in PATH)
$dockerCmdObj = Get-Command docker -ErrorAction SilentlyContinue
if ($dockerCmdObj) { $dockerAvailable = $true } else { $candidate = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe'; $dockerAvailable = Test-Path $candidate }
if (-not $dockerAvailable) {
    Write-Error "Docker CLI not found. Please install Docker Desktop and ensure Docker is running. https://www.docker.com/products/docker-desktop"
    exit 1
}
# Note: docker-compose availability is handled by start-dev-fixed.ps1 which tries both 'docker compose' and docker-compose.exe.

if (-not (Check-CommandExists 'dotnet')) {
    Write-Warning "dotnet SDK not found. You can still run the published exe if available. To build from source, install .NET SDK: https://dotnet.microsoft.com/en-us/download"
}

# Run the db init (reuses start-dev-fixed.ps1)
Write-Output "Starting DB container and initializing schema..."
$scriptPath = Join-Path $PSScriptRoot 'start-dev-fixed.ps1'
if (-Not (Test-Path $scriptPath)) {
    # In case script is run from repo root, try relative location
    $scriptPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'scripts\start-dev-fixed.ps1'
}

if (-Not (Test-Path $scriptPath)) {
    Write-Error "Cannot find start-dev-fixed.ps1 (expected at scripts/start-dev-fixed.ps1)."
    exit 1
}

& $scriptPath -saPassword $saPassword
if (-not $?) {
    Write-Error "DB initialization failed. Check Docker logs and try again."
    exit 1
}

# Set environment variable for this process so children inherit it
$env:SQL_CONN = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=$saPassword;"
Write-Output "Set SQL_CONN environment variable for this session. (SQL_CONN not printed for security)"

# Build the WinForms project if dotnet present
$proj = Join-Path $PSScriptRoot '..\src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj'
$proj = (Resolve-Path $proj).ProviderPath
if (Check-CommandExists 'dotnet') {
    Write-Output "Building WinForms project..."
    dotnet build $proj
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet build failed. Fix build issues before running the app."
        exit 1
    }
    Write-Output "Build succeeded."
    $exe = Join-Path (Split-Path $proj -Parent) 'bin\Debug\net10.0-windows\ExpenseTracker.WinForms.exe'
    if (Test-Path $exe) {
        Write-Output "Found built exe: $exe"
        if ($RunApp) {
            Write-Output "Starting application..."
            Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe -Parent)
            Write-Output "Application started. Close it to end the session."
        } else {
            Write-Output "RunApp not specified; skipping launching the application."
        }
    } else {
        Write-Warning "Built exe not found at expected path: $exe. You can run 'dotnet run --project src/ExpenseTracker.WinForms/ExpenseTracker.WinForms.csproj'"
    }
} else {
    Write-Warning "dotnet CLI not available; skipping build. If you have a published exe, start it now."
}

Write-Output "Setup script completed successfully."




