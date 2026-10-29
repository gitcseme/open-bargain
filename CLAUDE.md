# Open Bargain

A marketplace built around scheduled, live bargain rooms. A seller opens a room for a product. Buyers join, ask public questions and make visible offers. The seller accepts deals with several buyers, up to the available stock.

**Source of truth:** `docs/open-bargain-mvp.md` (the PRD). If code or other docs disagree with it, the PRD wins unless `docs/decisions.md` says otherwise.

## Docs to read before working
- `docs/open-bargain-mvp.md`: what we are building
- `docs/architecture.md`: modules, tables, states, how key flows work
- `docs/decisions.md`: answers to open questions in the PRD
- `docs/tasks/README.md`: task order and status. Work on one task at a time.

## Stack
- Backend: ASP.NET Core (.NET 10), EF Core, PostgreSQL
- Frontend: React + TypeScript, Vite, Tailwind CSS + shadcn/ui
- Live updates: SignalR (inside the same app)
- Tests: xUnit (backend), Vitest (frontend)
- Repo/CI/CD: GitHub + GitHub Actions
- Hosting: Azure App Service (API + built frontend), Azure Database for PostgreSQL, Azure Blob Storage, Azure Communication Services (email)

## Layout
Modular monolith: one deployable app. Each module is a folder with its own endpoints, data and logic. Modules call each other through plain service interfaces, never through each other's tables.

```
src/
  OpenBargain.Api/
    Modules/
      Accounts/  Rooms/  Bargaining/  QA/  Notifications/  Admin/
  web/                      # React + Vite
tests/
  OpenBargain.Api.Tests/
.github/workflows/
```

## Commands
_To be filled in by task M0 (walking skeleton)._

## How we work
1. **Simple first.** Build the simplest thing that works. Don't add libraries, layers, patterns or infrastructure until a real need shows up, and say so when you think one has.
2. **One task at a time** from `docs/tasks/`. The loop for each task:
   1. Plan it.
   2. The user approves.
   3. Implement it with tests.
   4. Run build + tests.
   5. Self-review.
   6. The user reviews the diff.
   7. Commit.
3. Keep diffs small and focused on the task. Update the task's status in `docs/tasks/README.md`.
4. When the PRD is unclear, ask, then record the answer in `docs/decisions.md`.
5. UI is designed for desktop web first and must be fully usable on mobile widths.

## Rules that must never break
- The server decides room state, timestamps and event order. Clients only display them.
- Never expose buyer contact details. Buyers appear under pseudonyms; the seller is clearly identified.
- Buyer is by default pseudinums, they can decide their identity reveiling.
- Private rooms never appear in discovery, search or public share previews. Only invitees can see them.
- Stock can never be over-allocated, even when two accepts happen at the same time.
- After a price is accepted in a room, no later deal in that room can be below that per-unit price.
- A deal is an agreement, not a payment or delivery. The UI must say so.
- Every room, offer, deal and moderation state change is recorded in the room's event history.
