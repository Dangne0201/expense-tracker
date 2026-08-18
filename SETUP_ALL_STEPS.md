SETUP ALL STEPS - Thiết lập máy mới

Mục tiêu
- Mỗi máy mới chỉ cần clone repo và chạy một lệnh để có SQL Server + schema sẵn sàng.
- Database không nằm trong Git. Nó sẽ được tạo lại từ file data/init.sql.

Yêu cầu
- Windows 10/11
- Docker Desktop đang chạy
- PowerShell
- Git
- .NET SDK (nếu muốn build từ source)

Cấu trúc quan trọng
- setup-all.ps1: script khởi động toàn bộ môi trường
- start-dev-fixed.ps1: script chính để up Docker SQL và chạy init.sql
- docker-compose.yml: cấu hình SQL Server trong Docker
- data/init.sql: schema + seed database
- src/ExpenseTracker.WinForms: mã nguồn app

Bước 1: Clone repo
- git clone <repo-url>
- cd <repo-folder>

Bước 2: Chạy setup 1 lệnh
Mở PowerShell trong thư mục repo và chạy:

powershell -NoProfile -ExecutionPolicy Bypass -File .\setup-all.ps1 -saPassword "Your_password123"

Nếu chỉ muốn khởi Docker + DB mà không build/run app:

powershell -NoProfile -ExecutionPolicy Bypass -File .\setup-all.ps1 -saPassword "Your_password123" -RunApp:$false

Script sẽ làm gì
- khởi SQL Server bằng Docker
- chờ DB sẵn sàng
- chạy data/init.sql nếu database ExpenseDb chưa tồn tại
- nếu RunApp = true thì build và mở app WinForms

Bước 3: Chạy lại sau này
Khi máy đã setup xong, không cần chạy lại setup-all mỗi khi mở app. Chỉ cần:

1) Bật Docker Desktop
2) Trong repo, chạy:
   docker compose up -d
3) Mở app bằng IDE hoặc build thủ công nếu cần

Bước 4: Nếu gặp lỗi
- Docker không chạy: mở Docker Desktop
- Port 1433 đang bị chiếm: kiểm tra SQL Server khác trên máy, hoặc đổi port trong docker-compose.yml
- DB không khởi: chạy lại setup-all.ps1 với password tương ứng
- Nếu muốn reset DB sạch, xóa volume Docker và chạy lại setup:
  docker compose down -v
  powershell -NoProfile -ExecutionPolicy Bypass -File .\setup-all.ps1 -saPassword "Your_password123"

Bước 5: Kiểm tra nhanh
- docker ps
- docker logs expense-mssql --tail 200

Lưu ý quan trọng
- Không commit file database binary (.mdf/.ldf/.ndf)
- Không commit .env thật
- Không commit build artifacts
- Repo nên chứa code + cấu hình + schema, còn dữ liệu app thì được rebuild từ init.sql
