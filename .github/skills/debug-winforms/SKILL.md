---
name: debug-winforms
description: Debug WinForms app behavior, database connectivity, DataGridView issues, and setup problems in this ExpenseTracker project.
---

# Debug WinForms

Use this skill when the app fails to start, data does not load, UI behavior is wrong, or the WinForms form needs debugging in relation to SQL Server and local setup.

## What this project typically involves

This repo has a desktop WinForms app with a SQL Server-backed data layer. The common debugging areas are:

- database connection problems
- missing SQL Server container or wrong connection string
- DataGridView sorting and display issues
- load logic not refreshing after add/delete actions
- app build issues caused by stale output or locked files

## Debugging workflow

1. Confirm the database is running
2. Confirm the app uses the expected connection string
3. Check whether the SQL schema was initialized
4. Confirm the DataGridView bindings and sort logic
5. Validate build/run with the smallest command that checks the issue
6. Keep the fix targeted and explain the root cause

## Typical checks

- Check Docker status: `docker ps`
- Confirm SQL container is healthy
- Build the app: `dotnet build src/ExpenseTracker.WinForms/ExpenseTracker.WinForms.csproj`
- Inspect the form load logic and data refresh code
- Verify DataGridView settings such as AutoGenerateColumns, scroll behavior, and row ordering

## Common failure patterns

- App starts but cannot connect to SQL Server
- Grid shows entries in the wrong order
- Grid visually misbehaves due to column sizing or row header settings
- A stale build lock prevents rebuilds
- Local runtime files or DB artifacts are mistaken for repo-managed data

## Repo-specific guidance

- This repo prefers Docker-first database setup
- Keep form logic and data logic easy to reason about
- Prefer small, local fixes over broad refactors when debugging
- When the UI issue is spacing or layout, check DataGridView column widths and scrolling configuration before changing business logic
- When the issue is data ordering, check the SQL ORDER BY and the load method together

## Good questions for this skill

- Why does the app not load records?
- Why is the data in the wrong order?
- Why is the grid layout broken?
- Why does the app fail to build or run?
- Why is the database not available even though the app is configured?

## Example answer pattern

"This looks like a WinForms + database issue, not a code-generation issue. First verify the SQL container is running, then check whether the app is hitting the correct connection string and whether the query is ordering records as expected. If the problem is purely UI, inspect the DataGridView settings before changing business logic."

## Rule of thumb

When debugging a WinForms app, separate the problem into:

- app startup / connection
- DB schema readiness
- data retrieval logic
- UI rendering and grid behavior

Fix one layer at a time and validate after each change.
