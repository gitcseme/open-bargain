# Decisions

Answers to open questions in the PRD. These override the PRD where they differ. Each entry is **Confirmed** (agreed with the product owner) or **Proposed** (a default, open to change before its task starts).

Add new entries at the bottom: date, decision, short reason.

## Stack and structure

1. **Stack.** Confirmed (2026-10-30). .NET 10 ASP.NET Core + EF Core + PostgreSQL; React + TypeScript + Vite + Tailwind CSS + shadcn/ui; SignalR; GitHub + GitHub Actions; Azure.
2. **Modular monolith with clean architecture.** Confirmed (2026-10-30). Details are in `architecture.md`.
   - **Module boundaries:**
     - Each module is its own project (all internal) plus a `.Contracts` project holding its public API.
     - Modules reference only each other's Contracts and call each other in-process.
     - Each module has its own DbContext/schema.
     - A single request shares one connection and transaction across modules.
   - **Clean architecture:** inside a module, the layers are Domain, Application, Infrastructure and Endpoints, with dependencies pointing inward. Architecture tests enforce the rules.
   - *Reason:* the compiler and tests enforce the boundaries, and there are no network calls between modules.
3. **Simple first.** Confirmed (2026-10-30). Add infrastructure (Azure SignalR Service, job libraries, Bicep, etc.) only when a real need shows up.
4. **UI.** Confirmed (2026-10-30). Designed for desktop web first; fully usable on mobile widths.

## Bargaining

5. **Stock hold.** Confirmed (2026-10-30). An offer holds its requested quantity for **5 minutes**. Stock is permanently reduced only when a deal is confirmed.
6. **Counter-offers.** Confirmed (2026-10-30). The seller may counter with a different quantity at the same price.
   - The counter may be fewer or more units; more only if it fits in held + available.
   - A counter restarts the 5-minute hold so the buyer has time to answer.
   - The buyer accepts (deal confirmed) or declines.
7. **One open offer per buyer per room.** Confirmed (2026-10-30). *Reason:* stops one buyer from locking up all the stock with several holds.
8. **Concurrency.** Confirmed (2026-10-30). Every stock change takes a PostgreSQL advisory lock for the room (`pg_advisory_xact_lock`) inside one transaction.
9. **Price floor visible.** Confirmed (2026-10-30). The floor (highest confirmed deal price) is shown to everyone in the room. The seller's minimum price stays hidden.
10. **Offers below a new floor.** Confirmed (2026-10-30). An open offer that falls below a newly raised floor expires and its hold is released.
11. **Offer below the hidden minimum.** Proposed. The buyer sees a generic "Offer too low" message and the hidden value is not shown. Rate limits slow down anyone probing for the minimum.

## Identity

12. **Buyer identity.** Confirmed (2026-10-30). Buyers appear under a per-room pseudonym by default and can choose to show their real display name in a room. Contact details are never shown.

## Accounts and admin

13. **Login with JWT.** Confirmed (2026-10-30).
    - ASP.NET Core Identity handles users and password hashing (email + password).
    - Login issues a JWT access token (15 min, kept in memory, sent as `Authorization: Bearer`) and a refresh token (7 days, httpOnly secure cookie, stored hashed, rotated on every use).
    - SignalR receives the token through the `access_token` query parameter.
    - Suspending a user revokes their refresh tokens.
    - Email verification is added when sending email exists (M7).
14. **Admin.** Proposed. An admin-only section inside the same app, protected by an `Admin` role.

## Delivery

15. **Branches and CI.** Confirmed (2026-10-31). Feature branches merge into `develop` through a pull request; `develop` is merged into `main` for releases. CI (`.github/workflows/ci.yml`) runs on every pull request to `develop` or `main` and on pushes to them. Branch protection on `develop` requires the "Backend build and test" check to pass before merging.
