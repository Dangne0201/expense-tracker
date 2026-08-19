CODE REVIEW — ExpenseTracker (Phase 6)
Date: 2026-08-19
Reviewer: Copilot (AI assistant using Copilot CLI runtime in VS Code)

Summary
-------
Tổng quan: đây là code review cho các thay đổi liên quan đến Phase 6 (testing, smoke scripts, packaging, small runtime conveniences). Những thay đổi chính bao gồm:
- Thêm test scaffolding (unit, integration) và 1 dự án UI test (FlaUI).
- Thêm smoke-test-remote.ps1 và các run-*.ps1 helpers.
- Thay đổi Program.cs để đọc sqlconn.txt cạnh exe khi biến môi trường SQL_CONN thiếu.
- Sửa docker-compose healthcheck (TCP-based) để tránh false-unhealthy khi thiếu sqlcmd.
- Cập nhật scripts để tránh khởi chạy GUI hai lần và sửa lỗi PowerShell -RunApp parameter handling.

Scope
-----
Đánh giá chỉ bao gồm các file thay đổi gần đây liên quan đến Phase 6: scripts (.ps1), Program.cs, docker-compose.yml, test projects, và files tài liệu (TESTS_README.md, CODE_REVIEW.md, CHANGELOG.md).

High-severity findings (need xử lý sớm)
--------------------------------------
1) Program.cs: reading sqlconn.txt next to exe
   - Vấn đề: sqlconn.txt có thể chứa credentials (SA password). Mặc định đọc file này khi exe được double-click có thể vô tình để lộ secret hoặc bị commit nếu người dev không cấu hình .gitignore đúng.
   - Rủi ro: secrets leakage, non-reproducible runtime behavior giữa dev và prod.
   - Recommendation:
     - Treat sqlconn.txt strictly as developer convenience. Ensure it's listed in .gitignore and include sqlconn.sample.txt as template (already done).
     - Add a clear startup log message (or MessageBox for dev) saying "Using sqlconn.txt from exe folder (developer override)" so users know it's using a local file.
     - Prefer an opt-in approach: only read sqlconn.txt when a special file name exists (sqlconn.local.txt) or when an env var like USE_LOCAL_SQLCONN=1 is present.
     - Long-term: replace with a secure config flow (user prompt, per-user config, or installer-created config) instead of plaintext credential files.

2) setup-all.ps1 / PowerShell parameter handling
   - Vấn đề: Passing -RunApp:$false to a script invoked with PowerShell -File can produce type conversion errors because the argument becomes a string.
   - Current mitigation: callers were normalized (smoke-test uses PowerShell -Command wrapping). But the root script should be robust.
   - Recommendation: normalize RunApp inside setup-all.ps1 to accept flexible inputs and coerce to boolean. Example snippet below.

Suggested snippet for setup-all.ps1 (coerce RunApp to boolean):

# --- begin snippet ---
param(
    [Parameter()] [string] $saPassword,
    [Parameter()] [object] $RunApp
)

# Normalize
function To-Bool($v) {
    if ($null -eq $v) { return $false }
    if ($v -is [System.Management.Automation.SwitchParameter]) { return $v.IsPresent }
    try { return [bool]::Parse($v.ToString()) } catch { return $false }
}
$RunAppBool = To-Bool $RunApp
# Use $RunAppBool in the script
# --- end snippet ---

This change makes setup-all.ps1 tolerant of both "-RunApp" (switch), "-RunApp:$false", and calls through -File.

Medium-severity findings
------------------------
1) smoke-test-remote.ps1
   - Good: fixed duplicate launches and empty ArgumentList error.
   - Recommend: add explicit exit codes (e.g., exit 0 on success, exit 1 on failure) and more verbose logging for remote diagnostics. Also consider a --no-gui option separate from RunApp to avoid ambiguity.

2) UI test project / framework targets
   - The repo now contains ExpenseTracker.UiTests targeting net10.0-windows. Ensure FlaUI package versions chosen are compatible with target runtime on your machines/CI. If CI runs net7 or net8 by default, adjust target frameworks or add multi-targeting.

Low-severity / housekeeping
--------------------------
1) CI workflow (.github/workflows/ci.yml)
   - Contains placeholder SA password. Must be replaced with GitHub secrets before enabling.
   - Recommend: add a separate CI job for integration tests that runs only on PRs to specific branches or when label added, to avoid long runs on every push.

2) Artifact creation & release
   - artifacts/expense-tracker-v0.1.0.zip exists locally. Recommend moving artifact creation into CI or release pipeline so builds are reproducible.

3) Tests: separation & naming
   - Keep unit vs integration tests clearly separated (Traits or separate test projects). Currently both live in src/ExpenseTracker.Tests; prefer markers [Trait] or separate projects (ExpenseTracker.IntegrationTests) for clearer CI filtering.

Suggested fixes / actionable items
---------------------------------
1) Make setup-all.ps1 robust to RunApp input (see snippet above).
2) Update Program.cs behavior to be explicit/opt-in or add clear logging when using local sqlconn.txt. Example alternatives:
   - Read sqlconn.local.txt only when USE_LOCAL_SQLCONN env var set.
   - Or show a single MessageBox on startup if local override is used (for dev awareness).
3) Add explicit exit codes and more verbose logging to smoke-test-remote.ps1 (so remote dev can paste full logs when something fails).
4) Split tests into two projects or use [Trait] so CI can run unit tests fast and integration tests in a separate job.
5) Update CI workflow to use repository secrets for SA password and add a gating policy for integration tests.
6) Add a short contributor doc (or update TESTS_README.md) to explain how to run UI tests and that they require an interactive desktop.

Validation steps (how to verify fixes)
--------------------------------------
- Run unit tests: PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\run-unit-tests.ps1
- Run integration tests: PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\run-integration-tests.ps1 -saPassword '<secret>'
  - Verify DB initialized, tests run, and DB left in clean state.
- Run smoke test remote: PowerShell -ExecutionPolicy Bypass -File .\scripts\tests\smoke-test-remote.ps1 -saPassword '<secret>' -RunApp:$true
  - Verify only one GUI process started and smoke results passed.
- Double-click published exe on a clean machine with sqlconn.sample.txt copied as sqlconn.txt (local dev) and verify app connects to DB.
- On CI: push branch with changes to ci.yml (update to use secrets) and verify unit tests pass; gate integration tests to a separate job with Docker available.

Files changed in Phase 6 (reference)
------------------------------------
- src/ExpenseTracker.WinForms/Program.cs       (added sqlconn.txt read convenience)
- smoke-test-remote.ps1                        (RunApp handling, avoid duplicate launch)
- run-unit-tests.ps1, run-integration-tests.ps1, run-ui-tests.ps1
- src/ExpenseTracker.UiTests/ (new project + tests)
- docker-compose.yml                            (healthcheck changed to TCP)
- TESTS_README.md, CODE_REVIEW.md, CHANGELOG.md
- .github/workflows/ci.yml                       (CI skeleton)

Follow-ups (recommended next PRs)
---------------------------------
1) Make setup-all.ps1 change (parameter normalization) and unit-test that calling styles work both via -File and -Command.
2) Split tests: create ExpenseTracker.UnitTests + ExpenseTracker.IntegrationTests projects (or mark tests) and adjust run scripts and CI accordingly.
3) Decide Program.cs behavior (Keep dev-convenience vs Revert) and implement opt-in approach if keeping.
4) Harden smoke-test-remote.ps1: add --logfile option to save run output for easier troubleshooting by remote developer.
5) Add a small CONTRIBUTING.md with quick pointers to run tests and run smoke tests on other machines.

Notes on security & process
---------------------------
- Avoid committing any sqlconn.txt with real credentials. Use sqlconn.sample.txt and local .gitignore (already added).
- Use GitHub secrets for SA passwords and never commit plain-text secrets into repo.

If you want, I can next:
- Implement the setup-all.ps1 normalization snippet and run a local smoke/test to verify (will edit that script), or
- Create separate test projects (unit vs integration) and update CI workflow, or
- Modify Program.cs to make local sqlconn override opt-in and add a log message.

Bạn muốn mình tiếp tục với bước nào trong 3 lựa chọn trên? Nếu muốn, mình sẽ thực hiện thay đổi (edit files) và chạy build/tests để verify rồi báo kết quả.