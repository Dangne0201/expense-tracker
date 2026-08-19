Manual QA Test Cases - Expense Tracker

1) Smoke tests (basic flows)
- [ ] Start DB with docker-compose up -d
- [ ] Build & run app
- [ ] Add a category
- [ ] Add an expense with valid amount and category
- [ ] Load expenses and verify the added record
- [ ] Delete the expense

2) Validation / edge cases
- [ ] Add category with empty name -> should be rejected
- [ ] Add expense with non-numeric amount -> should be rejected
- [ ] Add expense with negative amount -> check behavior
- [ ] Try to add expense when DB is unreachable -> app shows clear error

3) Integration checks
- [ ] Run integration test suite (dotnet test) against SQL Server container

4) Environment checks
- [ ] Verify README setup steps produce a running DB and app on a fresh machine
- [ ] Verify smoke-test-remote.ps1 works on another machine

Notes: Record results and any stack traces or screenshots for failures.