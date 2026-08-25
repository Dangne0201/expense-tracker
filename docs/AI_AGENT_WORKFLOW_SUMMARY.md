# Tóm tắt quá trình học và làm việc với AI Agent trong project ExpenseTracker

Dự án này là một project học tập để làm quen với cách làm việc thực tế với AI coding agent trong môi trường repo có code, test, Docker, SQL Server và WinForms.

## 1. Mục tiêu của dự án

Project này tập trung vào việc xây dựng ứng dụng quản lý chi tiêu bằng:
- C# WinForms
- SQL Server
- Docker cho database
- Test automation
- AI agent workflow: instructions, skills, tools, session context


## 2. Những thứ đã học về AI agent

### Instructions
Instruction là quy tắc chung cho agent. Nó nói cho agent biết:
- repo này dùng convention gì
- file nên đặt ở đâu
- không nên tạo file rời rạc quá nhiều
- nên validate bằng lệnh nào
- không nên commit file build artifact hoặc DB local

Trong repo này, phần quan trọng nhất là [.github/copilot-instructions.md](D:/NTGiang/AITrainning/.github/copilot-instructions.md). Đây là nơi ghi quy tắc mà AI agent cần tuân theo.

### Skill
Skill là một “chuyên môn riêng” mà agent có thể dùng khi cần.

Ví dụ:
- setup project
- debug WinForms
- review code
- học AI agent
- nghiên cứu và tổng hợp kiến thức

Skill khác instruction ở chỗ: instruction là luật chung, skill là workflow chuyên biệt cho kiểu task cụ thể.

### Tools
Tool là tay chân của agent. Agent dùng tool để:
- đọc file
- sửa file
- tìm kiếm pattern
- build project
- chạy test
- chạy shell
- truy vấn DB
- làm việc với browser

### Session context
Session context là bối cảnh của phiên làm việc: repo nào, task nào, file nào đang xử lý, output trước đó là gì. Đây là thứ giúp agent biết đang ở đâu và đâu là mục tiêu hiện tại.

### MCP
MCP là Model Context Protocol: chuẩn để agent kết nối với hệ thống bên ngoài như:
- filesystem
- GitHub
- database
- browser
- API

Trong repo, MCP demo chỉ là sân chơi thử nghiệm, không phải phần core của ứng dụng chính.

## 3. Dự án này đã học cách tổ chức repo như thế nào

Mục tiêu là repo nên gọn, rõ, dễ đọc và dễ cho AI agent làm việc.

Một số phần quan trọng:
- [docs](D:/NTGiang/AITrainning/docs): tài liệu, hướng dẫn, review, planning
- [scripts/setup](D:/NTGiang/AITrainning/scripts/setup): scripts khởi tạo môi trường và setup DB
- [scripts/tests](D:/NTGiang/AITrainning/scripts/tests): script chạy unit/integration/UI/smoke tests
- [src](D:/NTGiang/AITrainning/src): source code app và test
- [data](D:/NTGiang/AITrainning/data): SQL schema/data init
- [artifacts](D:/NTGiang/AITrainning/artifacts): output build/package nếu có

Điều quan trọng là không để “file rời rạc lung tung”, không để mỗi task tự tạo file ngẫu nhiên ở root. Nếu có cùng loại công việc thì gom chung, nếu chưa có folder phù hợp thì tạo folder, còn chưa tạo thì không bắt buộc tạo file mới vô nghĩa.

## 4. Học về git, repo hygiene và docs

Repo cần giữ sạch và có tổ chức:
- không commit file build output
- không commit DB local binary
- không commit log / temp file không cần thiết
- giữ docs ngắn gọn nhưng đủ dùng
- xóa file duplicate và file template không còn cần

Các file như project plan, testing guide, code review đều có vai trò riêng:

- project plan: kế hoạch và mục tiêu phát triển
- testing guide: hướng dẫn test và cách chạy
- code review: checklist review chất lượng code

## 5. Học về Docker và SQL Server

Project này dùng Docker để chạy SQL Server local, vì vậy database không phụ thuộc vào máy dev và dễ khởi tạo lại.

Các file trọng tâm:
- [docker-compose.yml](D:/NTGiang/AITrainning/docker-compose.yml)
- [data/init.sql](D:/NTGiang/AITrainning/data/init.sql)
- [scripts/setup/setup-all.ps1](D:/NTGiang/AITrainning/scripts/setup/setup-all.ps1)

Cách hoạt động thực tế:
- Docker container chạy SQL Server
- DB name là ExpenseDb
- schema được tạo từ init.sql
- biến môi trường SQL_CONN dùng để app/test biết database đang ở đâu

Nếu app chạy local mà Docker đang chạy, nó ưu tiên dùng SQL Server trên localhost:1433. Nếu không có Docker hoặc không kết nối được, app mới thử LocalDB fallback.

## 6. Học về C# WinForms app

App chính nằm ở:
- [src/ExpenseTracker.WinForms](D:/NTGiang/AITrainning/src/ExpenseTracker.WinForms)

MainForm làm những việc chính:
- chọn DB phù hợp
- đọc biến môi trường SQL_CONN
- tự động thử Docker SQL / LocalDB nếu cần
- load category từ DB
- thêm category mới
- load bảng expenses
- thêm expense
- xóa expense đang chọn

Đây là một form UI quản lý chi phí đơn giản để học cách:
- kết nối C# với SQL Server
- xây UI bằng WinForms
- xử lý lỗi kết nối
- refresh dữ liệu sau khi thao tác

## 7. Học về test và validation

Project có 3 loại test chính:
- unit test: test logic nhanh
- integration test: test DB, thực hiện query thật
- UI test: test app GUI bằng automation

Các script tương ứng nằm trong:
- [scripts/tests/run-unit-tests.ps1](D:/NTGiang/AITrainning/scripts/tests/run-unit-tests.ps1)
- [scripts/tests/run-integration-tests.ps1](D:/NTGiang/AITrainning/scripts/tests/run-integration-tests.ps1)
- [scripts/tests/run-ui-tests.ps1](D:/NTGiang/AITrainning/scripts/tests/run-ui-tests.ps1)
- [scripts/tests/smoke-test-remote.ps1](D:/NTGiang/AITrainning/scripts/tests/smoke-test-remote.ps1)


## 8. Vấn đề SSL / cert trong local Docker

Một lỗi rất quan trọng đã gặp phải: SQL Server trong Docker dùng cert self-signed, nên client .NET hoặc sqlcmd reject vì không tin cert.

Cách xử lý đúng là:
- dùng `Encrypt=False;TrustServerCertificate=True;` trong connection string
- hoặc dùng `-C` trong sqlcmd khi test trực tiếp trong container

Đây là một bài học thực tế rất quan trọng: môi trường local Docker không giống production, nên cần xử lý cert đúng cách.

## 9. Những bài học lớn nhất

Bài học quan trọng nhất là:
- AI agent mạnh khi biết repo conventions và khi có instruction rõ ràng
- code sạch hơn nếu file và folder được tổ chức theo mục đích
- test phải chạy thật, không chỉ build thành công
- Docker-first setup rất hữu ích cho SQL Server
- cần hiểu rõ flow của app từ startup đến DB connection đến UI action

## 10. Kết luận

Project này không chỉ là một app quản lý chi tiêu. Nó còn là một bài học thực tế về:
- cách làm việc với AI agent
- cách tổ chức repo
- cách làm setup Docker + SQL
- cách chạy test và review code
- cách tránh rơi vào file rời rạc, docs thừa, hay code không có mục đích

Nói ngắn gọn: đây là một repo học tập để hiểu cách một AI agent và một developer thực tế làm việc với code, database, test và automation trong một môi trường thực tế.
