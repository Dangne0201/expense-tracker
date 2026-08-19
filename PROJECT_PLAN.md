# Kế hoạch phát triển dự án Expense Tracker

## Mục tiêu dự án

- Xây dựng ứng dụng WinForms quản lý chi tiêu cá nhân.
- Dữ liệu lưu trữ trong SQL Server.
- Môi trường dev phải dễ setup trên máy mới bằng Docker.
- Repo phải sạch, dễ review và dễ share cho người khác.

## Giai đoạn 1 - Khởi tạo ý tưởng và nền tảng dự án

- Xác định phạm vi dự án: quản lý thu chi, danh mục chi tiêu, lưu trữ dữ liệu, thao tác CRUD cơ bản.
- Tạo project WinForms ban đầu với giao diện đơn giản.
- Thiết kế dữ liệu cơ bản:
  - Categories
  - Expenses
- Xây dựng màn hình chính để:
  - hiển thị danh mục
  - thêm danh mục mới
  - hiển thị chi tiêu
  - thêm chi tiêu
  - xóa chi tiêu
- Đảm bảo app có thể chạy trên máy local với cơ sở dữ liệu SQL Server.

## Giai đoạn 2 - Thiết lập môi trường phát triển

- Chuẩn hóa môi trường bằng Docker để không phụ thuộc vào máy cài SQL Server cục bộ.
- Tạo file docker-compose.yml để chạy SQL Server 2019.
- Tạo script setup-all.ps1 để chạy 1 lệnh cho người mới.
- Tạo file start-dev-fixed.ps1 để:
  - khởi động container
  - chờ database sẵn sàng
  - chạy init.sql nếu cần
- Tạo file data/init.sql để tái tạo schema và dữ liệu mẫu.
- Sử dụng môi trường biến .env hoặc tham số -saPassword để tránh hardcode mật khẩu vào code.

## Giai đoạn 3 - Xây dựng kết nối dữ liệu và logic nghiệp vụ

- Kết nối app với SQL Server bằng connection string.
- Đọc dữ liệu từ Categories và Expenses.
- Thực hiện CRUD cơ bản cho các bảng chính.
- Xử lý lỗi kết nối và hiển thị thông báo rõ ràng cho người dùng.
- Bổ sung logging startup để debug khi DB không kết nối được.

## Giai đoạn 4 - Tăng độ ổn định và dễ sử dụng

- Thêm kiểm tra đầu vào:
  - tên danh mục không được rỗng
  - số tiền phải hợp lệ
  - phải chọn category trước khi thêm chi tiêu
- Xử lý các trường hợp không hợp lệ.

## Giai đoạn 5 - Làm sạch repo và chuẩn hóa review

- Giữ README và setup guide ngắn gọn, dễ hiểu.
- Tạo quy tắc rõ ràng về file nào nên lên Git và file nào nên không lên Git.

## Kết luận

- Dự án này nên đi theo hướng: "mã nguồn sạch + môi trường chạy được trên máy mới + DB có thể tạo lại từ script".
- Đây là mô hình phù hợp cho dự án học tập, demo nội bộ và phát triển tiếp theo.
- Mục tiêu cuối cùng không phải chỉ có app chạy, mà là có repo có thể share, review và mở rộng dễ dàng.

## Giai đoạn 6 - Kiểm thử, đánh giá mã, và chuẩn bị phát hành (Test & Release Prep)

Mục tiêu: đảm bảo chương trình hoạt động trên máy phát triển hiện tại và trên máy khác, có đủ kiểm thử tự động và thủ công, tiến hành code review và cập nhật CI để ngăn regressions.

1. Thiết lập theo dõi công việc (todos)

- Tạo các tác vụ theo bước để dễ theo dõi: smoke test trên máy hiện tại, test trên máy khác, unit tests, integration tests, manual UI test, code review, cập nhật CI, cập nhật tài liệu và đóng gói/release.

2. Kiểm thử trên máy hiện tại (smoke test)

- Mục tiêu: xác nhận build thành công và các luồng chính hoạt động.
- Các bước:
  - Chạy script khởi tạo môi trường: setup-all.ps1
  - docker-compose up -d để chạy SQL Server
  - Chạy data/init.sql nếu cần
  - dotnet build .\src\ExpenseTracker.WinForms\
  - Chạy ứng dụng (bằng Visual Studio hoặc exe từ bin) và thực hiện các kịch bản cơ bản: thêm category, thêm expense, xóa expense, hiển thị danh sách.

3. Kiểm thử trên máy khác (cross-machine test)

- Mục tiêu: đảm bảo repo + script đủ để tái tạo môi trường và chạy app trên máy khác.
- Các bước:
  - Trên một máy khác (hoặc VM/WSL), clone repo và chạy setup-all.ps1 hoặc các lệnh trong README.
  - Kiểm tra các yêu cầu: Docker, .NET SDK phiên bản tương thích, quyền truy cập mạng nếu cần.
  - Thực hiện smoke test tương tự.

4. Kiểm thử tự động

- Unit tests (business logic): viết và chạy unit tests cho lớp xử lý nghiệp vụ (tách logic khỏi UI nếu có thể).
- Integration tests (DB): tạo bộ test tích hợp chạy trên SQL Server container (sử dụng docker-compose trong CI) để kiểm tra các truy vấn CRUD.
- (Tùy chọn) UI automation: ghi nhận test thủ công đầu tiên; nếu cần automation sau này, cân nhắc WinAppDriver hoặc tương tự.

5. Kiểm thử thủ công chi tiết (manual QA)

- Chuỗi test case để QA làm theo: tạo user data mẫu, thêm/sửa/xóa chi tiêu, kiểm tra validation, kiểm tra xử lý lỗi khi DB không sẵn sàng.
- Kiểm tra edge cases: số tiền âm, tên category trùng, kết nối DB thất bại, dữ liệu lớn.

6. Code review

- Checklist review:
  - Có unit tests cho logic quan trọng không?
  - Xử lý lỗi rõ ràng và logging đầy đủ không?
  - Tránh SQL injection: dùng parameterized queries/ORM.
  - Không commit secrets hoặc file nhạy cảm.
  - Giao diện người dùng có phản hồi lỗi rõ ràng không?
  - Tài liệu setup/update README có đầy đủ bước tái tạo môi trường?
- Thực hiện review, sửa theo comment và re-run tests.

7. CI / Automation

- Thêm hoặc cập nhật pipeline (GitHub Actions) để chạy: restore, build, unit tests, và integration tests (khởi Docker SQL Server trong job nếu cần).
- Thêm job smoke test hoặc publish artifact nếu pipeline qua.

8. Tài liệu và phát hành

- Cập nhật README với các bước test nhanh và FAQ troubleshooting.
- Thêm CHANGELOG.md và tag release (ví dụ v0.1.0).
- Chuẩn bị artifact: dotnet publish hoặc tạo installer nếu cần.

9. Vận hành và hậu release

- Backup DB, migration plan (migrations hoặc versioned SQL scripts).
- Logging/telemetry cơ bản (file/console) để hỗ trợ debug trong môi trường người dùng.

10. Theo dõi & backlog

- Tạo backlog issues cho bug found trong test và feature requests.
- Lên kế hoạch release tiếp theo sau khi fix bug quan trọng.
