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

15. **Branches and CI.** Confirmed (2026-10-31). Feature branches merge into `develop` through a pull request; `develop` is merged into `main` for releases. CI (`.github/workflows/ci.yml`) runs on every pull request to `develop` or `main` and on pushes to them. A ruleset on `develop` and `main` requires the "Backend build and test" and "Frontend lint, test and build" checks to pass before merging.

## M1

16. **Avatar upload waits for M2.** Confirmed (2026-11-21). M1 stores an optional `AvatarUrl` and the UI shows initials. Upload arrives in M2, when Blob Storage is added for room media. *Reason:* no new infrastructure just for avatars.
17. **Migrations run at app startup.** Confirmed (2026-11-21). Each module applies its pending EF Core migrations when the app starts (`Database:MigrateOnStartup`, default on). *Reason:* simplest with one App Service instance; revisit if we scale out.
18. **Identity tables.** Confirmed (2026-11-21). ASP.NET Core Identity's user table only, renamed to `accounts."Users"`. The role tables (role is a column) and the unused claims/logins/tokens tables are left out of the model. *Reason:* keep `UserManager` (hashing, lockout, later email tokens) without empty tables.
19. **Clock.** Confirmed (2026-11-21). Use .NET's built-in `TimeProvider` (and `FakeTimeProvider` in tests) instead of a custom `IClock`.

## M1 Accounts (planning session, 2026-11-22)

20. **JWT signing.** Confirmed (2026-11-22). HS256 with `Jwt:SigningKey` (at least 32 bytes; App Service setting `Jwt__SigningKey`). Issuer and audience `open-bargain`. The app refuses to start without a valid key.
21. **JWT layout.** Confirmed (2026-11-22). Accounts issues tokens; the host validates them. Claims `sub`, `name`, `role` (plus `jti`, `iat`, `exp`); inbound claim mapping off.
22. **Register.** Confirmed (2026-11-22). Creates the user only (201 + user). No token or cookie; the web app logs in straight after with the same credentials.
23. **Input rules.** Confirmed (2026-11-22). Password 8–128 characters, no composition rules. Email valid, at most 256, unique. Display name trimmed, 2–40, required, unique ignoring case (a `NormalizedDisplayName` column with a unique index).
24. **Errors.** Confirmed (2026-11-22). RFC 9457 ProblemDetails. Business errors carry a stable `code` (`email_taken`, `display_name_taken`, `invalid_credentials`, `invalid_refresh_token`, `rate_limited`). Validation uses FluentValidation validators in Application, run by a per-endpoint `ValidationFilter<T>`.
25. **Lockout.** Confirmed (2026-11-22). 5 failed logins lock the account for 15 minutes. Every login failure (unknown email, locked, wrong password, suspended) returns the same 401. *Reason:* doesn't reveal which accounts exist.
26. **Rate limiting.** Confirmed (2026-11-22). Built-in limiter, 10 requests per minute per IP on register, login and refresh → 429.
27. **Suspension window.** Confirmed (2026-11-22). Access tokens aren't checked against the suspended flag on each request; up to 15 minutes is accepted. `/api/me`, login and refresh refuse suspended users.
28. **Sessions.** Confirmed (2026-11-22). Refresh tokens slide: each rotation gives a new 7-day expiry. Reusing a revoked token revokes all of the user's tokens. The web app serializes refreshes across tabs with Web Locks. *Reason:* strict reuse detection would otherwise log out users with several tabs open.
29. **Profile update.** Confirmed (2026-11-22). `PUT /api/me { displayName }` only. Email, password and avatar can't change in M1.
30. **Frontend libraries.** Confirmed (2026-11-22). react-router v7 (declarative), Redux Toolkit + RTK Query, Formik + Yup, shadcn/ui. The access token lives only in memory (Redux).
31. **Tests.** Confirmed (2026-11-22). Backend tests drive time with `FakeTimeProvider`; frontend tests mock the API with MSW.
32. **Working with an implementation agent.** Confirmed (2026-11-22). Each milestone starts with a planning session where every decision is made and written to `docs/handovers/agent-handover-<Mn>.md`. The agent then implements all tasks without stopping, one commit per task, and logs any gap it fills in the handover. *Reason:* fewer round trips; the user reviews all commits at the end.
