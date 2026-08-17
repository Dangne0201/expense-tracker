Expense Tracker - Hướng dẫn nhanh (LocalDB - đơn giản)

Mục tiêu
- API nhỏ để quản lý chi tiêu (Expense) và category.
- Dùng C#, ASP.NET Core Web API, EF Core và SQL Server (LocalDB cho phát triển cá nhân, Docker optional cho reproducibility).

Yêu cầu tối thiểu (đơn giản)
- .NET SDK (bạn đã có .NET 10)
- Git
- SQL Server instance: LocalDB (Windows) hoặc SQL Server (bạn có thể đã có SSMS). Nếu không, README có hướng dẫn tùy chọn.

Khởi động nhanh (khuyến nghị dùng LocalDB trên Windows)
1. Clone repo
   git clone <your-repo-url>
   cd <repo-folder>

2. (Không cần Docker) — dùng LocalDB (mặc định đã cấu hình trong appsettings.Development.json)
   - Nếu bạn có SQL Server/LocalDB: tiếp bước 3.
   - Nếu không có LocalDB và không muốn cài SQL Server, bạn có thể dùng Docker (xem phần "Tùy chọn Docker" bên dưới).

3. Tạo database & apply migrations
   dotnet tool restore
   dotnet ef database update --project src\ExpenseTracker.Api\ExpenseTracker.Api.csproj --startup-project src\ExpenseTracker.Api\ExpenseTracker.Api.csproj

4. Chạy ứng dụng
   dotnet run --project src\ExpenseTracker.Api\ExpenseTracker.Api.csproj

5. Mở Swagger UI để test
   Mở trình duyệt: http://localhost:5119/swagger (hoặc URL output khi app chạy)

6. Chạy tests
   dotnet test

Tùy chọn Docker (nếu bạn muốn reproducible environment cho người khác)
- Nếu muốn dùng Docker để khởi SQL Server container (khuyến nghị khi chia sẻ với người không dùng Windows):
  copy .env.example .env
  docker compose up -d
  dotnet ef database update --project src\ExpenseTracker.Api\ExpenseTracker.Api.csproj

Lưu ý khi push lên GitHub
- Không commit file .env (đã thêm vào .gitignore)
- Commit docker-compose.yml và .env.example (không chứa secrets)

Ghi chú nhanh
- Project mặc định được cấu hình để dùng LocalDB (appsettings.Development.json). Đây là lựa chọn đơn giản cho phát triển cá nhân trên Windows.
- Docker là tùy chọn nếu bạn muốn người khác chạy giống môi trường của bạn (cross-platform).

Nếu muốn, tôi sẽ thay đổi cấu hình hoặc hướng dẫn cài LocalDB/SQL Server step-by-step — bạn muốn tiếp theo là gì?