Expense Tracker

Mô tả ngắn
- Dự án WinForms quản lý chi tiêu cá nhân.
- App dùng SQL Server chạy trong Docker để dễ setup trên máy mới.
- Database không được lưu trong Git; schema được tạo lại từ file data/init.sql.

Yêu cầu
- Windows 10/11
- Docker Desktop đang chạy
- Git
- .NET SDK (nếu muốn build từ source, không bắt buộc nếu dùng runtime có sẵn)

Khởi động trên máy mới
1. Clone repo
   git clone https://github.com/Dangne0201/expense-tracker.git
   cd expense-tracker

2. Chạy setup 1 lệnh
   powershell -NoProfile -ExecutionPolicy Bypass -File .\setup-all.ps1 -saPassword "Your_password123"

   Hoặc chỉ khởi DB + init SQL mà không chạy app:
   powershell -NoProfile -ExecutionPolicy Bypass -File .\setup-all.ps1 -saPassword "Your_password123" -RunApp:$false

3. Script sẽ làm gì
   - khởi SQL Server bằng Docker
   - chờ DB sẵn sàng
   - chạy data/init.sql nếu database ExpenseDb chưa tồn tại
   - nếu bật RunApp thì build và mở WinForms app

Chạy lại sau khi máy đã cài xong
- Bật Docker Desktop
- Trong repo, chạy:
  docker compose up -d
- Sau đó mở app theo cách bạn cần (VS, dotnet run, hoặc chạy lại setup-all nếu muốn)

Cấu trúc repo quan trọng
- docker-compose.yml: cấu hình SQL Server trong Docker
- data/init.sql: schema + seed dữ liệu khởi tạo DB
- setup-all.ps1: entrypoint chính cho máy mới
- start-dev-fixed.ps1: script khởi DB và init schema
- src/ExpenseTracker.WinForms: mã nguồn app

Lưu ý
- Không commit file .mdf/.ldf/.ndf, không commit .env thật, không commit build output
- DB được tạo lại từ init.sql, nên repo nhỏ gọn và dễ share trên Git

