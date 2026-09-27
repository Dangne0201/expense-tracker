# Testing guide

Run commands from the repository root (the directory containing `ExpenseTracker.sln`).

## Prerequisites

- Windows 10/11
- .NET SDK 10
- Docker Desktop for SQL integration tests
- An interactive Windows desktop for UI tests

## Unit tests

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tests\run-unit-tests.ps1 -Configuration Release
```

The script builds the solution and runs tests not tagged `Category=Integration`. These checks do not connect to SQL Server.

## Integration tests

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tests\run-integration-tests.ps1 -Configuration Release
```

The default port uses the repository's persistent local Docker `ExpenseDb`; tests create uniquely named records and clean them up, but this is not a disposable database. Setup generates and DPAPI-protects a local SQL admin credential on first initialization, then reuses saved credentials. For isolated testing, initialize a disposable SQL Server on a non-default loopback port and pass its connection using `SQL_CONN`:

```powershell
$env:SQL_CONN = "Server=127.0.0.1,11433;Database=ExpenseDb;User ID=sa;Password=<disposable-password>;TrustServerCertificate=True;Encrypt=False"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tests\run-integration-tests.ps1 -Configuration Release -Port 11433
Remove-Item Env:SQL_CONN
```

Replace the placeholder locally; never commit an actual password. The custom-port test runner validates that the target database is `ExpenseDb` on `localhost` or `127.0.0.1` at exactly the requested port and skips repository setup.

## UI tests

UI automation requires an interactive Windows desktop. For a repeatable full run, use a clean disposable SQL Server on a non-default loopback port, initialize it with `data/init.sql`, and set `SQL_CONN` to that database:

```powershell
$env:SQL_CONN = "Server=127.0.0.1,11433;Database=ExpenseDb;User ID=sa;Password=<disposable-password>;TrustServerCertificate=True;Encrypt=False"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tests\run-ui-tests.ps1 -Configuration Release -Port 11433
Remove-Item Env:SQL_CONN
```

The custom-port mode rejects non-local hosts, other databases, and port 1433. The CRUD flow checks exact row counts, creates a uniquely named test category/expenses, and removes only those test records during teardown. A persistent database containing unrelated expense rows may fail those exact-count assertions; do not clean user data to make the test pass.

To run against the default local database, omit `-Port`; the script uses the setup flow and may use the saved `ExpenseApp` credential without asking for the SA password. Use this only when you accept UI-test changes to the local database.

## Full local smoke path

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tests\smoke-test-remote.ps1
```

This starts/reuses Docker, reapplies safe schema migrations, builds, and runs integration tests. It never deletes the persistent volume, but its default target is still the local development database; prefer the custom disposable-instance path for isolated QA.

## Manual QA

- Add a non-empty category and a positive expense; confirm both persist after reload.
- Edit an expense, save, then cancel another edit with Escape.
- Confirm zero, negative, malformed, more-than-two-decimal, and out-of-range amounts are rejected.
- Confirm the total row is displayed and cannot be deleted.
- Check inclusive date filters, category filters, monthly totals, and Clear reset behavior.
- Confirm empty/error states explain recovery and **Load Expenses** retries a failed request.
- Resize/maximize/restore; at 800x600 verify grid/footer reachability, readable filters, keyboard navigation, and visible focus.
- Export filtered expenses; confirm only matching records are exported (not TOTAL), with invariant values, CSV quoting, and formula-prefix neutralization.
- Stop SQL Server, confirm a connection error is shown, restart it, and retry loading.
- Apply `data/init.sql` twice to a disposable database; verify migrations and starter categories are not duplicated.
- Verify unsafe legacy category/amount data blocks migration with a diagnostic and preserves rows for manual resolution.

## CI boundary and latest evidence

`.github/workflows/dotnet.yml` restores/builds the solution, verifies formatting, and runs database-independent tests on Windows. A separate Ubuntu job starts SQL Server, initializes a disposable `ExpenseDb`, and runs repository integration tests. Interactive UI automation is local/manual.

The latest verified GitHub Actions workflow is [run #12](https://github.com/Dangne0201/expense-tracker/actions/runs/36315454008) for commit `ea23628`; Windows build/format/unit and Ubuntu SQL integration passed. UI tests were not run by that workflow.

Final local Release QA on the current working tree passed: solution build (0 warnings/errors), formatting verification, 33 unit tests, 5 integration tests, 2 UI tests, and `git diff --check`. The integration and UI suites used an isolated SQL Server 2019 container on `127.0.0.1:11433`; `data/init.sql` applied successfully twice, and the disposable container was removed after testing. The persistent development container remained healthy and was not used by those suites. These uncommitted changes still need a CI run after they are committed; this local result does not replace the GitHub Actions evidence above.

The app uses a local `ExpenseApp` SQL login restricted to reader/writer roles. Setup protects the generated application credential with Windows DPAPI. Runtime logs at `%LOCALAPPDATA%\ExpenseTracker\logs\application.log` record operation, exception type, and SQL error number, not connection strings or credentials. This direct desktop-to-database design is for a local single-user portfolio demo, not a production security boundary.
