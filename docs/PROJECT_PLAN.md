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
- Database-independent validation/summary tests and separately tagged, local-only integration test.
- Windows GitHub Actions workflow for restore, build, and database-independent unit tests.
- Reviewer-oriented setup, demo steps, architecture notes, and documented security limitations.
- Responsive window sizing, current-culture currency formatting, and a clear guard against deleting the total row.
- DataGridView columns sized with fill weights so the category column remains in the viewport when the window is resized.
- UI checks for visible grid-cell bounds and screenshot capture cropped to the app window.
- A locally generated, ignored self-contained Windows bundle that records its source commit and tracked changes in `BUILD-INFO.txt`.
- A disposable-database UI-test mode that does not run setup against the developer's persistent database and cleans up its uniquely named test category.

## Verification completed

- Release solution build: passed with 0 warnings and 0 errors.
- Unit tests: 13 passed.
- UI tests: 2 passed against a disposable SQL Server; the extracted `v0.3.9` bundle executable also passed both UI tests.
- Integration test: 1 passed against a disposable SQL Server.
- The `v0.3.9` bundle was rebuilt from the current source and its metadata and startup files were inspected. It is ignored by Git.
- The final authentic screenshot was captured from the running app against a disposable database; the grid columns and footer were visible in the 1938x1038 app-window image.
- GitHub Actions `.NET` workflow for commit `d5ddf32` completed successfully.
- The follow-up documentation commit `b562c76` also passed GitHub Actions; the disposable SQL connection examples in the testing guide were parsed and verified after correction.
- The existing `expense-mssql` container was recreated with the loopback-only port binding; it is healthy and still uses the `expense_tracker_mssqldata` volume.
- GitHub Release `v0.3.9` was published with the verified Windows x64 bundle built from `d5ddf32`.

## Remaining proof before calling it ready to share

- Repeat setup and review-bundle startup on a second clean Windows profile/VM; verify DPAPI credential creation/reuse there. Prior bundle setup and credential tests were run only on the current Windows profile.
- Repeat the manual interview demo on a clean reviewer machine if useful.

## Deliberate non-goals for this portfolio version

- Multi-user login/authorization: a desktop client connected directly to SQL Server cannot provide a trustworthy per-user security boundary. A future multi-user version needs a server-side service and ownership checks in every read/write operation.
- Web REST API, JWT, CORS, or browser security headers: these do not fit the current native WinForms architecture.
- Category deletion/editing, budgets, charts, and recurring transactions: defer until there is a clear product goal and test plan rather than adding portfolio features by checklist.

## Data safety

The Docker SQL Server volume is persistent. Normal setup does not delete or recreate it, and database initialization is skipped when `ExpenseDb` already exists. Do not run `docker compose down -v` unless you explicitly intend to erase local database data.
