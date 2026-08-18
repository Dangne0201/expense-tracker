# Hướng dẫn học AI Agent: instructions, skills, tools, hooks, MCP, session context

File này dùng để tổng hợp các khái niệm cốt lõi khi làm việc với AI coding agent. Mục tiêu là hiểu rõ:
- agent là gì
- tool là gì
- skill là gì
- instruction là gì
- hook dùng để làm gì
- MCP server là gì
- session context và repo conventions là gì
- khi nào nên tạo phần nào

## 1. AI agent là gì?

AI agent không phải là một chatbot đơn thuần. Nó là một hệ thống có:
- 1 mô hình AI (bộ não)
- các tool để thao tác
- instruction để biết cách làm việc đúng chuẩn
- context để biết hiện tại đang làm gì
- permission / access để biết được quyền truy cập nào được cho phép

Nói gọn:

AI agent = model + tools + instructions + context + permissions

Ví dụ:
- Chatbot: hỏi một câu, trả lời theo lời hỏi
- Agent: đọc file, sửa code, chạy lệnh, đọc repo, review diff, kiểm tra build

Trong repo này, agent được dùng như một “trợ lý kỹ thuật” cho project WinForms + Docker + SQL Server.

## 2. Instructions là gì?

Instruction là các quy tắc hướng dẫn cho agent.

Nó nói cho agent biết:
- nên làm gì
- không nên làm gì
- ưu tiên gì
- repo này dùng convention nào
- validation nên chạy như thế nào

Ví dụ trong repo hiện tại:
- không commit file build artifact
- không commit DB local binary
- ưu tiên Docker-first setup
- keep docs short and practical

Instruction có thể nằm ở:
- `.github/copilot-instructions.md`
- `.github/instructions/*.instructions.md`
- các file custom instructions trong toolchain

### Khi nào tạo instruction?

Tạo instruction khi:
- bạn muốn quy tắc áp dụng chung cho mọi session
- repo cần có chuẩn mực rõ ràng
- các rule này nên được agent nhớ mãi

### Cách tạo instruction

Tạo file theo kiểu:

```md
---
description: Project coding conventions
---

# Project conventions

- Prefer Docker-first setup
- Do not commit local DB binaries
- Keep docs brief and current
- Validate with the smallest relevant command
```

Nói đơn giản:
- instruction là “luật chơi chung”
- nó không cần gọi theo tên, nó áp dụng tự động theo repo hoặc file pattern

## 3. Skill là gì?

Skill là một “chức năng chuyên biệt” của agent.

Nó giống như một bộ prompt + quy trình chuyên môn cho 1 loại công việc cụ thể.

Ví dụ:
- `code-review`
- `repo-setup`
- `debug-winforms`
- `ai-agent-learning`
- `research`

Skill giúp agent:
- biết khi nào dùng
- có workflow rõ ràng cho kiểu task đó
- module hóa kiến thức
- không phải nhắc lại nguyên tắc mỗi lần

### Skill nên đặt ở đâu?

Trong repo, thường là:
- `.github/skills/<skill-name>/SKILL.md`

Ví dụ:
- `.github/skills/ai-agent-learning/SKILL.md`

### Skill file mẫu

```md
---
name: repo-setup
description: Guide setup, Docker workflow, and repo hygiene.
---

# Repo Setup

Use this skill when the user needs to set up the project from scratch.

## Workflow
1. Check prerequisites
2. Start Docker
3. Run setup script
4. Build app
5. Keep generated files out of source control
```

### Khi nào tạo skill?

Tạo skill khi:
- bạn thấy một kiểu nhiệm vụ lặp lại nhiều lần
- bạn muốn lưu lại workflow chuyên biệt
- task đó có thể tách thành “chuyên môn riêng”

### Skill khác instruction như thế nào?

- instruction: quy tắc chung, áp dụng mọi lúc
- skill: chức năng chuyên môn, gọi khi cần

## 4. Tools là gì?

Tool là các thao tác mà agent có thể thực hiện.

Ví dụ tools phổ biến:
- đọc file
- sửa file
- chạy shell command
- kiểm tra git status
- search file
- browse / click trong browser
- truy vấn DB
- gọi API

Tool là “tay chân” của agent.

Nếu không có tools, agent chỉ có thể nói lý thuyết. Có tools mới có thể làm việc thật.

### Ví dụ tool thực tế trong môi trường này

- `view` để đọc file
- `edit` để sửa file
- `grep` để tìm kiếm pattern
- `powershell` để chạy lệnh Windows/PowerShell
- `browser` để mở và thao tác trang web
- `sql` để query DB

## 5. Hooks là gì?

Hook là “móc kích hoạt tự động” khi có sự kiện xảy ra.

Ví dụ:
- trước khi commit
- sau khi save file
- khi PR được mở
- khi file thay đổi
- khi task bắt đầu / kết thúc

Hook thường dùng để:
- tự động validate
- tự động lint
- tự động chạy test
- tự động review code
- tự động tạo checklist / ghi log

### Hook có phải bắt buộc không?

Không. Nhiều agent không cần hook. Hook chỉ là một cách để tự động hóa thêm.

### Ví dụ đơn giản

- Hook A: khi file `.cs` thay đổi, tự động chạy build
- Hook B: khi commit, tự động chạy unit test
- Hook C: khi mở PR, tự động review code

### Cách hiểu dễ nhớ

- instruction: quy tắc
- skill: chuyên môn
- tool: thao tác
- hook: sự kiện kích hoạt automation

## 6. MCP server là gì?

MCP = Model Context Protocol.

Nói đơn giản:
- MCP là một chuẩn để agent kết nối với hệ thống bên ngoài
- giúp agent truy cập dữ liệu hoặc tính năng mà không cần viết code riêng cho từng system

Ví dụ:
- MCP server cho filesystem
- MCP server cho GitHub
- MCP server cho database
- MCP server cho browser
- MCP server cho API

### MCP server hoạt động như thế nào?

Nói đơn giản:

- Client = agent / IDE / app
- Server = dịch vụ MCP
- Client hỏi server: “hãy đọc file / query DB / list repo / call API”
- Server trả kết quả theo chuẩn MCP

### Ví dụ thực tế trong repo này

Đã cài một server mẫu cho filesystem:
- `.vscode/mcp.json`
- `mcp-demo/...`

Nó cho phép agent:
- đọc thư mục
- list file
- tìm kiếm file
- tạo/xóa thư mục
- sửa file trong phạm vi được phép

### MCP có cho phép full quyền máy không?

Không phải tự động.

MCP chỉ mở quyền nếu:
- server được cài
- permission được cấp
- client/host cho phép
- access scope được giới hạn rõ

Nói đúng hơn:
- MCP = cổng kết nối hệ thống ngoài repo
- permission / scope quyết định quyền truy cập

### Khi nào cần MCP?

Cần khi:
- agent cần truy cập DB trực tiếp
- cần tự động làm việc với GitHub/PR/issue
- cần browser automation
- cần gọi API/servic e external
- cần kết nối với công cụ chuyên biệt ngoài repo

## 7. Session context là gì?

Session context là bối cảnh hiện tại của phiên làm việc.

Nó thường chứa:
- repo đang làm việc
- file đang active
- branch / status
- task hiện tại
- dữ liệu runtime của session

Nó giúp agent biết:
- đang ở đâu
- đang làm công việc gì
- đang ở repo nào
- kết quả trước đó trong cuộc làm việc

Ví dụ:
- “bạn đang ở repo ExpenseTracker”
- “target đang là file MainForm.cs”
- “đang review code mới”

## 8. Repo conventions là gì?

Repo conventions là các quy ước của project.

Ví dụ:
- code style
- docs format
- khi nào commit
- file nào nên ở repo, file nào không
- setup workflow
- validation command

Trong repo này là những thứ như:
- Docker-first setup
- không commit DB binary
- không commit build output
- README phải ngắn, đủ dùng
- keep source + schema + setup scripts

Nó giúp agent hoạt động đúng với project và không phá cấu trúc repo.

## 9. Khi nào nên tạo instruction, skill, tool, hook, MCP?

### Tạo instruction khi:
- quy tắc cần giữ mãi
- muốn agent luôn tuân theo 1 chuẩn
- repo có convention rõ

### Tạo skill khi:
- có một kiểu task lặp lại
- cần workflow chuyên biệt
- có thể tách thành “phần hiểu biết có cấu trúc”

### Tạo tool khi:
- agent cần một thao tác mà môi trường hiện tại chưa có
- cần kết nối với hệ thống mới
- cần automation chuyên biệt

### Tạo hook khi:
- cần tự động hóa theo event
- cần validate / lint / review khi sự kiện xảy ra

### Tạo MCP server khi:
- cần agent truy cập ngoài repo
- cần cho agent vào database, GitHub, browser, API, máy chủ
- cần standard protocol cho external integration

## 10. Cách tạo skill thật sự

### 1. Chọn folder phù hợp

```bash
.github/skills/<ten-skill>/
```

### 2. Tạo file `SKILL.md`

```md
---
name: repo-setup
description: Help users set up the project and understand the expected workflow.
---

# Repo Setup

Use this skill when the user needs help setting up the project.

## Workflow
1. Check prerequisites
2. Start Docker
3. Run the setup script
4. Build or run the app
5. Keep generated files out of Git
```

### 3. Lưu ý quan trọng
- `name` phải trùng với folder name
- description nên rõ ràng
- viết ví dụ cụ thể để agent biết dùng khi nào

## 11. Cách tạo instruction thật sự

```md
---
description: Repo conventions and setup rules
applyTo: '**/*'
---

# Repo conventions

- Prefer Docker-first setup
- Keep repo clean
- Do not commit local DB files
- Validate with smallest relevant command
```

## 12. Cách hiểu nhanh các khái niệm

- Chatbot: trả lời câu hỏi
- Agent: thực hiện công việc với tools
- Instruction: luật chơi
- Skill: chuyên môn
- Tool: hành động thực tế
- Hook: sự kiện trigger để automation
- MCP: chuẩn để mở kết nối ngoài repo
- Session context: bối cảnh hiện tại
- Repo conventions: quy ước của project

## 13. Mẹo học hiệu quả

Học theo kiểu “thực hành + ví dụ”:

1. Hỏi agent: “agent là gì?”
2. Hỏi: “skill khác instruction khác gì?”
3. Hỏi: “tool và mcp khác nhau như nào?”
4. Hỏi: “ở repo này nên tạo skill nào?”
5. Tự tạo 1 skill mẫu cho task lặp lại của bạn

## 14. Bài tập thực hành gợi ý

- Viết 1 skill `project-review` cho repo này
- Viết 1 skill `repo-setup` cho việc chạy dự án mới
- Viết 1 instruction cho rule “không commit file build artifact”
- Cài 1 MCP server mẫu cho filesystem
- Tạo 1 hook mẫu để chạy build khi file `.cs` thay đổi

## 15. Kết luận

AI agent mạnh khi có đầy đủ 5 thành phần:
- model
- tools
- instructions
- context
- permissions / integrations

Nếu thiếu một trong các phần này, agent sẽ yếu hoặc không hoạt động đúng như mong muốn.

Mục tiêu của bạn khi học agent là hiểu rõ:
- agent làm được gì
- nó bị giới hạn ở đâu
- nó được quyền truy cập gì
- repo rule là gì
- khi nào cần skill / instruction / MCP / hook

Đây là cách để bạn dùng AI agent một cách có kiểm soát, không mơ hồ, và hiệu quả hơn trong project thực tế.
