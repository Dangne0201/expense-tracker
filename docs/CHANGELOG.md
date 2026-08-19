# Changelog

All notable changes to this project will be documented in this file.

## [v0.1.0] - 2026-08-19
### Added
- Initial packaging for release: published WinForms build (Release/publish).
- Integration tests project added (src/ExpenseTracker.Tests) with a DB integration test.
- Docker Compose healthcheck improved to use TCP check.
- Project start-up reads local `sqlconn.txt` from the executable folder to allow double-click run with a local connection string.
- Smoke test scripts and instructions: `smoke-test-remote.ps1`, `SMOKE_TEST_INSTRUCTIONS.md`.

### Fixed
- Fixed DataReader disposal in integration test to avoid transaction/reader conflict.
- Resolved local dev healthcheck issue by adjusting docker-compose healthcheck.

### Notes
- Do not commit `sqlconn.txt` containing secrets. Use `sqlconn.sample.txt` as a template if needed.
- CI pipeline and additional unit/UI tests remain as planned in Project Plan (Giai đoạn 6).
