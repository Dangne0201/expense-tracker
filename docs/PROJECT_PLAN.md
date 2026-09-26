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
- Windows GitHub Actions workflow for restore, build, and unit tests.
- Reviewer-oriented setup, demo steps, architecture notes, and documented security limitations.
- Responsive window sizing, current-culture currency formatting, and a clear guard against deleting the total row.
- A versioned `v0.3.8` review bundle that includes the setup script in its expected directory structure.

## Remaining proof before calling it ready to share

- Run the documented setup and `v0.3.8` review bundle on a second clean Windows profile/VM; record the exact outcome and verify DPAPI credential creation/reuse there. Bundle setup, DPAPI create/reuse, app launch, and tests passed in an isolated project/profile directory on the current Windows user. Rebuild the bundle after the latest source UI layout adjustment before distributing it.
- Reapply and verify the current loopback-only port binding on any existing Docker container created with the older all-interface mapping; do this only with the matching SA password and preserve the existing named volume.
- Integration test passed against a separately-created, disposable SQL Server container on loopback port 11433; repeat on a clean reviewer machine if possible.
- The interactive FlaUI launch and automated add/edit/cancel/delete/restart flow passed against that disposable database; repeat the same path manually for an interview demo if useful.
- Capture and inspect an authentic screenshot or short screen recording from the running app on a desktop where the whole form is visible; include it only after confirming all controls and grid columns are in frame.
- Confirm a successful GitHub Actions run before describing CI as green. No completed workflow run is currently available for this branch.

## Deliberate non-goals for this portfolio version

- Multi-user login/authorization: a desktop client connected directly to SQL Server cannot provide a trustworthy per-user security boundary. A future multi-user version needs a server-side service and ownership checks in every read/write operation.
- Web REST API, JWT, CORS, or browser security headers: these do not fit the current native WinForms architecture.
- Category deletion/editing, budgets, charts, and recurring transactions: defer until there is a clear product goal and test plan rather than adding portfolio features by checklist.

## Data safety

The Docker SQL Server volume is persistent. Normal setup does not delete or recreate it, and database initialization is skipped when `ExpenseDb` already exists. Do not run `docker compose down -v` unless you explicitly intend to erase local database data.
