# M0 — Walking skeleton

**Goal:** prove the whole pipeline works end to end before building any features. An empty app has its backend, frontend and database connected. It is built and tested in GitHub Actions and running on Azure.

**Not in scope:** any product feature, auth, or modules. Module projects and architecture tests arrive with the first module (M1).

---

## M0-1 Backend solution
**Goal:** a .NET 10 solution that builds and tests from one command.

Scope:
- `.gitattributes`: normalize line endings to LF
- `global.json`: pin .NET SDK 10
- `Directory.Build.props`: nullable on, warnings as errors, implicit usings
- `OpenBargain.slnx` with:
  - `src/OpenBargain.Api`: minimal API host, `GET /api/health` returns `{ "status": "ok" }`
  - `src/OpenBargain.SharedKernel`: empty for now; the host references it
  - `tests/OpenBargain.UnitTests` (xUnit): one placeholder test
  - `tests/OpenBargain.IntegrationTests` (xUnit + `WebApplicationFactory`): a test that calls `/api/health`

Done when:
- [ ] `dotnet build` and `dotnet test` pass from the repo root
- [ ] `dotnet run --project src/OpenBargain.Api` serves `/api/health`

## M0-2 PostgreSQL connection
**Goal:** the app talks to PostgreSQL locally and in tests.

Scope:
- `docker-compose.yml` with PostgreSQL for local development
- Connection string in `appsettings.Development.json`. It holds local dev credentials only; real secrets never go in the repo.
- Register an `NpgsqlDataSource` in the host. `/api/health` also runs `SELECT 1` and reports `database: ok | unavailable`. No EF Core yet; it arrives with the first module that has tables.
- Integration tests start a real PostgreSQL in Docker (Testcontainers) and point the app at it.

Done when:
- [ ] `docker compose up -d` and then `dotnet run` show `database: ok`
- [ ] The integration test passes against the Testcontainers database

## M0-3 Frontend
**Goal:** a React app, styled with Tailwind + shadcn/ui, that shows the backend health.

Scope:
- `src/web`: Vite + React + TypeScript
- Tailwind CSS + shadcn/ui set up, with one shadcn component (`Card`)
- Home page that shows "Open Bargain" and the result of `/api/health`; responsive from mobile to desktop
- In development, the Vite dev server forwards `/api` to the backend
- In production, the backend serves the built files and falls back to `index.html` for client routes
- Vitest with one component test; ESLint

Done when:
- [ ] `npm run dev` (with the API running) shows API and database status
- [ ] `npm run lint`, `npm test` and `npm run build` pass

## M0-4 CI (GitHub Actions)
**Goal:** every pull request and every push to `main` is built and tested automatically.

Scope:
- `.github/workflows/ci.yml`:
  - backend: restore, build, test (Testcontainers runs on the GitHub runner's Docker)
  - frontend: `npm ci`, lint, test, build

Done when:
- [ ] The workflow passes on the M0 pull request

## M0-5 Deploy to Azure
**Goal:** `main` is deployed to Azure automatically, and `/api/health` works there.

Scope:
- **The user creates in the Azure portal** (step-by-step guide in `docs/deploy.md`):
  - Resource group
  - App Service plan + Web App (Linux, .NET 10)
  - Azure Database for PostgreSQL Flexible Server (smallest tier)
  - Web App setting `ConnectionStrings__Default`
  - The publish profile, added as a GitHub secret
- `.github/workflows/deploy.yml`, on push to `main`:
  1. build the frontend
  2. publish the API with the frontend files included
  3. deploy to the Web App

Done when:
- [ ] After merging to `main`, the Azure URL shows the home page with `API: ok` and `database: ok`

## Finally
- Fill in the **Commands** section of `CLAUDE.md`
- Mark M0 done in `docs/tasks/README.md`
