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
- Cải thiện layout UI để không bị vỡ khi resize hoặc trên màn hình khác nhau.
- Kiểm tra trên nhiều kích thước màn hình và DPI khác nhau.
- Xử lý các trường hợp không có dữ liệu hoặc DB chưa khởi tạo.

## Giai đoạn 5 - Làm sạch repo và chuẩn hóa review
- Xóa build outputs, obj/bin, log không cần thiết.
- Xóa file DB binary không nên up vào Git.
- Giữ repo chỉ chứa code + config + schema + docs cần thiết.
- Giữ README và setup guide ngắn gọn, dễ hiểu cho người mới.
- Tạo quy tắc rõ ràng về file nào nên lên Git và file nào nên không lên Git.

## Giai đoạn 6 - Nâng cấp kiến trúc cho phát triển dài hạn
- Tách UI khỏi logic dữ liệu.
- Thiết kế lớp Repository/Service cho Categories và Expenses.
- Xây dựng model rõ ràng: Category, Expense.
- Chuẩn hóa xử lý exception và logging.
- Chuẩn bị cho việc thêm tính năng báo cáo, thống kê, lọc theo thời gian.

## Giai đoạn 7 - Mở rộng tính năng kinh doanh
- Thêm thống kê chi tiêu theo danh mục.
- Thêm báo cáo theo tháng / quý.
- Thêm lọc dữ liệu theo thời gian.
- Thêm nhập liệu nhanh và sửa dữ liệu đã có.
- Có thể thêm tính năng export CSV hoặc báo cáo PDF nếu cần thiết.

## Giai đoạn 8 - Tối ưu hóa và đóng gói cho người dùng
- Chuẩn bị cách build release ổn định.
- Xác định workflow triển khai cho máy dev và máy production.
- Tối ưu doc để người mới chỉ cần clone repo và chạy 1 lệnh.
- Tạo checklist QA cơ bản trước khi release.

## Kết luận
- Dự án này nên đi theo hướng: "mã nguồn sạch + môi trường chạy được trên máy mới + DB có thể tạo lại từ script".
- Đây là mô hình phù hợp cho dự án học tập, demo nội bộ và phát triển tiếp theo.
- Mục tiêu cuối cùng không phải chỉ có app chạy, mà là có repo có thể share, review và mở rộng dễ dàng.
