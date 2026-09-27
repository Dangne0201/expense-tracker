# Project status and next steps

## Current scope

Expense Tracker is a single-user Windows desktop portfolio project. The WinForms app manages expense categories and expenses with SQL Server as persistent storage. Docker Compose provides a repeatable local database; the GUI runs natively on Windows.

## Implemented

- WinForms UI for creating categories and creating, viewing, editing, and deleting expenses.
- Positive amount validation, `DECIMAL(18,2)` range/precision checks, and category-name length validation.
- Parameterized SQL access in `ExpenseRepository`.
- Docker SQL Server setup with persistent volume and initialization from `data/init.sql`.
- Starter categories for a new database without fake expense history.
- Restricted local application login; setup protects its generated password for the current Windows user.
- Database-independent core tests and separately tagged repository integration tests on disposable SQL Server instances.
- Windows GitHub Actions workflow for restore/build/unit tests plus an Ubuntu disposable-SQL integration job.
- Reviewer-oriented setup, demo steps, architecture notes, and documented security limitations.
- Responsive window sizing, current-culture currency formatting, and a clear guard against deleting the total row.
- DataGridView columns sized with fill weights so the category column remains in the viewport when the window is resized.
- UI checks for visible grid-cell bounds and screenshot capture cropped to the app window.
- A locally generated, ignored self-contained Windows bundle that records its source commit and tracked changes in `BUILD-INFO.txt`.
- A disposable-database UI-test mode that does not run setup against the developer's persistent database and cleans up its uniquely named test category.
- A platform-neutral `ExpenseTracker.Core` project for validation, summaries, and the SQL repository, referenced by the WinForms UI.
- Expense date-range/category filters, a monthly total, a clear-filters action, and visible loading/empty/error status.
- Inline validation feedback, delete confirmation, keyboard mnemonics, and accessible names/descriptions for key controls.
- Typed category/expense/filter models with asynchronous SQL repository operations and cancellation for superseded expense loads.
- A tracked, idempotent database migration that enforces trimmed, nonblank, case-insensitively unique category names and positive expense amounts, and refuses unsafe legacy data.
- CSV export of filtered expense rows (not the display total), with invariant values, CSV quoting, and formula-prefix neutralization.
- Filter semantics documented: inclusive date range, monthly summary by month/category independent of grid dates, and Clear resetting all filters.

## Verification completed

- Release solution build: passed with 0 warnings and 0 errors.
- Unit tests: 33 passed, including culture-specific parsing, decimal boundaries, filter validation, CSV Unicode/formula safety, overview orchestration, and safe database-error messages.
- UI tests: 2 passed against a disposable SQL Server, covering add/edit/cancel/delete/restart, date-filtered and empty states, clear-filter recovery, CSV save/cancel/output, accessible names, and an 800x600 resize.
- Repository integration tests: 5 passed against an isolated disposable SQL Server; coverage includes CRUD, date/category filters, monthly totals, FK/uniqueness constraints, and DECIMAL(18,2)/Unicode/day-end round trips.
- A database-unavailable repository call against loopback port 1 produced a handled `SqlException`; the shared user-message mapping distinguishes unavailable SQL, uniqueness/FK constraints, and unexpected failures without including credentials.
- Release script verification now requires a clean committed worktree, records the full source SHA, and prints the ZIP SHA-256. The current modified worktree has not produced a release bundle.
- A temporary clean clone was populated with the current tracked diff and new source files, excluding ignored local settings and build output; the resulting source snapshot contained no `.env`, database, build, log, or ZIP artifacts before build. Release solution build passed with 0 warnings/errors and all 33 database-independent tests passed there. This is not a clean Windows profile/VM setup test, which remains pending.
- Migration failure tests against disposable SQL Server 2019 confirmed that duplicate, blank, and non-positive legacy data produce clear diagnostics, preserve existing rows, and roll back migration DDL.
- The setup script completed twice against an isolated SQL Server 2019 Compose project; it preserved the disposable database, reused the DPAPI-protected credential, and configured the `ExpenseApp` login. The disposable container, volume, and credential file were removed afterward.
- The `v0.3.10` bundle was built from commit `ca858d1`; its metadata, loopback Compose configuration, and startup files were inspected, and both UI workflows passed against its executable. The ignored ZIP is 52,987,384 bytes; SHA-256 is `EB718C9D56F1F59BC089273360A7176BCED23F06D57A10D8B19BD64A2C8EF4F6`.
- The final authentic screenshot was captured from the running app against a disposable database; the grid columns and footer were visible in the 1938x1038 app-window image.
- GitHub Actions `.NET` run [#10](https://github.com/Dangne0201/expense-tracker/actions/runs/36310319077) passed for source commit `94e3941`: Windows build/format/unit and Ubuntu disposable SQL integration. This run predates the current uncommitted changes; rerun CI after committing them. UI automation remains local/manual because it requires an interactive Windows desktop.
- The existing `expense-mssql` container uses the loopback-only port binding; disposable QA did not alter its persistent volume.
- GitHub Release [v0.3.10](https://github.com/Dangne0201/expense-tracker/releases/tag/v0.3.10) was published with the verified Windows x64 bundle built from `ca858d1`.

## Remaining proof before calling it ready to share

- A clean Windows profile/VM test was deferred; the setup/DPAPI reuse check above ran on the current profile with isolated Docker data.
- Databases with duplicate/invalid legacy values intentionally stop for manual resolution; setup does not merge or delete user data.

## Deliberate non-goals for this portfolio version

- Multi-user login/authorization: a desktop client connected directly to SQL Server cannot provide a trustworthy per-user security boundary. A future multi-user version needs a server-side service and ownership checks in every read/write operation.
- Web REST API, JWT, CORS, or browser security headers: these do not fit the current native WinForms architecture.
- Category deletion/editing, budgets, charts, and recurring transactions: defer until there is a clear product goal and test plan rather than adding portfolio features by checklist.

## Data safety

The Docker SQL Server volume is persistent. Normal setup does not delete or recreate it; the idempotent schema script preserves existing rows and blocks the category migration if duplicate/invalid legacy category names need manual resolution. Do not run `docker compose down -v` unless you explicitly intend to erase local database data.
