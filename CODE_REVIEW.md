Code review summary (session: phase-6)

Findings & suggestions:
- Tests: integration test exists and passes locally; add more unit tests for non-UI logic when refactoring allows separation of concerns.
- Security: avoid committing sqlconn.txt with secrets. Provide sqlconn.sample.txt and add sqlconn.txt to .gitignore.
- Docker: healthcheck originally used sqlcmd which isn't in official image; changed to TCP-based check — acceptable for CI/dev. If stronger check desired, add mssql-tools or a custom image.
- Error handling: MainForm shows messageboxes on exceptions; consider centralizing logging and write to a log file for easier support.
- CI: Add workflow to run setup and tests on push/pull requests.

Action items:
- Add sqlconn.sample.txt and update .gitignore (optional)
- Expand unit tests to cover business logic once separated from UI
- Add CI workflow and ensure secrets are set for SA_PASSWORD when required
