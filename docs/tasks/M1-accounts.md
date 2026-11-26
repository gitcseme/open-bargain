# M1 — Accounts

**Goal:** users can sign up, log in with JWT, stay logged in through rotating refresh tokens, and edit a basic profile. M1 also sets the patterns later modules follow: module projects, a `DbContext` per schema, migrations, and architecture tests.

**Not in scope:**
- Email verification and password reset. They need email, so they come in M7.
- Avatar upload. M1 stores an optional `AvatarUrl` and shows initials; upload arrives in M2 together with Blob Storage.
- Admin role management and suspending users (M9). The fields exist, and login already refuses suspended users.
- Per-room pseudonyms (M3).

---

## M1-1 Accounts module skeleton, EF Core, architecture tests
**Goal:** an empty Accounts module with its own schema and migrations, wired into the host, with boundary rules enforced.

Scope:
- `src/Modules/Accounts/OpenBargain.Modules.Accounts` (Domain/ Application/ Infrastructure/ Endpoints/, all internal) and `OpenBargain.Modules.Accounts.Contracts`
- Public entry point `AccountsModule`: `AddAccountsModule`, `MapAccountsEndpoints`, `MigrateAccountsDatabaseAsync`
- `AccountsDbContext` on ASP.NET Core Identity (`IdentityUserContext`), schema `accounts`:
  - users table `accounts."Users"` with `DisplayName`, `AvatarUrl`, `IsSuspended`, `Role`, `CreatedAt`
  - Identity's unused claims/logins/tokens tables removed from the model
- First migration. The app applies pending migrations at startup.
- `tests/OpenBargain.ArchitectureTests` (NetArchTest) for the boundary rules

Done when:
- [ ] Build and tests pass; the app creates the `accounts` tables on startup
- [ ] Architecture tests pass, and fail on a deliberate violation

## M1-2 Sign-up, login, JWT
**Goal:** a user can register and log in, and protected endpoints accept the access token.

Scope:
- `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/me`
- Access token: 15 min, claims user id, display name, role. Signing key from config (`Jwt__SigningKey` in Azure).
- Lockout after repeated failures. Suspended or locked users get the same generic error.

Done when:
- [ ] Integration tests cover register → login → `/api/me`, wrong password, duplicate email, suspended user

## M1-3 Refresh tokens and logout
**Goal:** sessions survive a page reload and last 7 days, with rotation.

Scope:
- `RefreshTokens` table. Tokens are stored hashed. Cookie `ob_refresh`: httpOnly, Secure, SameSite=Strict, path `/api/auth`.
- `POST /api/auth/refresh` rotates the token. Reusing a revoked token revokes all of the user's tokens.
- `POST /api/auth/logout`

Done when:
- [ ] Integration tests cover rotation, reuse detection, logout, expiry, suspended user

## M1-4 Profile API
**Goal:** a user can view and edit their basic profile.

Scope:
- `PUT /api/me` updates the display name. Avatar is read-only until M2.

Done when:
- [ ] Integration tests cover update, validation error, 401 without a token

## M1-5 Frontend: auth
**Goal:** sign up, log in, stay logged in across reloads, log out.

Scope:
- react-router. Sign-up and login pages. Header with a user menu.
- Access token kept in memory only. Session restored through refresh on load. On 401: refresh once, then retry.

Done when:
- [ ] Vitest covers login, error, session restore, refresh-and-retry, route guard
- [ ] Usable at mobile widths

## M1-6 Frontend: profile page + deploy config
**Goal:** a user can edit their display name, and production has its signing key.

Scope:
- `/profile` page
- `docs/deploy.md`: add the `Jwt__SigningKey` App Service setting

Done when:
- [ ] Sign up → reload → edit profile → log out → log in works locally and on Azure

## Finally
- Update `architecture.md` / `CLAUDE.md` where something real changed
- Mark M1 done in `docs/tasks/README.md`
