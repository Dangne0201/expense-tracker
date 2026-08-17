Expense Tracker - Hướng dẫn nhanh (WinForms chính, LocalDB)

Tình trạng hiện tại
- Phiên bản hiện tại tập trung vào WinForms desktop app (src/ExpenseTracker.WinForms).
- Database vật lý (LocalDB .mdf/.ldf) đã được commit vào thư mục /data trong repo để tiện di chuyển giữa máy. (Lưu ý: commit file DB có thể làm tăng kích thước repo.)

Yêu cầu tối thiểu
- .NET SDK (đã thử nghiệm với .NET 10)
- Git
- SQL Server LocalDB (Windows) — dùng LocalDB để chạy DB đã commit.

Khởi động nhanh (máy mới)
1. Clone repo
   git clone https://github.com/Dangne0201/expense-tracker.git
   cd expense-tracker

2. Kiểm tra LocalDB (Windows)
   # khởi instance nếu chưa chạy
   sqllocaldb start MSSQLLocalDB
   sqllocaldb info MSSQLLocalDB

3. Attach database đã commit (nếu chưa attach trên máy này)
   # Thay đường dẫn nếu bạn clone vào folder khác
   sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "CREATE DATABASE ExpenseDb ON (FILENAME='$(pwd)\\data\\ExpenseDb.mdf'), (FILENAME='$(pwd)\\data\\ExpenseDb_log.ldf') FOR ATTACH;"

   Nếu lệnh trên lỗi với $(pwd), thay bằng đường dẫn tuyệt đối tới file data trong máy bạn, ví dụ:
   sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "CREATE DATABASE ExpenseDb ON (FILENAME='D:\\Projects\\expense-tracker\\data\\ExpenseDb.mdf'), (FILENAME='D:\\Projects\\expense-tracker\\data\\ExpenseDb_log.ldf') FOR ATTACH;"

4. Chạy WinForms app
   dotnet run --project src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj

5. Dùng ứng dụng
   - Nút Load: tải Categories và Expenses từ DB
   - Add/Edit/Delete: thao tác trực tiếp trên DB (ADO.NET)

Lưu ý quan trọng
- Vì DB đã được commit, repo sẽ lớn hơn; bạn đã đồng ý nên tôi đã thêm file vào git.
- Nếu muốn chia sẻ với người khác, họ chỉ cần clone và attach file .mdf/.ldf như hướng dẫn ở trên.
- Không còn migrations EF trong repo; nếu bạn muốn chuyển lại sang workflow migration (tái tạo schema từ code), có thể tái tạo migrations sau.

File & thư mục quan trọng
- /data/ExpenseDb.mdf, ExpenseDb_log.ldf  — database vật lý (đã commit)
- /src/ExpenseTracker.WinForms/  — WinForms app (UI chính, dùng ADO.NET)
- .gitignore  — hiện cho phép file DB trong /data (theo lựa chọn của bạn)
- LEARNING_PLAN.md, README.md  — tài liệu học tập và hướng dẫn

