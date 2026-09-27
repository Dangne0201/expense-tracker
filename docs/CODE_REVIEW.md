# Code Review — Expense Tracker

## Mục tiêu

Tài liệu này dùng để review chất lượng mã nguồn, quy trình kiểm thử và khả năng bảo trì của dự án Expense Tracker trước khi phát hành hoặc mở rộng thêm tính năng. Mục tiêu không chỉ là kiểm tra app có chạy được hay không, mà còn đánh giá xem repo có đủ sạch, dễ setup, dễ review, dễ mở rộng và an toàn khi phát triển tiếp hay không.

## Phạm vi review

Review tập trung vào các khía cạnh chính sau:

- cấu trúc repo và tổ chức file
- cách khởi tạo môi trường dev
- cách kết nối và tương tác với SQL Server
- tính ổn định của các tính năng CRUD
- việc xử lý lỗi và validation dữ liệu
- sự sẵn có của kiểm thử (unit/integration/manual QA)
- tài liệu setup và review cho người mới
- các yếu tố an toàn như không commit secret và không commit tập tin nhạy cảm

## Tiêu chí đánh giá

### 1. Chất lượng mã nguồn

- Mã nên rõ ràng, dễ đọc và dễ hiểu.
- Tên biến, tên hàm, tên class nên phản ánh đúng chức năng.
- Không nên để logic nghiệp vụ lẫn trong giao diện UI quá nhiều.
- Có nên tách logic sang lớp/service riêng nếu logic phức tạp hơn.
- SQL query nên dùng parameterized queries để tránh SQL injection.

### 2. Tính ổn định của ứng dụng

- App nên khởi động được trên máy mới mà không cần cài SQL Server cục bộ.
- Nếu DB không sẵn sàng, app nên hiển thị thông báo rõ ràng thay vì treo.
- Các thao tác thêm/sửa/xóa danh mục và chi tiêu cần có xử lý lỗi tốt.
- Dữ liệu đầu vào phải validate đúng kiểu: số tiền, tên danh mục, ngày tháng, note.

### 3. Môi trường dev và setup

- Repo nên có cách chạy đơn giản: clone → setup → build → chạy.
- SQL Server nên chạy bằng Docker để dễ tái tạo trên máy khác.
- Dữ liệu schema và seed nên nằm trong file mã nguồn, ví dụ data/init.sql.
- Không nên lưu database binary như .mdf/.ldf vào Git.
- README và script setup cần ngắn, rõ, dễ làm theo.

### 4. Kiểm thử

- Cần có unit tests cho logic quan trọng.
- Có integration tests cho truy vấn DB và CRUD.
- Có smoke test để kiểm tra luồng chính trên máy local.
- Manual QA nên test các trường hợp lỗi, dữ liệu không hợp lệ và kết nối DB không sẵn sàng.

### 5. Dễ review và dễ bảo trì

- Repo nên có cấu trúc tương đối rõ: source code, scripts, data, docs.
- Tài liệu nên ngắn nhưng đủ để người mới build và test.
- Các file liên quan nên có tên và vị trí nhất quán.
- Không nên để file nhạy cảm hoặc output build loang trong repo.

## Checklist review bắt buộc

- Có review lại naming conventions chưa?
- Có chạy setup script trên môi trường mới chưa?
- Có kiểm tra database schema có khởi tạo tự động không?
- Có xem lại bảng Categories và Expenses có đúng cấu trúc chưa?
- Có validate input khi thêm category/expense chưa?
- Có xử lý trường hợp SQL Server chưa bắt đầu chưa?
- Có đọc lại README để chắc chắn các lệnh còn đúng không?
- Có xóa file build artifact, log, dữ liệu local không cần thiết chưa?
- Có kiểm tra secret / password chưa hardcode vào code không?
- Có chạy smoke test / build / test chưa?

## Kết quả review mong muốn

Một dự án tốt nên đạt được các tiêu chí sau:

1. Build và chạy được trên máy dev local.
2. Có thể khởi tạo database bằng Docker hoặc script đơn giản.
3. Có khả năng tái tạo môi trường mới nhanh chóng.
4. Có unit/integration test hoặc ít nhất smoke test cho luồng chính.
5. Không chứa dữ liệu nhạy cảm hay file tạm trong Git.
6. Có README và hướng dẫn setup đủ rõ để thành viên mới tiếp cận dễ dàng.
7. Có khả năng mở rộng thêm tính năng mà không phá vỡ cấu trúc hiện tại.

## Vấn đề cần lưu ý trong dự án Expense Tracker

### 1. Kết nối database

- App phải rõ ràng khi chọn connection string.
- Khi SQL Server không chạy, app cần báo lỗi có thể hiểu, không để crash không rõ nguyên nhân.
- Nên ưu tiên logic: lấy biến môi trường SQL_CONN, nếu không có thì fallback tới LocalDB hoặc Docker-local nếu cần.

### 2. Validation dữ liệu

- Tên category không được rỗng.
- Số tiền phải là số hợp lệ và không âm hoặc theo quy định của business.
- Nếu không chọn category thì không cho thêm expense.
- Khi có lỗi xảy ra, UI nên hiển thị thông báo rõ ràng hơn.

### 3. Dữ liệu và repo hygiene

- schema và seed dữ liệu nên được lưu trong data/init.sql.
- Không commit file .mdf/.ldf/.ndf hoặc các file logs local.
- Không commit secret hoặc password thực tế vào Git.
- Build output và file publish nên nằm trong folder build artifact hoặc ignore khi commit.

### 4. Test và CI

- Unit tests cần chạy nhanh và không phụ thuộc DB.
- Integration tests nên chạy trên SQL Server container hoặc môi trường có DB thực.
- Tối thiểu phải có smoke test cho luồng chính.
- Nếu có CI, nên chạy build + unit tests trên PR và chạy integration tests khi có Docker/kho dữ liệu sẵn sàng.

## Khuyến nghị hành động

1. Hoàn thiện quick-start cho người mới trong README.
2. Kiểm tra lại các scripts PowerShell để đảm bảo đường dẫn và tham số hoạt động ổn định.
3. Đảm bảo database init hoàn toàn có thể tái tạo từ file SQL.
4. Bổ sung test case cho luồng thêm danh mục và chi tiêu.
5. Thêm check lỗi rõ ràng trên UI và log startup.
6. Cập nhật CHANGELOG sau mỗi đợt phát triển mang tính chất user-visible.
7. Đảm bảo app không lưu password vào repo hoặc file local không kiểm soát được.

## Kết luận

Dự án Expense Tracker có nền tảng tốt cho một ứng dụng học tập và demo: có Docker-based setup, schema/migration từ source, UI automation, integration tests trên SQL disposable, và có giới hạn bảo mật được ghi rõ. Đây là template review/learning doc; các gợi ý dưới đây không phải cam kết rằng sản phẩm hiện thiếu toàn bộ các mục đó.

- validation rõ ràng,
- tests đầy đủ hơn,
- lỗi xử lý tốt hơn,
- tài liệu setup và changelog được cập nhật liên tục,
- tính an toàn và hygiene của repo được duy trì.

## Trạng thái hiện tại cần đối chiếu khi review

- `ExpenseTracker.Core` hiện có typed category/expense/filter models, asynchronous SQL repository methods, validation/summary helpers, và CSV serialization; `MainForm` vẫn giữ layout và orchestration, nên có thể tiếp tục tách view logic nếu app phát triển thêm.
- `data/init.sql` chạy lại an toàn và có migration có version; trên SQL Server 2019 đã kiểm tra nâng từ schema cũ, chạy script hai lần và giữ lại dữ liệu mẫu cũ. Duplicate/invalid legacy data sẽ chặn migration có chủ đích, không tự gộp dữ liệu.
- Local verification gần nhất: Release build không warning/error, 25 unit tests, 4 repository integration tests và 2 UI tests pass; integration/UI chạy với SQL disposable.
- GitHub Actions run [#8](https://github.com/Dangne0201/expense-tracker/actions/runs/36309807926) xanh cho commit `ca858d1` (Windows build/unit và Ubuntu SQL integration).
- Release public [v0.3.10](https://github.com/Dangne0201/expense-tracker/releases/tag/v0.3.10) được build từ `ca858d1`; ZIP và SHA-256 đã được kiểm tra.
- Clean Windows profile/VM và DPAPI first-run/reuse vẫn cần xác minh độc lập; kết quả local trên cùng profile không thay thế bằng chứng đó.

Khi review một commit mới, cập nhật phần trạng thái trên từ kết quả thực tế thay vì giữ số test, commit hoặc trạng thái release cũ.
