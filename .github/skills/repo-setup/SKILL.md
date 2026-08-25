---
name: repo-setup
description: Guide project setup, local environment bootstrapping, and repo hygiene for this WinForms + Docker project.
---

# Repo Setup

Use this skill when the user needs help setting up the project from scratch, verifying prerequisites, or understanding the expected workflow for this repo.

## Project goal

This repo is a small WinForms app backed by SQL Server in Docker. The intended setup flow is intentionally simple:

1. Install prerequisites
2. Start Docker
3. Run the setup script or Docker Compose
4. Build or run the app
5. Keep DB and generated files out of source control

## Core workflow

- Docker Desktop or equivalent must be running
- Use the repo setup script as the entry point when available
- Recreate the database from schema files rather than committing local DB state
- Keep generated build outputs and local runtime artifacts out of the repo

## Typical commands

Use the smallest valid setup path:

- PowerShell: `./scripts/setup/setup-all.ps1`
- Or manual Docker workflow: `docker compose up -d`
- Build the app: `dotnet build src/ExpenseTracker.WinForms/ExpenseTracker.WinForms.csproj`

## Repo conventions

- Prefer source + scripts + schema over generated DB binaries
- Do not commit local runtime files, build artifacts, or environment secrets
- Keep README and setup docs short, practical, and current
- Prefer Docker-first setup when the DB depends on a containerized service

## Good questions for this skill

- How do I run this project on a new machine?
- What setup script should I use?
- Why do I need Docker?
- What files should stay in Git vs not be committed?
- What is the normal startup flow?

## Example answer pattern

"This repo expects Docker for SQL Server. The app itself is a WinForms project, but the database is created from schema/setup files. On a new machine, install Docker, start it, run the setup script or docker compose, then build/run the app. Keep local DB state and build outputs out of source control."

## Red flags

- User tries to run the app without Docker when the DB is containerized
- User expects local DB binaries to be committed
- User adds generated build outputs into the repo
- User treats setup scripts as weekly run commands instead of bootstrap commands

## Remember

This is a learning repo as well as an app repo. Keep the setup flow easy to explain and easy to reproduce.
