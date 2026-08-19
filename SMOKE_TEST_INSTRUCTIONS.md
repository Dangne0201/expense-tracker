SMOKE TEST — HƯỚNG DẪN NHANH

Mục tiêu
- Kiểm tra nhanh rằng repository có thể được clone, DB khởi tạo bằng Docker, và tests/integration chạy thành công trên máy khác.

Yêu cầu
- Windows (PowerShell) hoặc PowerShell 7
- Docker Desktop / Docker Engine (đã chạy)
- .NET SDK (phiên bản tương thích với project)
- Repo đã được clone (ví dụ: C:\projects\expense-tracker)

Các bước (1 lệnh, từ thư mục gốc của repo)
1) Mở PowerShell với quyền người dùng bình thường (không cần Admin) và chạy:
   PowerShell -ExecutionPolicy Bypass -File .\smoke-test-remote.ps1 -saPassword 'Your_password123' -RunApp:$false

Giải thích tham số
- -saPassword: mật khẩu cho tài khoản sa trong SQL Server container (thay đổi để an toàn)
- -RunApp:$false: chỉ chạy tests; để chạy GUI sau khi setup, dùng -RunApp:$true

Kết quả mong đợi
- Docker container SQL Server khởi động
- script setup-all.ps1 chạy (khởi tạo DB nếu cần, build solution)
- dotnet test chạy và hiển thị kết quả (Passed/Failed)

Nếu test fail
- Kiểm tra Docker đang chạy
- Kiểm tra .NET SDK đã cài
- Mở file smoke-test-remote.ps1 để xem log output
- Gửi nội dung lỗi cho người hỗ trợ (sao chép output console)

Các lưu ý an toàn
- Không commit mật khẩu vào Git
- Trên CI hoặc môi trường chia sẻ, lưu mật khẩu/connection string dưới dạng secret

Liên kết file trong repo
- Script: ./smoke-test-remote.ps1
- Setup script (đã gọi): ./setup-all.ps1
- Tests project: ./src/ExpenseTracker.Tests
- README: ./README.md

Mình đã chuẩn bị script và hướng dẫn này để gửi cho đồng nghiệp — nếu muốn, mình có thể tạo 1 file .zip chứa script + hướng dẫn hoặc soạn sẵn 1 email mẫu kèm hướng dẫn và lệnh để chạy.