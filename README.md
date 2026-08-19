Expense Tracker

## Overview
- Dự án WinForms quản lý chi tiêu cá nhân.
- App kết nối SQL Server chạy trong Docker để dễ setup trên máy mới.
- Database không được lưu trong Git; schema được tạo lại từ file data/init.sql.

## Requirements
- Windows 10/11
- Docker Desktop đang chạy
- Git
- .NET SDK (nếu muốn build từ source)

## Quick start
1. Clone repo
   git clone https://github.com/Dangne0201/expense-tracker.git
   cd expense-tracker

2. Run the setup script
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup-all.ps1 -saPassword "Your_password123"

   Nếu chỉ muốn dựng Docker + DB mà không mở app:
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup-all.ps1 -saPassword "Your_password123" -RunApp:$false

3. What the script does
   - tạo SQL Server bằng Docker
   - chờ SQL Server sẵn sàng
   - chạy data/init.sql nếu database ExpenseDb chưa tồn tại
   - nếu RunApp = true thì build và mở WinForms app

## Run again later
- Bật Docker Desktop
- Trong repo, chạy:
  docker compose up -d
- Mở app bằng VS, dotnet run, hoặc chạy lại setup-all.ps1 nếu cần

## Troubleshooting
- Docker không chạy: mở Docker Desktop
- Port 1433 đang bị chiếm: kiểm tra SQL Server khác trên máy hoặc đổi port trong docker-compose.yml
- Database không khởi: chạy lại setup-all.ps1 với password tương ứng
- Nếu SQL Server liên tục restart và log báo "Access is denied" hoặc "master database file is owned by root": đây là lỗi permission trên Docker volume /var/opt/mssql/data. Khắc phục nhanh:
  docker run --rm -v expense_tracker_mssqldata:/var/opt/mssql/data alpine sh -c "chown -R 10001:10001 /var/opt/mssql/data || true"
  docker compose up -d
- Nếu muốn reset DB sạch:
  docker compose down -v
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup-all.ps1 -saPassword "Your_password123"

## Repo structure
- docker-compose.yml: cấu hình SQL Server trong Docker
- data/init.sql: schema + seed dữ liệu khởi tạo DB
- scripts/setup/: entrypoint chính cho máy mới và script khởi DB
- scripts/tests/: helper cho unit/integration/UI/smoke tests
- scripts/tools/: small helpers (organize-files.ps1)
- src/ExpenseTracker.WinForms: mã nguồn app

File organizer helper
- A small manifest lives at .file-catalog.json at the repo root that maps file patterns to canonical folders.
- Use the helper to move loose files into the proper folder and (optionally) auto-create categories:
  PowerShell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tools\organize-files.ps1 -Path "path\to\file.ext" -AutoCreate
- Agents should call this helper (or consult .file-catalog.json) before committing new files so files are placed into the appropriate folder instead of scattered in the repo.

## Notes
- Không commit file database binary (.mdf/.ldf/.ndf)
- Không commit .env thật
- Không commit build artifacts
- DB được tạo lại từ init.sql, nên repo nhỏ gọn và dễ share trên Git


