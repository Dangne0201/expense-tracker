# Hướng dẫn kiểm thử và smoke test cho Expense Tracker

File này là tài liệu gộp dùng để hướng dẫn chạy tất cả các loại test trong repo. Mục tiêu là có 1 nguồn hướng dẫn duy nhất thay vì tản mạn ở nhiều file.

## 1. Điều kiện cần có

Trước khi chạy test, đảm bảo:
- Windows 10/11
- Docker Desktop đang chạy (cho integration test + setup DB)
- .NET SDK đã cài
- Repo đã được clone về máy local

Mở PowerShell ở thư mục gốc repo:

 cd D:\NTGiang\AITrainning

## 2. Unit tests (nhanh, không cần DB)

### Dùng script

 PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\run-unit-tests.ps1



### Ý nghĩa
- Kiểm tra logic nghiệp vụ đơn giản, validate, tính toán, parse dữ liệu, xử lý lỗi.
- Không cần SQL Server.
- Dùng khi muốn kiểm tra nhanh ngay sau khi sửa logic.

## 3. Integration tests (cần SQL Server)

### Bước 1: khởi động DB và init schema

 PowerShell -ExecutionPolicy Bypass -File .\scripts\setup\setup-all.ps1 -saPassword "Your_password123" -RunApp:$false

### Bước 2: chạy integration tests

 PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\run-integration-tests.ps1 -saPassword "Your_password123"


### Ý nghĩa
- Test này tương tác với SQL Server thật.
- Dùng để kiểm tra CRUD, Categories, Expenses, kết nối DB và query thực tế.
- Nên chạy trong transaction hoặc rollback để dữ liệu test không dính bẩn.

## 4. UI tests (test giao diện WinForms)

### Bước 1: build app ở chế độ Debug

 dotnet build .\src\ExpenseTracker.WinForms\ExpenseTracker.WinForms.csproj -c Debug

### Bước 2: chạy UI tests

 PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\run-ui-tests.ps1

### Ý nghĩa
- Test các thao tác trên giao diện như: thêm category, nhập expense, xóa item.
- Yêu cầu máy có desktop tương tác; không nên chạy trên môi trường headless/container.

## 5. Smoke test (kiểm tra nhanh trên máy khác / kiểm tra môi trường)

Đây là cách chạy 1 lệnh để kiểm tra quãng đường chính của repo:

 PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\smoke-test-remote.ps1 -saPassword "Your_password123" -RunApp:$false

### Mục tiêu
- Khởi động Docker SQL Server
- Chạy setup-all.ps1
- Build solution
- Chạy nhanh test suite
- Xác nhận repo có thể chạy lại ở máy khác mà không cần setup thủ công nhiều bước

### Kết quả mong đợi
- Docker container SQL Server khởi động
- DB được init nếu chưa có
- dotnet test chạy và hiển thị Passed/Failed

### Nếu fail
- Kiểm tra Docker Desktop đang chạy
- Kiểm tra .NET SDK đã cài đúng
- Kiểm tra script setup và log output
- Nếu cần, chạy lại setup script với password đúng

## 6. Checklist test thủ công (manual QA)

Dùng khi muốn kiểm tra luồng người dùng thực tế. Đây là checklist cơ bản:

### Smoke flows (luồng chính)
- [ ] Start DB với Docker
- [ ] Build và chạy app
- [ ] Thêm một Category mới
- [ ] Thêm một Expense hợp lệ với amount và category
- [ ] Load lại danh sách để kiểm tra bản ghi hiển thị
- [ ] Xóa expense vừa thêm

### Validation / edge cases
- [ ] Thêm category rỗng -> bị từ chối
- [ ] Thêm expense với amount không hợp lệ -> bị từ chối
- [ ] Thêm expense với số âm -> kiểm tra quy định business
- [ ] DB không sẵn sàng -> app hiển thị lỗi rõ ràng

### Integration checks
- [ ] Chạy integration tests trên SQL Server container
- [ ] Đảm bảo dữ liệu test không để lại bẩn sau khi chạy

### Environment checks
- [ ] README setup steps tạo được DB và app trên máy mới
- [ ] Smoke test chạy đúng trên máy khác

## 7. Thứ tự chạy test hợp lý

Nên làm theo thứ tự sau:

1. Unit tests
2. Setup DB
3. Integration tests
4. Smoke test
5. UI tests (nếu cần đánh giá giao diện)

Lý do:
- unit test nhanh và dễ phát hiện lỗi logic ban đầu
- integration test cần DB thật
- smoke test kiểm tra môi trường end-to-end
- UI test cần desktop và chi phí cao hơn nên chạy cuối cùng

## 8. Lưu ý và mẹo

- Tách logic nghiệp vụ ra khỏi form để dễ test hơn.
- Dùng Moq hoặc mock cho logic không phụ thuộc DB.
- Với integration test nên ưu tiên chạy trên Docker SQL Server.
- UI automation hiện đang dùng FlaUI; các framework tương tự có thể là WinAppDriver, TestStack.White.
- Trong CI, unit test và integration test nên chạy trên runner Windows có Docker. UI test nên chạy trên môi trường desktop thật hoặc self-hosted Windows machine.


