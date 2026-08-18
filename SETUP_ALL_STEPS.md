SETUP ALL STEPS - Thiết lập máy mới (Docker-first)

Mục đích
--------
Tài liệu này là hướng dẫn duy nhất, đầy đủ, bằng tiếng Việt để thiết lập môi trường phát triển trên một máy Windows mới chỉ bằng Docker và PowerShell. Thực hiện theo từng bước dưới đây (copy/paste vào PowerShell) trên một máy hoàn toàn mới sẽ giảm thiểu lỗi permission/SSL/ENV mà ta đã gặp.

Yêu cầu trước khi bắt đầu
-------------------------
- Windows 10/11 với Docker Desktop (WSL2 backend) cài và đang chạy.
- PowerShell (mặc định Windows PowerShell hoặc PowerShell 7).
- Git (để clone). Nếu muốn build ứng dụng từ nguồn: .NET SDK (phiên bản tương thích, e.g. .NET 10 hoặc .NET 6/7 theo project).
- Docker Desktop cần cho phép host.docker.internal (Docker Desktop mặc định trên Windows hỗ trợ).

Tóm tắt file quan trọng trong repo
---------------------------------
- setup-all.ps1 (root): entrypoint "1-lệnh". Chạy: powershell -NoProfile -ExecutionPolicy Bypass -File .\setup-all.ps1 -saPassword "Your_password123"
- scripts\reset-db.ps1: tái tạo named volume và apply init.sql (khi cần rebuild DB từ đầu).
- docker-compose.yml: service expense-mssql, dùng Docker-managed named volume mssqldata.
- data\init.sql: schema + seed SQL để khởi tạo DB.
- start-expense-app.bat: batch wrapper để khởi WinForms với SQL_CONN (đã thêm TrustServerCertificate=True).
- src\ExpenseTracker.WinForms: mã nguồn WinForms (nếu muốn build/run từ source).

Bước 0 - Clone repository
-------------------------
1) Clone repo và chuyển tới thư mục:
   git clone <your-repo-url>
   cd <repo-folder>

Bước 1 - (Tùy) Xem/đổi mật khẩu SA dev
--------------------------------------
Trong hầu hết các hướng dẫn mặc định dùng mật khẩu dev: "Your_password123".
Bạn có thể thay bằng mật khẩu khác khi chạy setup-all.ps1 bằng tham số -saPassword.

Bước 2 - Chạy thiết lập chính (DB container + init + tạo batch wrapper + (tùy) build & run app)
-------------------------------------------------------------------------------------------
Mở PowerShell ở thư mục gốc repo (không cần quyền Admin theo mặc định)

Chạy (để up DB + init + build + run):
   powershell -NoProfile -ExecutionPolicy Bypass -File .\setup-all.ps1 -saPassword "Your_password123"

Nếu chỉ muốn up DB + init (không build/run app):
   powershell -NoProfile -ExecutionPolicy Bypass -File .\setup-all.ps1 -saPassword "Your_password123" -RunApp:$false

Giải thích ngắn:
- script sẽ gọi scripts\start-dev-fixed.ps1 để docker-compose up service expense-mssql, chờ SQL Server sẵn sàng, rồi áp data\init.sql nếu DB chưa tồn tại.
- script sẽ tạo start-expense-app.bat chứa SQL_CONN đầy đủ (bao gồm Password và TrustServerCertificate=True) và khởi app (nếu bật RunApp).

Bước 3 - Nếu lần đầu gặp lỗi permission (Access is denied) khi SQL bootstrap
------------------------------------------------------------------------------
Nguyên nhân phổ biến: volume cũ có quyền không tương thích, hoặc host bind-mount tới file .mdf gây Access denied.
Hướng khắc phục tạm (dev):
1) Dừng containers và xóa volume problem (trong PowerShell):
   docker compose down -v
2) Tạo lại volume và cấp quyền trên nội dung volume (dùng busybox/chmod):
   docker volume create mssqldata
   docker run --rm -v mssqldata:/data busybox sh -c "chmod -R 0777 /data || true; ls -la /data | sed -n '1,80p'"
3) Chạy lại setup-all.ps1 như ở Bước 2.

Bước 4 - Kiểm tra DB & kết nối thủ công (nếu cần)
--------------------------------------------------
Nếu muốn xác nhận DB đã tồn và init.sql đã chạy, dùng container mssql-tools để query (không cần cài sqlcmd trên host):

# Mount folder data và chạy init.sql hoặc query
$abs = (Resolve-Path .\data).Path
# Áp init.sql (nếu cần):
docker run --rm -v "${abs}:/data:ro" mcr.microsoft.com/mssql-tools /opt/mssql-tools/bin/sqlcmd -S host.docker.internal,1433 -U SA -P 'Your_password123' -i /data/init.sql
# Liệt kê databases:
docker run --rm mcr.microsoft.com/mssql-tools /opt/mssql-tools/bin/sqlcmd -S host.docker.internal,1433 -U SA -P 'Your_password123' -Q "SET NOCOUNT ON; SELECT name FROM sys.databases;"

Hoặc kiểm tra từ PowerShell trên host (dùng SqlClient):
$conn = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=Your_password123;Connection Timeout=3;TrustServerCertificate=True"
try { $c = New-Object System.Data.SqlClient.SqlConnection $conn; $c.Open(); $c.Close(); Write-Output 'DB OK' } catch { Write-Output "DB ERR: $($_.Exception.Message)" }

Bước 5 - Khởi app (GUI) bằng batch wrapper
------------------------------------------
Script tạo file start-expense-app.bat tại thư mục gốc repository. Chạy file đó để khởi WinForms app (nó sẽ set SQL_CONN và start exe).

   .\start-expense-app.bat

Nếu muốn khởi exe trực tiếp, export biến cho phiên shell trước khi chạy:
   $env:SQL_CONN = "Server=localhost,1433;Database=ExpenseDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True"
   Start-Process -FilePath ".\src\ExpenseTracker.WinForms\bin\Debug\net10.0-windows\ExpenseTracker.WinForms.exe" -WorkingDirectory ".\src\ExpenseTracker.WinForms\bin\Debug\net10.0-windows"

Bước 6 - Nếu gặp lỗi SSL Provider / certificate not trusted
------------------------------------------------------------
Triệu chứng: app kết nối tới SQL nhưng login thất bại với lỗi: "The certificate chain was issued by an authority that is not trusted.".
Giải pháp dev (đã áp dụng trong wrapper): thêm TrustServerCertificate=True vào connection string. Không dùng cách này cho production.

Bước 7 - Tái tạo DB sạch (khi muốn reset hoàn toàn)
-----------------------------------------------------
Sử dụng script helpers:
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\reset-db.ps1 -Force
Script này sẽ:
- docker compose down -v
- docker volume create mssqldata
- docker compose up -d
- apply data\init.sql

Bước 8 - Các lệnh kiểm tra hữu ích
----------------------------------
- Kiểm tra containers:
  docker ps --filter "name=expense-mssql"
- Xem logs:
  docker logs expense-mssql --tail 200
- Xem network:
  docker network ls
- Kiểm tra file start-expense-app.bat đã được tạo:
  type .\start-expense-app.bat
- Xem file logs app:
  Get-Content .\src\ExpenseTracker.WinForms\bin\Debug\net10.0-windows\logs\startup.log -Tail 200

Phần Troubleshooting ngắn
--------------------------
- Nếu container MSSQL không khởi: kiểm tra docker logs để xem lỗi Access denied hoặc permission. Nếu Access denied thì làm theo Bước 3.
- Nếu không thể connect từ host: kiểm tra port 1433 (bảo đảm không bị firewall/antivirus chặn) và thử docker run mssql-tools để test.
- Nếu start-expense-app.bat chứa ****** thay vì Password, mở file và sửa để chứa Password=$saPassword hoặc chạy setup-all.ps1 với tham số -saPassword để script tạo wrapper đúng.

Gợi ý repository hygiene (các file dư thừa)
-------------------------------------------
- Không commit file .mdf/.ldf/.ndf vào repo. Nếu có, di chuyển chúng ra folder backup ngoài repo (ví dụ D:\<backup>_db) và thêm pattern vào .gitignore.
- Các file tài liệu tạm thời (ONBOARDING.md, README_SETUP.md, SETUP_STEPS_VN.md, DATA_RESTORE.md) đã được hợp nhất vào file này. Bạn có thể xóa các file cũ nếu muốn; nếu không, chúng có link/trỏ đến file này.

Kết luận
--------
- Thực hiện tuần tự: clone → chạy setup-all.ps1 (với saPassword) → nếu permission lỗi làm theo Bước 3 → chạy start-expense-app.bat để khởi GUI.
- Nếu cần, gửi nội dung lỗi (docker logs và startup.log) cho tôi, tôi sẽ chỉ cách sửa cụ thể.

(END OF SETUP_ALL_STEPS)
