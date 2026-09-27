param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = "0.3.9",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
$project = Join-Path $repoRoot "src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj"
$artifactRoot = Join-Path $repoRoot "artifacts"
$archive = Join-Path $artifactRoot "expense-tracker-v$Version-$Runtime.zip"
$stagingRoot = Join-Path ([IO.Path]::GetTempPath()) ("expense-release-" + [Guid]::NewGuid().ToString("N"))
$publishRoot = Join-Path $stagingRoot "app"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet CLI was not found. Install the .NET SDK before creating a release."
}
if (Test-Path $archive) {
    throw "Release archive already exists and will not be overwritten: $archive. Choose a new version."
}
$worktreeStatus = & git -C $repoRoot status --porcelain=v1 --untracked-files=all
if ($LASTEXITCODE -ne 0) {
    throw "Unable to verify the Git worktree before creating a release."
}
if ($worktreeStatus) {
    throw "Release bundles must be built from a clean, committed checkout. Commit or discard all tracked and untracked changes first."
}
New-Item $publishRoot -ItemType Directory -Force | Out-Null

try {
    Write-Host "Publishing self-contained WinForms app for $Runtime..."
    & dotnet publish $project `
        --configuration Release `
        --runtime $Runtime `
        --self-contained true `
        --output $publishRoot `
        -p:DebugType=None `
        -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed."
    }

    $sourceCommit = & git -C $repoRoot rev-parse HEAD
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to determine the source commit for the release bundle."
    }
    $buildInfo = @(
        "Version: $Version"
        "Runtime: $Runtime"
        "Source commit: $sourceCommit"
    )
    Set-Content -Path (Join-Path $stagingRoot "BUILD-INFO.txt") -Value $buildInfo -Encoding ASCII

    $bundleCompose = Get-Content (Join-Path $repoRoot "docker-compose.yml") -Raw
    $bundleCompose = $bundleCompose -replace '(?m)^\s*container_name:.*\r?\n', ''
    $bundleCompose = $bundleCompose.Replace(
        '127.0.0.1:1433:1433',
        '127.0.0.1:${EXPENSE_TRACKER_SQL_PORT:-1433}:1433')
    Set-Content -Path (Join-Path $stagingRoot "docker-compose.yml") -Value $bundleCompose -Encoding ASCII
    New-Item (Join-Path $stagingRoot "data") -ItemType Directory -Force | Out-Null
    Copy-Item (Join-Path $repoRoot "data\init.sql") (Join-Path $stagingRoot "data\init.sql")
    $bundleSetupDirectory = Join-Path $stagingRoot "scripts\setup"
    New-Item $bundleSetupDirectory -ItemType Directory -Force | Out-Null
    Copy-Item (Join-Path $repoRoot "scripts\setup\start-dev-fixed.ps1") (Join-Path $bundleSetupDirectory "start-dev-fixed.ps1")

    $bundleProjectName = "expense-tracker-v" + ($Version -replace '[^a-zA-Z0-9_-]', '-').ToLowerInvariant()
    @'
function ConvertTo-PlainText([Security.SecureString]$SecureValue) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$env:COMPOSE_PROJECT_NAME = "__PROJECT_NAME__"
$saPassword = ConvertTo-PlainText (Read-Host "Enter a strong local SQL Server SA password" -AsSecureString)
if ($saPassword.Length -lt 12 -or
    $saPassword -notmatch '[A-Z]' -or
    $saPassword -notmatch '[a-z]' -or
    $saPassword -notmatch '[0-9]' -or
    $saPassword -notmatch '[^a-zA-Z0-9]') {
    throw "Use at least 12 characters with uppercase, lowercase, a number, and a symbol."
}

$env:SA_PASSWORD = $saPassword
& (Join-Path $root "scripts\setup\start-dev-fixed.ps1") -saPassword $saPassword
if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "Database startup failed." }

Remove-Item Env:SA_PASSWORD -ErrorAction SilentlyContinue
Start-Process (Join-Path $root "app\ExpenseTracker.WinForms.exe") -WorkingDirectory (Join-Path $root "app")
'@ | ForEach-Object { $_.Replace("__PROJECT_NAME__", $bundleProjectName) } |
        Set-Content (Join-Path $stagingRoot "Run-ExpenseTracker.ps1") -Encoding ASCII

    @'
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run-ExpenseTracker.ps1"
if errorlevel 1 pause
'@ | Set-Content (Join-Path $stagingRoot "Run-ExpenseTracker.bat") -Encoding ASCII

    @'
Expense Tracker review bundle

1. Install and start Docker Desktop.
2. Double-click Run-ExpenseTracker.bat and enter a strong local SQL Server password when prompted.
3. The first run downloads SQL Server and creates ExpenseDb with sample categories.
4. Later runs reuse the persistent Docker volume; ExpenseDb is not overwritten.

The app is self-contained and does not require the .NET SDK.
The Docker volume is persistent. Do not remove it unless you intend to delete its database.
'@ | Set-Content (Join-Path $stagingRoot "README.txt") -Encoding ASCII

    $stagedArchive = Join-Path $stagingRoot "release.zip"
    Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $stagedArchive -CompressionLevel Optimal
    Move-Item -Path $stagedArchive -Destination $archive
    Write-Host "Created $archive"
    $checksum = (Get-FileHash -Path $archive -Algorithm SHA256).Hash
    Write-Host "SHA-256: $checksum"
}
finally {
    if (Test-Path $stagingRoot) {
        Remove-Item $stagingRoot -Recurse -Force
    }
}
