param(
    [string]$Version = "0.2.0",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
$project = Join-Path $repoRoot "src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj"
$artifactRoot = Join-Path $repoRoot "artifacts"
$stagingRoot = Join-Path $artifactRoot ".release-staging"
$publishRoot = Join-Path $stagingRoot "app"
$archive = Join-Path $artifactRoot "expense-tracker-v$Version-$Runtime.zip"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet CLI was not found. Install the .NET SDK before creating a release."
}

if (Test-Path $stagingRoot) {
    Remove-Item $stagingRoot -Recurse -Force
}
New-Item $publishRoot -ItemType Directory -Force | Out-Null

Write-Host "Publishing self-contained WinForms app for $Runtime..."
dotnet publish $project `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishRoot `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

Copy-Item (Join-Path $repoRoot "docker-compose.yml") $stagingRoot
New-Item (Join-Path $stagingRoot "data") -ItemType Directory -Force | Out-Null
Copy-Item (Join-Path $repoRoot "data\init.sql") (Join-Path $stagingRoot "data\init.sql")
Copy-Item (Join-Path $repoRoot "scripts\setup\start-dev-fixed.ps1") (Join-Path $stagingRoot "start-dev-fixed.ps1")

@"
param(
    [string]`$saPassword = "Your_password123"
)

`$ErrorActionPreference = "Stop"
`$root = `$PSScriptRoot
Set-Location `$root
`$env:SA_PASSWORD = `$saPassword

Write-Host "Starting SQL Server and initializing ExpenseDb..."
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path `$root "start-dev-fixed.ps1") -saPassword `$saPassword
if (`$LASTEXITCODE -ne 0) {
    throw "Database startup failed. Check Docker Desktop and try again."
}

`$env:SQL_CONN = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=`$saPassword;Encrypt=False;TrustServerCertificate=True;"
Start-Process (Join-Path `$root "app\ExpenseTracker.WinForms.exe") -WorkingDirectory (Join-Path `$root "app")
"@ | Set-Content (Join-Path $stagingRoot "Run-ExpenseTracker.ps1") -Encoding ASCII

@"
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run-ExpenseTracker.ps1" %*
if errorlevel 1 pause
"@ | Set-Content (Join-Path $stagingRoot "Run-ExpenseTracker.bat") -Encoding ASCII

@"
Expense Tracker review bundle

1. Install and start Docker Desktop.
2. Double-click Run-ExpenseTracker.bat.
3. The first run downloads SQL Server (about 1 GB), creates ExpenseDb, and starts the app.
4. Later runs reuse the Docker volume and start faster.

The app is self-contained and does not require the .NET SDK.
To reset the sample database, run: docker compose down -v
"@ | Set-Content (Join-Path $stagingRoot "README.txt") -Encoding ASCII

if (Test-Path $archive) {
    Remove-Item $archive -Force
}
Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $archive -CompressionLevel Optimal
Remove-Item $stagingRoot -Recurse -Force

Write-Host "Created $archive"
