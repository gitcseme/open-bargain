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
- Auth: ASP.NET Core Identity for users/passwords. JWT access token (15 min, sent as Bearer) + rotating refresh token (7 days, httpOnly cookie)
- Live updates: SignalR (inside the same app)
- Tests: xUnit (backend), Vitest (frontend)
- Repo/CI/CD: GitHub + GitHub Actions
- Hosting: Azure App Service (API + built frontend), Azure Database for PostgreSQL, Azure Blob Storage, Azure Communication Services (email)

## Layout
**Modular monolith with clean architecture inside each module.** Details are in `docs/architecture.md`.

```
src/
  OpenBargain.Api/                          # host only: startup, JWT auth, SignalR hub, serves web build
  OpenBargain.SharedKernel/
  Modules/<Module>/
    OpenBargain.Modules.<Module>/           # Domain/ Application/ Infrastructure/ Endpoints/ (all internal)
    OpenBargain.Modules.<Module>.Contracts/ # public API: I<Module>Api + request/response types
  web/                                      # React + Vite
tests/
  OpenBargain.UnitTests/  OpenBargain.IntegrationTests/  OpenBargain.ArchitectureTests/
.github/workflows/
```

Modules: Accounts, Rooms, Bargaining, QA, Notifications, Admin. They are created when their first task starts.

Boundary rules (checked by `OpenBargain.ArchitectureTests`):
- **Between modules:**
  - A module references only other modules' `.Contracts` projects.
  - Calls go through `I<Module>Api`, in-process.
  - A module never touches another module's tables or internal classes.
  - Each module has its own `DbContext` and PostgreSQL schema.
- **Inside a module:**
  - Endpoints → Application → Domain. Infrastructure implements Application's interfaces.
  - Domain has no outside dependencies.
- **Keep it simple:** plain use-case classes and constructor injection. No MediatR, no generic repositories, no CQRS frameworks unless a real need shows up.

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
- Never expose buyer contact details. The seller is clearly identified.
- Buyers appear under a pseudonym by default and can choose to show their real display name in a room.
- Private rooms never appear in discovery, search or public share previews. Only invitees can see them.
- Stock can never be over-allocated. Sold + held units never exceed total, even under simultaneous requests. Every stock change takes the room's PostgreSQL advisory lock inside one transaction.
- An offer holds its quantity for a limited time. Stock is permanently reduced only when the deal is confirmed: either the seller accepts, or the buyer accepts the seller's counter-quantity.
- After a deal at a per-unit price, no later deal in that room can be below that price (the price floor). The floor is visible to everyone; the seller's minimum price stays hidden.
- A deal is an agreement, not a payment or delivery. The UI must say so.
- Every room, offer, deal and moderation state change is recorded in the room's event history.
