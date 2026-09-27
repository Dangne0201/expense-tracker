# Code review — Expense Tracker

## Current assessment

Expense Tracker is a focused junior-level Windows desktop portfolio project: WinForms UI, a testable platform-neutral core, SQL Server persistence through Docker, and reviewer-oriented setup/QA documentation. It demonstrates validation, parameterized SQL, migrations, error handling, CI, and both database and UI test approaches. It is intentionally a local single-user demo, not a production finance service.

## Strengths

- `ExpenseTracker.Core` keeps typed models, validation, summaries, CSV export, and repository operations outside the form.
- SQL access uses parameters and explicit types; schema creation and additive migrations are sourced from `data/init.sql`.
- Docker SQL Server uses a persistent volume and loopback-only port mapping. Setup avoids deleting or recreating existing data.
- The application uses a restricted `ExpenseApp` login. Locally generated credentials are protected for the current Windows user with DPAPI.
- Unit, repository integration, and interactive UI tests cover distinct layers. CI runs Windows build/format/unit checks and disposable SQL integration tests.
- The UI supports date/category filters, monthly totals, CSV export, accessible names, responsive resizing, and visible error/empty states.
- Documentation describes run/setup, test boundaries, engineering trade-offs, and deliberate security limitations.

## Latest evidence

- GitHub Actions [run #12](https://github.com/Dangne0201/expense-tracker/actions/runs/36315454008) passed for commit `ea23628`: Windows build/format/unit and Ubuntu disposable SQL integration. This workflow does not run interactive UI automation.
- Final local Release QA on the current working tree passed: build (0 warnings/errors), format verification, 33 unit tests, 5 isolated SQL integration tests, 2 UI tests, and whitespace validation. The disposable database was initialized twice, used only on loopback port 11433, then removed. Detailed evidence is recorded in [docs/PROJECT_PLAN.md](./PROJECT_PLAN.md) and [docs/TESTING_GUIDE.md](./TESTING_GUIDE.md).
- The current working-tree changes are uncommitted, so the GitHub Actions run above does not cover them; rerun CI after committing. UI automation remains local because it requires an interactive Windows desktop.
- The currently published [v0.3.10 release](https://github.com/Dangne0201/expense-tracker/releases/tag/v0.3.10) was built from `ca858d1`, before recent setup/UI changes. Do not treat that bundle as the latest source; build and verify a new bundle before distributing updated code.
- A clean Windows profile/VM setup has not been independently verified. DPAPI credentials are user/profile-bound, so fresh-profile setup remains useful final evidence.

## Trade-offs and limitations

- The desktop client connects directly to SQL Server. Local reader/writer permissions do not create a trusted multi-user boundary; a shared production app would need a server-side API and per-user authorization.
- Setup can reuse a saved `ExpenseApp` credential for ordinary startup on an existing volume that has no saved SA credential. Administrative actions such as migrations may still require the existing SA credential; never delete a volume to bypass this.
- UI automation requires an interactive Windows desktop and a clean disposable database for exact row-count assertions. It is not part of GitHub-hosted CI.
- The current public release is older than the latest source. A new release should follow clean-worktree, disposable database, startup, UI, and CI verification.
- Category edit/delete, budgets, charts, recurring expenses, multi-user auth, and a web API are outside the declared portfolio scope rather than partially implemented features.

## Review checklist for future changes

- Keep SQL parameterized and validate data in both application logic and database constraints where appropriate.
- Preserve persistent database data; test migrations and destructive scenarios on disposable databases.
- Keep credentials, connection files, database binaries, logs, and build/publish artifacts out of Git.
- Update README/setup/testing docs whenever commands, behavior, release artifacts, or QA evidence change.
- Run the narrow relevant checks first, then CI-equivalent build/format/unit and SQL integration checks for cross-cutting changes.
