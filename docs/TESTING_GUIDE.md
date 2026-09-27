# Testing guide

Run commands from the repository root (the directory containing `ExpenseTracker.sln`).

## Prerequisites

- Windows 10/11
- .NET SDK 10
- Docker Desktop only for integration and smoke tests
- An interactive Windows desktop for UI tests

## Unit tests

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tests\run-unit-tests.ps1
```

These tests cover database-independent rules in `ExpenseTracker.Core`, including culture parsing, filter validation, and CSV quoting/formula safety. The script filters out tests tagged `Category=Integration`, so it cannot accidentally connect to SQL Server.

## Integration tests

Use a disposable local Docker database only:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tests\run-integration-tests.ps1
```

The script securely prompts for the local SA password, starts SQL Server without force-recreating the container, and reapplies the idempotent schema/migration script without deleting existing rows. Integration tests are tagged `Category=Integration`, reject non-local connection targets, and use uniquely named test data with cleanup. Do not use them for shared or production data.

For a separately created disposable SQL Server on another loopback port, set `SQL_CONN` to that instance and pass its port. The test runner then validates that the target is `ExpenseDb` on `localhost` or `127.0.0.1` at exactly that port and skips the repository's persistent Docker setup:

```powershell
$env:SQL_CONN = "Server=127.0.0.1,11433;Database=ExpenseDb;User ID=sa;Password=<disposable-password>;TrustServerCertificate=True;Encrypt=False"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tests\run-integration-tests.ps1 -Port 11433
Remove-Item Env:SQL_CONN
```

Use only a disposable database for this mode; the test rolls back its transaction, but the custom instance is managed outside the setup script. Replace `<disposable-password>` in the connection string before running; do not commit real credentials.

## UI smoke test

For safe, isolated QA, start a disposable SQL Server on a non-default loopback port, initialize it with `data/init.sql`, then run the UI flow using its connection string:

```powershell
$env:SQL_CONN = "Server=127.0.0.1,11433;Database=ExpenseDb;User ID=sa;Password=<disposable-password>;TrustServerCertificate=True;Encrypt=False"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tests\run-ui-tests.ps1 -Configuration Release -Port 11433
Remove-Item Env:SQL_CONN
```

With a non-default port the script does not call repository setup; it requires `SQL_CONN` to target `ExpenseDb` on `localhost` or `127.0.0.1` at that exact port. The UI flow cleans up its uniquely named category and its expenses in teardown. Use only a disposable database regardless, and do not commit real credentials. The connection variable is restored when the script exits.

For the existing local development database, omit `-Port`; the script securely prompts for its SA password and starts/preserves Docker as needed. This mode writes a uniquely named UI test category to that database.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tests\run-ui-tests.ps1
```

Both modes build the WinForms app in the requested configuration and launch it through FlaUI. An interactive Windows desktop is required.

## Full local smoke path

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tests\smoke-test-remote.ps1
```

This starts Docker, reapplies the safe schema/migrations without removing existing database rows, creates/verifies the restricted application login, builds the test project, and runs only integration tests. It may add data only inside a rolled-back transaction; it never removes the Docker volume. Use the same SA password already associated with an existing volume.

## Manual QA

- Start Docker and launch the app.
- Add a non-empty category; confirm it appears after loading categories.
- Add a positive expense with a selected category.
- Double-click an expense, change its note, save, and confirm the update persists after reloading.
- Double-click an expense and press Escape; confirm edit mode is cancelled without saving.
- Confirm zero, negative, malformed, more-than-two-decimal, and out-of-range amounts are rejected.
- Load expenses and confirm the total row is shown.
- Confirm amounts use the current Windows culture's currency format; the total row must not be deletable.
- Confirm date filters are inclusive and monthly totals use the selected month/category regardless of the grid date range.
- Confirm filter empty/error states offer a clear/retry path; **Load Expenses** retries a failed request.
- Resize and maximize/restore the window; verify the grid and footer remain reachable, then exercise the form using keyboard navigation and Escape.
- Export filtered expenses; verify the CSV includes only matching expenses (not the TOTAL display row), uses invariant decimal/date values, safely quotes commas/quotes/newlines, and neutralizes formula-leading text.
- Delete the new expense and confirm it disappears.
- Stop SQL Server and confirm the app reports a connection problem.
- Apply `data/init.sql` twice to a disposable database; both runs should succeed without duplicating starter categories or migration records.
- Category names are trimmed by the UI, reject blank/space-padded values, and are unique ignoring case; a database-level duplicate must fail without exposing SQL details to the user.
- A filter start and end on the same day includes expenses throughout that day; monthly totals use the selected month and category, independent of the grid date range.
- Run the Windows setup workflow twice against a disposable database; the schema and migration should remain idempotent and preserve existing rows.
- Confirm a legacy database with duplicate categories or non-positive expense amounts fails the migration with a specific diagnostic and retains all rows for manual resolution.

## CI boundary

`.github/workflows/dotnet.yml` restores/builds the Windows solution and runs unit tests on Windows. A separate Ubuntu job starts an ephemeral SQL Server container, initializes it from `data/init.sql`, and runs integration tests against loopback port 11433; the container is removed whether tests pass or fail. UI automation still requires an interactive Windows desktop and remains local/manual.

The desktop client uses a local `ExpenseApp` SQL login restricted to database reader/writer roles. Setup protects its randomly generated password with Windows DPAPI for the current user. When `SQL_CONN` is configured but unavailable, startup retries twice with a short timeout before showing a database-unavailable message. Do not reuse this architecture for a shared or production database: a desktop client can still inspect its connection and read/write all rows, and SQL Server is intended to be bound to loopback for local demonstration.
