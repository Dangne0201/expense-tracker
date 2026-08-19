<#
smoke-test-remote.ps1
Script để chạy smoke test trên máy dev khác (Windows, PowerShell).
Usage (from repo root):
  PowerShell -ExecutionPolicy Bypass -File .\smoke-test-remote.ps1 -saPassword 'Your_password123' -RunApp:$false

Yêu cầu trước khi chạy:
- Docker Desktop hoặc Docker Engine đã cài và đang chạy
- .NET SDK tương thích (dotnet CLI) đã cài (phiên bản tương thích với project)
- PowerShell (Windows PowerShell / PowerShell 7)
- Đã clone repository vào máy

Script thực hiện:
- Kiểm tra Docker và dotnet
- Chạy docker compose up -d
- Gọi setup-all.ps1 để khởi DB và build project (không chạy GUI theo default)
- Thiết lập biến môi trường SQL_CONN cho tiến trình hiện tại
- Chạy dotnet test cho project tests (src\ExpenseTracker.Tests)
- (Tùy chọn) Nếu -RunApp, tạo wrapper để chạy ứng dụng GUI sau khi setup

Lưu ý bảo mật: thay thế 'Your_password123' bằng mật khẩu SA an toàn; không commit mật khẩu vào git.
#>

param(
    [string]$saPassword = "Your_password123",
    [Parameter()] [object]$RunApp = $false
)

# Normalize RunApp so the script accepts calling styles like:
#  .\smoke-test-remote.ps1 -RunApp:$false
#  PowerShell -File .\smoke-test-remote.ps1 -RunApp:$false
#  PowerShell -Command "& { .\smoke-test-remote.ps1 -RunApp:$false }"
$RunAppBool = $false
if ($RunApp -is [System.Management.Automation.SwitchParameter]) {
    $RunAppBool = [bool]$RunApp
} elseif ($RunApp -is [string]) {
    $s = $RunApp.ToString().Trim().ToLower()
    if ($s -in @('true','1','yes','$true')) { $RunAppBool = $true } else { $RunAppBool = $false }
} else {
    try { $RunAppBool = [bool]$RunApp } catch { $RunAppBool = $false }
}


function Write-Log { param($m) Write-Output "[smoke-test-remote] $m" }

# Check prerequisites
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Log "Docker CLI not found. Please install Docker Desktop or Docker Engine and ensure 'docker' command is on PATH." ; exit 1
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Log "dotnet CLI not found. Please install .NET SDK." ; exit 1
}

# Ensure running from repository root (look for setup-all.ps1)
if (-not (Test-Path "$PSScriptRoot\setup-all.ps1")) {
    Write-Log "Cannot find setup-all.ps1 in script folder. Run this script from the repository root." ; exit 1
}

Write-Log "Starting smoke test: bring up Docker SQL Server and run integration tests"

# Start Docker Compose and setup DB
Write-Log "Running docker compose up -d"
& docker compose up -d
if ($LASTEXITCODE -ne 0) { Write-Log "docker compose up failed (exit $LASTEXITCODE)"; exit $LASTEXITCODE }

# Call setup-all.ps1 to initialize DB and build solution
Write-Log "Calling setup-all.ps1 to initialize DB and build projects"
& "$PSScriptRoot\setup-all.ps1" -saPassword $saPassword -RunApp:$RunAppBool
if ($LASTEXITCODE -ne 0) { Write-Log "setup-all.ps1 failed (exit $LASTEXITCODE)"; exit $LASTEXITCODE }

# Configure SQL_CONN for the test run
$env:SQL_CONN = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=$saPassword;TrustServerCertificate=True;Encrypt=False;"
Write-Log "Set SQL_CONN for this session (not saved to disk)"

# Build tests and run them
Write-Log "Building and running tests (src\ExpenseTracker.Tests)"
Push-Location -Path "$PSScriptRoot\src\ExpenseTracker.Tests"
try {
    dotnet restore
    if ($LASTEXITCODE -ne 0) { Write-Log "dotnet restore failed"; exit $LASTEXITCODE }

    dotnet build --configuration Debug
    if ($LASTEXITCODE -ne 0) { Write-Log "dotnet build failed"; exit $LASTEXITCODE }

    dotnet test --no-build --logger "console;verbosity=normal"
    $testExit = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($testExit -ne 0) { Write-Log "Some tests failed (exit $testExit)" } else { Write-Log "All smoke tests passed" }

if ($RunAppBool) {
    Write-Log "RunApp specified: checking whether GUI is already running"
    # If setup-all already launched the GUI, don't start another instance.
    $proc = Get-Process -Name ExpenseTracker.WinForms -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Log "GUI already running (Id=$($proc.Id)); not starting new process."
    } else {
        $exe = "$PSScriptRoot\src\ExpenseTracker.WinForms\bin\Debug\net10.0-windows\ExpenseTracker.WinForms.exe"
        if (Test-Path $exe) {
            Write-Log "Starting GUI: $exe"
            Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
        } else {
            Write-Log "GUI executable not found at $exe (build output may differ)." 
        }
    }
}

exit $testExit
