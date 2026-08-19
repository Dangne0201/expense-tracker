# Copilot Instructions

## Project goal
- This repo is a learning project for using AI coding agents in a real software workflow.
- The primary goal is to learn how to build, review, setup, and maintain a small WinForms + SQL Server app.
- Keep the project usable for new developer setup and easy to share in GitHub.

## Core principles
- Prefer minimal, surgical changes.
- Do not add unrelated refactors or cleanup unless directly required by the task.
- Keep repo review-friendly: no generated build output, no database binaries, no .env secrets, no local logs or temp files.
- Prefer Docker-first setup for SQL Server.
- Use data/init.sql as the source of truth for schema creation.
- Keep the app buildable and runnable with the smallest validation command that checks the changed behavior.

## Repository conventions
- Code lives in src/ExpenseTracker.WinForms.
- Database setup lives in docker-compose.yml and data/init.sql.
- New machine setup uses setup-all.ps1.
- Keep README.md and SETUP_ALL_STEPS.md short, practical, and easy to follow.
- Do not commit large binaries (.mdf/.ldf/.ndf), build outputs (bin/obj), or local environment files.

## Working style
- Start by reading the relevant file(s) before editing.
- Keep comments only where they clarify non-obvious logic.
- Prefer clear names and straightforward logic over clever tricks.
- Explain connection and startup behavior clearly because this project intentionally includes Docker + LocalDB fallback logic.
- If a fix is about build/setup environment, validate using the smallest relevant command (for example dotnet build or the setup script).

## Development workflow
- For setup or database tasks, prefer Docker and PowerShell scripts.
- For app logic tasks, prefer WinForms code changes with direct validation by building the project.
- For repo hygiene tasks, remove duplicate docs, generated files, and local artifacts.
- Keep review comments specific, actionable, and focused on correctness.

## Communication style
- Write in a direct, practical Vietnamese/English mix when needed.
- Focus on what changed, why it changed, and how to validate it.
- Keep instructions short and clear; do not over-explain basic tasks.
- When asked to review code, prefer concrete issues over style-only comments.

## File organization policy for agents
- Before creating or moving files, check whether the repository provides an organization manifest (e.g., .file-catalog.json at repo root). If present, follow it (place files into the matching category/folder).
- If there is no manifest or no matching category, do NOT autonomously scatter files in the repo. Instead produce a structured proposal containing:
  - proposed destination(s),
  - rationale (matching by extension or similar patterns),
  - a dry-run listing of the actions to be taken,
  - and a manifest diff if you intend to add a category.
- Require explicit human approval before creating new top-level categories or updating repository-wide manifests. Use auto-create only when a maintainer has opt-ed in.
- Prefer returning machine-readable proposals (JSON) so maintainers or automation can accept or reject programmatically. Example response shape:
  {
    "actions": [ {"from":"src/newfile.ext","to":"docs/newfile.ext","reason":"*.md"} ],
    "manifestDiff": { /* optional */ },
    "dryRun": true
  }
- If the repo does later add a manifest, consult it on subsequent edits and avoid duplicating categories.

Agents: put organization proposals and dry-run outputs into the PR description or into the review comment so reviewers can see what changed and why.
