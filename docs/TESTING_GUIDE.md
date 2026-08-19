Test runner guide

Short summary to run different kinds of tests locally:

1) Unit tests (fast, do not touch DB)
   - Powershell: .\scripts\tests\run-unit-tests.ps1
   - Command: dotnet test src/ExpenseTracker.Tests -c Debug

2) Integration tests (need SQL Server)
   - Start DB/init: .\scripts\setup\setup-all.ps1 -saPassword 'Your_password123' -RunApp:$false
   - Or run helper: .\scripts\tests\run-integration-tests.ps1 -saPassword 'Your_password123'
   - Command: dotnet test src/ExpenseTracker.Tests -c Debug
   - Integration tests in the test project should use rollback/transactions so they leave DB clean.

3) UI automation tests (interactive)
   - Build WinForms app in Debug so exe is at bin\Debug\net10.0-windows\ExpenseTracker.WinForms.exe
     dotnet build src/ExpenseTracker.WinForms -c Debug
   - Then run: .\scripts\tests\run-ui-tests.ps1
   - UI tests use FlaUI and require an interactive desktop (they will not work in headless containers).

Notes & tips
 - Keep business logic out of forms so it can be unit-tested easily.
 - Use Moq for mocking dependencies in unit tests.
 - For integration tests prefer to run DB in Docker (scripts\setup\setup-all.ps1) and run tests inside a transaction and roll it back.
 - UI automation frameworks: FlaUI (used here), WinAppDriver, TestStack.White. FlaUI is good for WinForms on dev machines.
 - In CI you can run unit + integration tests on a windows-labeled runner with Docker available. UI tests require an interactive runner or self-hosted Windows machine.

If you want, I can:
 - add a sample unit test targeting a small business class,
 - convert any existing form logic into a testable class,
 - add CI steps to run these test scripts on push/PR.

Tell me which of the above to do next.