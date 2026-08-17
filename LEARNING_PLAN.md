KẾ HOẠCH HỌC & THỰC HÀNH: Expense Tracker + Học Copilot Agent

Mục tiêu chung
- Học và thực hành xây dựng một API Expense Tracker nhỏ bằng C#, ASP.NET Core, EF Core và SQL Server.
- Đồng thời học cách sử dụng GitHub Copilot Agent (code agents / skills), tận dụng AI để hỗ trợ phát triển, tạo skill, và workflow commit/PR.
- Kết quả: mã nguồn push lên GitHub (repo cá nhân) với hướng dẫn chạy (README), docker-compose cho SQL Server, và tests cơ bản.

Tình trạng hiện tại của máy (đã kiểm tra)
- .NET SDK: có (.NET 10)
- Git: có
- VS Code CLI (code): có
- sqlcmd / SqlLocalDB: có
- dotnet-ef: đã cài (10.0.11)
- Docker Desktop: đã cài và hoạt động (bạn đã xác nhận)
- Đã tạo: docker-compose.yml và .env.example ở thư mục dự án

Lưu ý an toàn khi đưa repo lên GitHub
- Không commit file .env (chứa SA_PASSWORD) — thêm .env vào .gitignore.
- Commit docker-compose.yml và .env.example (mẫu, không chứa secrets).
- Trong README, ghi rõ bước sao chép .env.example -> .env và cách thiết lập password an toàn.

Kế hoạch 2 ngày (bản dành để gửi cho sếp)
Thời lượng tổng: 2 ngày (~12–16 giờ). Mỗi ngày gồm các tasks nhỏ, kèm: học gì / implement gì / hỏi Copilot Agent / verify.

Ngày 1 — Thiết lập môi trường, domain model, EF Core (6–8 giờ)
1. Chuẩn bị môi trường & repo (30–45 phút)
   - Học: dotnet CLI cơ bản, git init/branching, lý do dùng .NET solution.
   - Implement: tạo solution + project webapi (dotnet new webapi), tạo repo, commit ban đầu; thêm .gitignore (kèm .env) và .env.example (đã có).
   - Hỏi Copilot Agent: "Tạo .gitignore cho C# .NET project và README khởi".
   - Verify: dotnet build chạy được, git log có commit ban đầu.

2. Thiết kế model (Expense, Category) + DTO (60–90 phút)
   - Học: entity design, data types (decimal cho amount, DateTimeOffset), DTO vs Entity.
   - Implement: tạo Models/Entities và DTOs.
   - Hỏi Agent: "Sinh class C# cho entity Expense và Category và DTOs với DataAnnotations".
   - Verify: kiểm tra model hợp lý, không leak navigation properties vào DTO.

3. Thiết lập EF Core DbContext và migrations (60 phút)
   - Học: DbContext, DbSet, Fluent API, connection string, DI trong Program.cs, migrations với dotnet-ef.
   - Implement: AppDbContext, đăng ký DbContext, tạo migration đầu (InitialCreate).
   - Hỏi Agent: "Tạo AppDbContext với cấu hình precision cho Amount và disable cascade delete nếu cần".
   - Verify: dotnet ef migrations add InitialCreate thành công; dotnet ef database update chạy; bảng xuất hiện trong DB.

4. (Tuỳ chọn) Seed dữ liệu demo (30 phút)
   - Học: seed EF Core (ModelBuilder.HasData hoặc custom seeder).
   - Implement: thêm vài category/expense mẫu.
   - Hỏi Agent: "Sinh code seed dữ liệu cho categories và expenses".
   - Verify: kiểm tra dữ liệu trong DB.

Ngày 2 — API endpoints, tests, Git + Copilot skills (6–8 giờ)
5. Category CRUD API (60 phút)
   - Học: designing RESTful endpoints, trả status codes hợp lý.
   - Implement: CategoriesController (GET, GET/{id}, POST, PUT, DELETE) sử dụng DTOs.
   - Hỏi Agent: "Generate CategoriesController with async CRUD using EF Core and DTOs".
   - Verify: test endpoints bằng curl/Postman.

6. Expense CRUD API + filtering/paging (90–120 phút)
   - Học: query params, filtering by date range & category, paging, EF Core projection vào DTO.
   - Implement: ExpensesController với GET filters, POST/PUT/DELETE.
   - Hỏi Agent: "Create ExpensesController with GET supporting categoryId, from, to, page, pageSize; use projection to DTO".
   - Verify: thử các query param, kiểm tra SQL log để tránh N+1.

7. Unit tests (xUnit) cơ bản (60–90 phút)
   - Học: xUnit basics, InMemory provider vs mocking DbContext.
   - Implement: tests cho POST expense, invalid request, GET filter.
   - Hỏi Agent: "Generate xUnit tests for ExpensesController using InMemory DB".
   - Verify: dotnet test chạy xanh.

8. Git workflow & sử dụng Copilot Agent cho commit/PR (30–45 phút)
   - Học: cách viết commit message rõ ràng, feature branch, PR body.
   - Implement: commit theo bước, push lên GitHub, tạo PR draft.
   - Hỏi Agent: "Viết commit message và PR body tóm tắt thay đổi và checklist".
   - Verify: PR trên GitHub có mô tả và hướng dẫn test.

9. Tài liệu & polish (45–60 phút)
   - Học: viết README rõ ràng, bật Swagger, small error handling.
   - Implement: README với hướng dẫn run, migrate, docker compose; bật Swagger.
   - Hỏi Agent: "Sinh README.md gồm bước cài, chạy docker compose, apply migrations, curl examples".
   - Verify: chạy theo README trên máy sạch (hoặc Docker) và app hoạt động.

Phần đặc biệt: Học Copilot Agent & tạo skill (đưa vào kế hoạch học)
- Mục tiêu học thêm: cách tương tác với Copilot Agent để
  - Tạo code scaffold (controllers, DTOs, DbContext),
  - Tạo và chạy unit tests tự động,
  - Sinh commit messages, PR descriptions,
  - (Nâng cao) viết một "skill" hoặc workflow agent tùy chỉnh để tự động hoá tác vụ (ví dụ: tạo migration, chạy test, tạo PR draft).

Tasks cụ thể liên quan đến Agent/Skill (sắp xếp theo độ ưu tiên)
1) Học cách viết prompts hiệu quả (1–2 giờ, rải trong 2 ngày)
   - Học: prompt context (file, mục tiêu), cung cấp ví dụ, yêu cầu cụ thể.
   - Thực hành: dùng Agent để tạo controller + test, so sánh mã do Agent sinh với tay viết.
2) Dùng Agent để tạo commit message và PR body (15–30 phút)
   - Học: formattng PR, checklist, migration steps.
   - Implement: mỗi lần commit, ghi message được Agent đề xuất và chỉnh sửa nếu cần.
3) Tìm hiểu tạo skill / custom agent (1–3 giờ, tuỳ depth)
   - Học: docs của Copilot Agent / GitHub Actions / Copilot CLI skill creation (tùy platform), design principle của skill.
   - Implement (optional): viết script hoặc config để tự động hoá: chạy migration, tạo db, chạy tests, và tạo PR draft.
   - Hỏi Agent: "Gợi ý bước để tạo skill tự động hoá migration + tests + PR".
   - Verify: skill hoạt động trên local hoặc cloud dev environment (nếu triển khai).

File nên có trong repo (khi push lên GitHub)
- src/ (code project)
- tests/ (xUnit tests)
- docker-compose.yml (commit)
- .env.example (commit)
- .gitignore (chứa .env)
- README.md (chỉ dẫn chạy & test)
- LEARNING_PLAN.md (file này) — mô tả những gì đã học để sếp xem

Checklist (cho sếp)
- [ ] Tạo project và cấu trúc solution
- [ ] Thiết kế entity + DTOs và mapping
- [ ] Thiết lập EF Core + migrations (migration đầu)
- [ ] CRUD endpoints cho Category và Expense (filtering/paging)
- [ ] Unit tests (xUnit) cơ bản pass
- [ ] Docker compose cho SQL Server; README hướng dẫn chạy
- [ ] Sử dụng Copilot Agent để scaffold code, generate tests, commit messages, PR draft
- [ ] Code và docs pushed lên GitHub (repo cá nhân)

Bước tiếp theo đề xuất (ngay bây giờ)
1. Tạo .gitignore và thêm .env vào đó (nếu bạn muốn, tôi có thể tạo giúp).
2. Tạo README.md mẫu (tôi sẽ tạo và bạn có thể chỉnh) gồm: cách chạy docker compose, tạo migration, chạy app, curl examples.
3. Bắt đầu scaffold project (dotnet new webapi) — tôi có thể chạy các lệnh và tạo file nếu bạn muốn tôi làm.

Bạn muốn tiếp theo là tôi thực hiện bước nào?
- A: Tạo .gitignore và README.md mẫu ngay (khuyến nghị)
- B: Bắt đầu scaffold project (tạo solution + webapi)
- C: Bạn muốn tự làm, tôi chỉ hướng dẫn chi tiết từng bước

Trả lời bằng lựa chọn A, B hoặc C hoặc tự nhập ý muốn của bạn.
