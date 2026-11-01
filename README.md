# Open Bargain

[![CI](https://github.com/gitcseme/open-bargain/actions/workflows/ci.yml/badge.svg?branch=develop)](https://github.com/gitcseme/open-bargain/actions/workflows/ci.yml?query=branch%3Adevelop) [![Deploy](https://github.com/gitcseme/open-bargain/actions/workflows/deploy.yml/badge.svg)](https://github.com/gitcseme/open-bargain/actions/workflows/deploy.yml)

A marketplace built around scheduled, live bargain rooms. A seller opens a room for a product. Buyers join, ask public questions and make visible offers. The seller accepts deals with several buyers, up to the available stock. A deal is an agreement; Open Bargain doesn't handle payment or delivery.

**Live:** https://open-bargain-efb0gkdfczesg4dw.southeastasia-01.azurewebsites.net

This project is built by a developer working with an AI coding agent ([Claude Code](https://claude.com/claude-code)). The rest of this README describes how that works.

## The agentic workflow

### Who does what

| The developer | The agent |
|---|---|
| Decides what to build and approves each plan | Plans each task against the docs |
| Answers open product questions | Asks when the PRD is unclear, and records the answer |
| Reviews every diff before it is committed | Writes the code and tests, runs build + tests, reviews its own diff |
| Merges pull requests and releases to `main` | Commits, pushes and opens pull requests when asked |
| Handles secrets and cloud accounts (Azure portal, passwords, publish profile) | Writes step-by-step guides and the CI/CD workflows; never sees secret values |

### The docs are the agent's memory

The agent starts every session with no memory of earlier ones. Everything it needs to work consistently lives in the repo:

| File | Role |
|---|---|
| [CLAUDE.md](CLAUDE.md) | The agent's guide: stack, layout, commands, how we work, and the **rules that must never break** (e.g. stock is never over-allocated, buyer contact details are never exposed). Loaded automatically in every session. |
| [docs/open-bargain-mvp.md](docs/open-bargain-mvp.md) | The PRD: **the source of truth** for what we build |
| [docs/decisions.md](docs/decisions.md) | Answers to open questions. Overrides the PRD where they disagree. |
| [docs/architecture.md](docs/architecture.md) | Modules, tables, states and key flows |
| [docs/tasks/](docs/tasks/README.md) | Features M0–M10, split into small tasks, with their status |
| [docs/deploy.md](docs/deploy.md) | How the Azure resources were set up |

When code and docs disagree, the PRD wins unless `decisions.md` says otherwise. When a question comes up that the docs don't answer, the agent asks, and the answer goes into `decisions.md` so it's never asked again.

### One task at a time

Features are broken into small tasks just before work on them starts. Each task goes through the same loop:

1. **Plan:** the agent reads the task and the docs and proposes a plan.
2. **Approve:** the developer approves or changes it.
3. **Implement** with tests.
4. **Build + tests** must pass locally.
5. **Self-review:** the agent reviews its own diff.
6. **Developer review** of the diff.
7. **Commit**, and update the task's status in `docs/tasks/README.md`.

Diffs stay small and focused on one task. "Simple first": no new library, layer or pattern until a real need shows up, and the agent says when it thinks one has.

### Guardrails

The agent's work is checked by more than its own review:

- **CI** (lint, tests, build for backend and frontend) runs on every pull request. A ruleset on `develop` and `main` requires it to pass, requires a pull request, and blocks force-pushes.
- **Human review** of every diff and every pull request.
- **Architecture tests** (planned, `OpenBargain.ArchitectureTests`) will enforce the module boundaries described in `architecture.md` once the first modules exist.
- **Secrets stay out of the agent's hands**: connection strings and deploy credentials are entered by the developer directly in Azure and GitHub.

### Branches and releases

```
feature branch ──PR + CI──▶ develop ──PR + CI──▶ main ──▶ deploy to Azure
```

- Each task gets a branch from `develop` and a pull request back into it.
- A pull request from `develop` into `main` deploys to Azure when merged. A deploy can also be started by hand from `main` (**Actions → Deploy → Run workflow**).

### Working on a task with the agent

Open Claude Code at the repo root and name the task, for example *"Plan task M1-1"*. The agent reads `CLAUDE.md`, the task and the related docs, and proposes a plan. Nothing is implemented until you approve it, and nothing is committed until you've reviewed the diff.

## Tech stack

| Area | Technology |
|---|---|
| **Backend** | ASP.NET Core (.NET 10), EF Core; planned: SignalR (live updates), ASP.NET Core Identity + JWT |
| **Frontend** | React + TypeScript, Vite, Tailwind CSS, shadcn/ui |
| **Database** | PostgreSQL (Docker locally, Azure Database for PostgreSQL in the cloud) |
| **Testing** | xUnit (backend), Testcontainers (integration tests against a real PostgreSQL in Docker), Vitest + Testing Library (frontend) |
| **Cloud** | Azure App Service (API + frontend), Azure Database for PostgreSQL; planned: Azure Blob Storage (media), Azure Communication Services (email) |
| **CI/CD** | GitHub Actions: CI on every pull request, deploy to Azure on merge into `main` |
| **AI agent** | Claude Code |

The backend is a modular monolith with clean architecture inside each module ([docs/architecture.md](docs/architecture.md)).

## Running locally

You need the .NET 10 SDK, Node.js 24 and Docker.

```bash
docker compose up -d                              # PostgreSQL on port 15432
cd src/web && npm install && npm run build && cd ../..
dotnet run --project src/OpenBargain.Api          # http://localhost:5142
```

- Health check: http://localhost:5142/api/health
- Frontend with live reload: `npm run dev` in `src/web` (with the API running), then http://localhost:5173
- Tests: `dotnet test OpenBargain.slnx` (needs Docker), and `npm run lint` + `npm test` in `src/web`
