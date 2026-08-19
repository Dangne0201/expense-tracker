Organizing files policy for AI agents (Instructions)

Mục tiêu
- Đảm bảo file mới hoặc thay đổi được đặt vào thư mục hợp lý theo loại (docs, scripts/setup, scripts/tests, src, data, artifacts, ...).
- Tránh tình trạng file rải rác khắp repo; đảm bảo agent biết manifest (nguồn chân lý) trước khi tạo file.

Vị trí manifest
- manifest nằm tại repo root: .file-catalog.json
- Agent bắt buộc đọc file này để biết ánh xạ pattern → folder trước khi tạo/di chuyển file.

Nguyên tắc hành xử của AI agent (bắt buộc)
1. Trước khi tạo hoặc thêm file mới, đọc .file-catalog.json để tìm category phù hợp.
2. Nếu có pattern khớp với tên file hoặc đường dẫn (ví dụ "*.md" → docs), tạo file ngay trong folder mục tiêu (không để file ở repo root).
3. Nếu không tìm thấy match:
   - Nếu user/maintainer đã cấu hình rõ policy cho repo (biến môi trường hoặc file config), tuân theo policy đó.
   - Ngược lại: Không tự ý tạo folder mới và di chuyển file. Thay vào đó, trả về một đề xuất gồm:
     a) Tên file/đường dẫn đề nghị
     b) Danh sách các category khả dĩ (ví dụ theo extension hoặc từ patterns tương tự)
     c) Một hành động đề nghị ("move to X", "create category Y and move", hoặc "ask user")
   - Chỉ khi user cho phép (explicit approval) mới thực hiện auto-create category và cập nhật manifest.
4. Khi di chuyển hoặc tạo folder mới, cập nhật .file-catalog.json (append category) và trả về diff của manifest (machine-readable JSON). Nếu agent không có quyền ghi, chuyển đề xuất cho maintainer.
5. Luôn cung cấp dry-run (mô tả chính xác hành động sẽ làm) trước khi thực thi thay đổi trên repo.

Agent checklist (trước khi commit)
- Đã đọc .file-catalog.json?
- File mới được đặt vào folder theo manifest không? Nếu có, OK.
- Nếu không có match: đã propose và chờ user/maintainer duyệt (hoặc được explicit allow auto-create)?
- Nếu thay đổi manifest: manifest diff đã được trả về và được user chấp nhận?
- Nếu di chuyển file: có thông báo (commit message / PR description) để reviewer hiểu thay đổi tổ chức?

Kịch bản mẫu
- Tạo README.md mới: agent thấy "*.md" → category docs → tạo file tại docs/README.md
- Tạo script khởi động: file tên start-dev.ps1 → pattern "start-*.ps1" khớp scripts/setup → tạo tại scripts/setup/start-dev.ps1
- Tạo file loại chưa có: new.gadget → không có pattern khớp → agent đề xuất "byext_gadget -> misc/gadget" rồi chờ user duyệt

Gợi ý kỹ thuật (cho maintainer/agent implementer)
- Giữ .file-catalog.json ở repo root; cập nhật khi có category mới.
- Agent nên trả về structured JSON khi propose (ví dụ: {actions: [{from, to, reason}], manifestDiff: {...}}) để dễ tự động hóa.
- Khuyến nghị kèm dry-run và một bước xác nhận người dùng cho auto-create category.
- Consider adding a repository-level flag to allow automatic category creation by trusted agents (opt-in).

Thực thi và kiểm tra
- Trước khi tự động di chuyển/ghi manifest, agent luôn chờ explicit approval nếu repo chưa bật auto-create.
- Sau tổ chức file, agent nên tạo commit / PR với mô tả rõ: "Organize files: moved X → Y; updated .file-catalog.json".

Nếu bạn muốn, tôi có thể:
- Tạo mẫu SKILL.md (mô tả interface) để agent gọi (không thực thi),
- Hoặc chỉ tạo docs/ORGANIZATION.md (đã tạo),
- Hoặc tạo mẫu JSON response và ví dụ dry-run.

File tạo: [ORGANIZATION.md](/D:/NTGiang/AITrainning/docs/ORGANIZATION.md)
