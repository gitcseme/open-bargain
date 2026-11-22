# Agent handover — M1 Accounts (tasks M1-2 … M1-6)

**Status:** ready for implementation · **Decided:** 2026-11-22 (planning session with the product owner)

This document holds every decision needed to finish M1. The implementation agent works through it **without asking or waiting**. Read first: `CLAUDE.md`, `docs/architecture.md`, `docs/decisions.md`, `docs/tasks/M1-accounts.md`. Where this document is more specific than those, follow this document. The PRD's "must never break" rules always win.

---

## 1. How to work

- Branch: `m1-accounts` (already checked out). M1-1 is done.
- Do tasks **in order**: M1-2 → M1-3 → M1-4 → M1-5 → M1-6 → Finally.
- For each task:
  1. Implement it, with the tests listed under the task.
  2. All green: `dotnet build OpenBargain.slnx`, `dotnet test OpenBargain.slnx`, and in `src/web`: `npm run lint`, `npm test`, `npm run build`.
  3. Self-review the diff against this document and the CLAUDE.md rules.
  4. Set the task to `done` in `docs/tasks/README.md`.
  5. One commit: `M1-n: <short summary>` (use the `git-commit` skill format). **Don't push or open a PR.**
- **Gaps:** if something isn't covered here, take the simplest option that fits the PRD and this document. Record it in §9 *Agent log* (what, why) and carry on.
- **Stop only if:** build or tests can't be made green, or a choice would break a CLAUDE.md "must never break" rule. Explain why in §9.
- The user reviews all commits at the end, then pushes and opens the PR.
- Keep it simple: no packages beyond §3, no MediatR, no generic repositories.

---

## 2. Decision summary

| # | Topic | Decision |
|---|---|---|
| Q1 | JWT signing | HS256, key from config `Jwt:SigningKey` (≥ 32 bytes), `Issuer` = `Audience` = `open-bargain`. Fail at startup if missing or short. |
| Q2 | JWT layout | Accounts **issues** tokens; the host **validates** them (`AddJwtBearer`). Claims `sub`, `name`, `role`; `MapInboundClaims = false`. |
| Q3 | Register | Creates the user only → 201 `UserResponse`. **No token, no cookie.** |
| Q4 | Input rules | Password 8–128, no composition rules. Email valid, ≤ 256, unique. Display name trimmed, 2–40, required, **unique**. |
| Q5 | Name uniqueness | Case-insensitive via a `NormalizedDisplayName` column with a unique index. |
| Q6 | Errors | RFC 9457 ProblemDetails, with a stable `code` on business errors. Validation via **FluentValidation**. |
| Q7 | Validator wiring | Validators in Application; run by a **per-endpoint** generic `ValidationFilter<T>`. |
| Q8 | Lockout | 5 failures → 15 min. Every login failure returns the same 401 `invalid_credentials`. |
| Q9 | Duplicate email | 409 `email_taken`. |
| Q10 | Rate limiting | Built-in limiter, policy `auth`, 10/min per IP on register, login and refresh → 429 `rate_limited`. |
| Q11 | Suspension | No per-request check; a window of up to 15 min is accepted. `GET /api/me` returns 401 if the user is suspended. |
| Q12 | API shapes | See §4. |
| Q13 | Session length | Sliding 7 days (each rotation sets a new expiry 7 days ahead). |
| Q14 | Reuse / tabs | Strict reuse detection (revoke **all** the user's tokens). Client serializes refresh with Web Locks. |
| Q15 | Refresh spec | See M1-3. |
| Q16 | Profile update | `PUT /api/me { displayName }` only. |
| Q17 | Frontend libs | react-router v7 (declarative), **Redux Toolkit + RTK Query**, **Formik + Yup**, shadcn/ui. |
| Q18 | Sign-up UX | After register, the frontend logs in automatically with the same credentials. |
| Q19 | Routes / layout | See M1-5. |
| Q20 | M1-6 split | The agent builds the page and docs and verifies locally. **The user** does the Azure steps (§8). |
| Q21 | Tests | Backend `FakeTimeProvider`; frontend **MSW**. |
| Q22 | Agent flow | §1. |
| Q23 | Process docs | Update the work loop in CLAUDE.md and the tasks README, and add the decisions to decisions.md (see "Finally"). |

---

## 3. New dependencies (only these)

**Backend**
- `OpenBargain.Api`: `Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.x)
- `OpenBargain.Modules.Accounts`: `FluentValidation.DependencyInjectionExtensions` (brings in `FluentValidation`); token creation uses `Microsoft.IdentityModel.JsonWebTokens`, which comes with JwtBearer or can be added directly if needed
- `OpenBargain.SharedKernel`: `FluentValidation` + `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, for the endpoint filter and claims helper
- `OpenBargain.IntegrationTests`: `Microsoft.Extensions.TimeProvider.Testing`

**Frontend** (`src/web`)
- dependencies: `react-router`, `@reduxjs/toolkit`, `react-redux`, `formik`, `yup`
- devDependencies: `msw`
- shadcn components (via the `shadcn` CLI): `button`, `input`, `label`, `card`, `dropdown-menu`, `avatar`

Use current stable versions. Pin them as the repo already does (caret ranges in package.json; exact versions in csproj).

---

## 4. Shared conventions

### Config
```jsonc
// appsettings.json (no secret here)
"Jwt": { "Issuer": "open-bargain", "Audience": "open-bargain", "AccessTokenMinutes": 15, "RefreshTokenDays": 7 },
"RateLimiting": { "Auth": { "PermitLimit": 10, "WindowSeconds": 60 } }
// appsettings.Development.json
"Jwt": { "SigningKey": "<a dev-only random value of at least 32 bytes>" }
```
- Azure: App Service setting `Jwt__SigningKey`.
- On startup the host checks that `Jwt:SigningKey` exists and is at least 32 bytes (UTF-8); otherwise it throws with a clear message.

### Auth claims and helper
- Access token claims: `sub` = user id (Guid), `name` = display name, `role` = `User` | `Admin`, plus `jti`, `iat`, `exp`.
- Host: `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` with `MapInboundClaims = false`, `NameClaimType = "name"`, `RoleClaimType = "role"`, a 30 s clock skew, and the issuer, audience, lifetime and key all validated. `AddAuthorization()`. `UseAuthentication()` and `UseAuthorization()` come after `UseForwardedHeaders` and `UseRateLimiter`.
- SharedKernel: `ClaimsPrincipalExtensions.GetUserId(this ClaimsPrincipal)` → `Guid`, read from `sub`.
- The SignalR `access_token` query parameter is **not** part of M1 (comes in M6).

### Errors (ProblemDetails)
- `builder.Services.AddProblemDetails()`, `app.UseExceptionHandler()` and `app.UseStatusCodePages()`. 500 responses carry no stack trace.
- Validation: 400 `ValidationProblem` with `errors: { "<camelCaseField>": ["message"] }`.
- Business errors: `Results.Problem(statusCode: ..., title: ..., extensions: { ["code"] = "<code>" })`.

| Code | Status | When |
|---|---|---|
| `email_taken` | 409 | register, email exists |
| `display_name_taken` | 409 | register / PUT /api/me, name clashes (case-insensitive) |
| `invalid_credentials` | 401 | login failed for any reason |
| `invalid_refresh_token` | 401 | refresh failed for any reason |
| `rate_limited` | 429 | auth rate limit hit |

### Validation (FluentValidation)
- Validators are `internal sealed` classes in `Application/`, next to their request record (e.g. `RegisterRequestValidator`).
- The module registers them with `services.AddValidatorsFromAssembly(typeof(AccountsModule).Assembly, includeInternalTypes: true)`.
- SharedKernel `ValidationFilter<T> : IEndpointFilter` finds the `T` argument, resolves `IValidator<T>` and returns `TypedResults.ValidationProblem(...)` on failure. Each endpoint opts in with `.AddEndpointFilter<ValidationFilter<RegisterRequest>>()`.
- Checks that need the database (uniqueness) stay in the use case.
- Rules:
  - `email`: not empty, `EmailAddress()`, max 256
  - `password`: length 8–128, nothing else
  - `displayName`: after trimming, length 2–40

### Rate limiting
- `AddRateLimiter` with fixed-window policy `"auth"`, partitioned by `HttpContext.Connection.RemoteIpAddress`. Limit and window come from `RateLimiting:Auth`. On rejection: 429 ProblemDetails, `code: rate_limited`.
- Applied (`.RequireRateLimiting("auth")`) to register, login and refresh. Logout and `/api/me` are not limited.
- `UseForwardedHeaders` with `XForwardedFor | XForwardedProto`. Clear `KnownNetworks`/`KnownProxies` so App Service's proxy is trusted.

### JSON and times
- camelCase (the minimal-API default). All times are `DateTimeOffset` in UTC, serialized as ISO-8601.
- Use the injected `TimeProvider` everywhere (decision #19); never `DateTime.UtcNow`.

### Module layout (Accounts)
Architecture tests forbid Application from depending on Infrastructure, and `AccountUser` (an Identity type) lives in Infrastructure. So:
- **Domain:** `UserRole` (exists).
- **Application:**
  - Use-case classes: `RegisterUser`, `LoginUser`, `RefreshSession`, `Logout`, `GetMe`, `UpdateProfile`
  - Request/response records and validators
  - Interfaces implemented by Infrastructure:
    - `IUserAccounts`: find by id/email, create, check password with lockout, display-name-taken check, update display name. Works with a small Application record `UserAccount(Id, Email, DisplayName, AvatarUrl, Role, IsSuspended)`.
    - `IRefreshTokenStore`
    - `IAccessTokenIssuer`
  - `DisplayNames.Normalize(string)` = `Trim().ToUpperInvariant()`
- **Infrastructure:** implementations, using `UserManager<AccountUser>` and `AccountsDbContext`, plus `JwtAccessTokenIssuer`, EF configurations and migrations.
- **Endpoints:** `AuthEndpoints` (`/api/auth/*`) and `MeEndpoints` (`/api/me`). Thin: bind → filter → use case → map to `IResult`.
- `AccountsModule.MapAccountsEndpoints(this IEndpointRouteBuilder app)` is the public entry; the host calls it.
- Use cases return a simple result (a success value, or an error code the endpoint maps to ProblemDetails). No exceptions for expected failures.
- **Contracts:** stays empty in M1.

### Identity configuration
`AddIdentityCore<AccountUser>(...)` + `AddEntityFrameworkStores<AccountsDbContext>()`:
- Password: `RequiredLength = 8`, `RequireDigit = RequireLowercase = RequireUppercase = RequireNonAlphanumeric = false`, `RequiredUniqueChars = 1`.
- `User.RequireUniqueEmail = true`. `UserName` = email.
- Lockout: `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15 min`, `AllowedForNewUsers = true`.

---

## 5. M1-2 Sign-up, login, JWT

### Database
- Migration `AddNormalizedDisplayName`: column `NormalizedDisplayName` (varchar 40, not null) with a unique index on `accounts."Users"`. The table is empty in every environment, so no backfill is needed.
- `CreatedAt` is set from `TimeProvider` at registration.

### Endpoints
| Method / path | Auth | Body | Success | Errors |
|---|---|---|---|---|
| `POST /api/auth/register` | — (rate-limited) | `{ email, password, displayName }` | **201** `UserResponse`, `Location: /api/me` | 400 validation · 409 `email_taken` · 409 `display_name_taken` · 429 |
| `POST /api/auth/login` | — (rate-limited) | `{ email, password }` | **200** `AuthResponse` | 400 validation · 401 `invalid_credentials` · 429 |
| `GET /api/me` | Bearer | — | **200** `UserResponse` | 401 (no or invalid token, or user missing or suspended) |

```ts
UserResponse = { id: string, email: string, displayName: string, avatarUrl: string | null, role: "User" | "Admin" }
AuthResponse = { accessToken: string, expiresAt: string /* ISO UTC */, user: UserResponse }
```

### Behaviour
- **Register:**
  - Trim the display name.
  - Check email (409 `email_taken`), then normalized display name (409 `display_name_taken`).
  - Create the user through `UserManager`.
  - If the unique index on `NormalizedDisplayName` fails because of a race (PostgreSQL `23505`), map it to 409 `display_name_taken`. A duplicate-email error from Identity maps to `email_taken`.
- **Login:**
  1. Find the user by email; not found → 401 `invalid_credentials`.
  2. Locked out → 401.
  3. Wrong password → `AccessFailedAsync`, then 401.
  4. Suspended → 401, checked after a correct password.
  5. Success → `ResetAccessFailedCountAsync` and issue the token.

  All failures return exactly the same body.
- Access token lifetime: `Jwt:AccessTokenMinutes` (15). `expiresAt` = now + 15 min.

### Tests (integration, `tests/OpenBargain.IntegrationTests`)
- register → login → `GET /api/me` returns the same user
- register: invalid input → 400 with the field errors (email, password, displayName)
- register: duplicate email → 409 `email_taken`; duplicate display name in another case → 409 `display_name_taken`
- login: wrong password → 401 `invalid_credentials`; unknown email gives the identical body
- login: 5 wrong passwords lock the account, so even the right password then gets 401; after advancing `FakeTimeProvider` 15 min, login works
- login: suspended user (set `IsSuspended` directly through `AccountsDbContext` in the test) → 401 `invalid_credentials`
- `GET /api/me` without a token → 401; with an expired token (advance time 16 min) → 401
- rate limit: an extra factory with `PermitLimit = 2`, where the 3rd login → 429 `rate_limited`
- Unit tests (`OpenBargain.UnitTests`): validators' rules; `DisplayNames.Normalize`

### Test infrastructure
- `ApiFactory` sets `Jwt:SigningKey` (test value), `RateLimiting:Auth:PermitLimit = 1000`, and replaces `TimeProvider` with a singleton `FakeTimeProvider`, exposed as `factory.Time`. Set it to real UTC now at startup, so the containers and JWT validation agree.
- Note: JwtBearer validates lifetime with its own clock. Make it use the injected `TimeProvider`: set `TokenValidationParameters.LifetimeValidator`, or the options' `TimeProvider` if JwtBearer 10 has one. If neither works, put the expiry test at the token-issuer level and note it in §9.
- Each test uses unique emails and display names (e.g. a Guid suffix), so tests can share one container.

---

## 6. M1-3 Refresh tokens and logout

### Database
- Migration `AddRefreshTokens`, table `accounts."RefreshTokens"`:
  - `Id` uuid PK
  - `UserId` uuid FK → `Users.Id`, cascade delete, indexed
  - `TokenHash` varchar(64), unique (lowercase hex SHA-256 of the raw token)
  - `CreatedAt`, `ExpiresAt` timestamptz
  - `RevokedAt` timestamptz null
  - `ReplacedByTokenId` uuid null
- Raw token: 32 bytes from `RandomNumberGenerator`, base64url. Only the hash is stored.
- No cleanup of expired rows in M1.

### Cookie `ob_refresh`
- `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth`, `Expires` = token `ExpiresAt`.
- Clearing it means the same attributes with an expiry in the past (`Response.Cookies.Delete` with the same path).
- Secure cookies work on `http://localhost` in modern browsers. The Vite proxy keeps it same-origin.

### Endpoints
| Method / path | Auth | Success | Errors |
|---|---|---|---|
| `POST /api/auth/login` | — | as M1-2 **plus** sets `ob_refresh` | as M1-2 |
| `POST /api/auth/refresh` | cookie (rate-limited) | **200** `AuthResponse` + a new `ob_refresh` | 401 `invalid_refresh_token` + cookie cleared |
| `POST /api/auth/logout` | — (cookie optional) | **204**, cookie cleared | — (always 204) |

Register still sets **no** cookie.

### Refresh algorithm
All in one DB transaction:
1. No cookie → 401.
2. Hash the token and look it up. Not found → 401.
3. Already revoked (`RevokedAt != null`) → **reuse**: revoke all the user's unrevoked tokens (`RevokedAt = now`), commit, 401.
4. Expired → 401.
5. User missing or suspended → revoke all the user's tokens, 401.
6. Rotate:
   - `UPDATE "RefreshTokens" SET "RevokedAt" = now, "ReplacedByTokenId" = @newId WHERE "Id" = @id AND "RevokedAt" IS NULL` (EF `ExecuteUpdateAsync`).
   - If 0 rows are affected, a parallel request won the race. Treat it as reuse, as in step 3.
   - Otherwise insert the new token with `ExpiresAt` = now + `Jwt:RefreshTokenDays` (**sliding**) and commit.
7. Issue the access token and set the new cookie.

Every 401 path clears the cookie.

### Logout
If the cookie is present and matches an unrevoked token, set `RevokedAt`. Always clear the cookie and return 204.

### Tests (integration)
- Login sets `ob_refresh` with HttpOnly, Secure, SameSite=Strict and Path=/api/auth. Register sets no cookie.
- Refresh returns a new access token and a new cookie, which differs from the old one. The new access token works on `/api/me`.
- Rotation: the old cookie is no longer valid after a refresh.
- Reuse: refresh with A → B; refresh with A again → 401, and B now also fails (everything revoked).
- Expiry: advance time 7 days + 1 min → 401. Sliding: refresh at day 6, then the token is still valid at day 12.
- Logout → 204, and refreshing with that cookie afterwards → 401. Logout with no cookie → 204.
- Suspended user: refresh → 401, and all their tokens are revoked.
- Use `HttpClient` with a manual `Cookie` header, or a cookie container. Note that `Secure` cookies aren't sent over the test server's http unless you set `BaseAddress` to `https://localhost`.

---

## 7. M1-4 Profile API

| Method / path | Auth | Body | Success | Errors |
|---|---|---|---|---|
| `PUT /api/me` | Bearer | `{ displayName }` | **200** `UserResponse` | 400 validation · 401 · 409 `display_name_taken` |

- Same validator rule as register (trimmed, 2–40). Email, password and avatar can't change.
- The clash check skips the user's own row, so keeping your own name, or changing only its casing, is allowed.
- Update both `DisplayName` and `NormalizedDisplayName`. Map a race on the unique index to 409.
- The access token's `name` claim stays stale until the next refresh. That's accepted, because the UI never reads the user from the token.

**Tests (integration):**
- update succeeds, and `GET /api/me` shows the new name
- invalid input → 400
- name taken by another user (other casing) → 409
- recasing your own name → 200
- no token → 401

---

## 8. Frontend (M1-5, M1-6)

### Structure (`src/web/src`)
```
app/store.ts            configureStore (auth slice + api reducer/middleware), RootState/AppDispatch, typed hooks
features/auth/authSlice.ts       { accessToken: string|null, user: UserResponse|null, status: 'restoring'|'authenticated'|'guest' }
features/auth/api.ts             RTK Query endpoints: register, login, refresh, logout, getMe, updateMe
features/auth/LoginPage.tsx, RegisterPage.tsx, RequireAuth.tsx, GuestOnly.tsx
features/profile/ProfilePage.tsx
lib/baseQuery.ts        fetchBaseQuery + reauth wrapper
lib/problem.ts          ProblemDetails parsing → { code?, fieldErrors }
components/AppHeader.tsx, components/UserMenu.tsx, components/FullPageSpinner.tsx
App.tsx                 <BrowserRouter> + routes; main.tsx wraps in <Provider store>
test/server.ts          MSW server + default handlers
```

### State and API calls
- **Access token: in memory only** (Redux state). Nothing goes in localStorage or sessionStorage.
- One RTK Query `createApi` with `baseUrl: '/api'` and `credentials: 'include'`. `prepareHeaders` adds `Authorization: Bearer <token>`.
- `baseQueryWithReauth`:
  - On a 401 from any request **except** `/auth/login`, `/auth/register`, `/auth/refresh` and `/auth/logout`, call `refreshOnce()`. Success → store the new token and user, then retry the original request **once**. Failure → dispatch `loggedOut()`; the route guard then sends the user to `/login`.
  - `refreshOnce()` keeps a module-level in-flight promise, so concurrent 401s in one tab share a single refresh. Inside it, the refresh runs in `navigator.locks.request('ob-refresh', ...)` so tabs take turns. Fall back to a direct call if `navigator.locks` is undefined.
- Login, refresh and the auto-login after register store `{ accessToken, user }`. `updateMe` replaces `user`. Logout calls the API, then clears state even if the call fails.

### Session restore
On app start, dispatch one refresh (`status: 'restoring'`) and show `FullPageSpinner` until it finishes. 200 → `authenticated`; any error → `guest`.

### Routes (react-router v7, declarative mode)
| Path | Access | Notes |
|---|---|---|
| `/` | public | Home placeholder: "Open Bargain" heading + the existing `HealthStatus` card |
| `/login` | guests only | logged-in → `/` |
| `/register` | guests only | logged-in → `/` |
| `/profile` | `RequireAuth` | guest → `/login?returnTo=/profile` |
| `*` | public | simple "Page not found" with a link home |

- **`returnTo`:** accept it only if it starts with `/` and not `//`. Otherwise use `/`.

### Pages
- **Header** (every page): "Open Bargain" on the left, linking to `/`.
  - Guest: right side shows `Log in` and `Sign up` buttons.
  - Signed in: right side shows the initials avatar (first letters of up to two words of the display name; shadcn `avatar`) with a dropdown: the display name (not clickable), `Profile`, `Log out`.
  - Same layout at mobile widths, no hamburger menu.
- **Login:** email and password fields (Formik + Yup: required, email format) and a submit button.
  - The button is disabled while submitting, to prevent double submits.
  - 401 → form-level message "Email or password is incorrect."
  - 429 → "Too many attempts. Try again in a minute."
  - Success → `returnTo` or `/`.
  - Shows the notice "Account created, please log in" when the URL has `?registered=1`.
- **Register:** email, display name, password.
  - Yup rules mirror the server: email format and ≤ 256; display name trimmed 2–40; password 8–128.
  - Server 400 `errors` are mapped onto Formik fields. 409 `email_taken` → email field "An account with this email already exists." 409 `display_name_taken` → display name field "This display name is taken."
  - On 201 the app automatically calls login with the same credentials, then goes to `returnTo` or `/`. If that login fails → `/login?registered=1`.
- **Profile** (`/profile`): a card with the initials avatar, email (read-only text) and a display-name form (Formik + Yup, same rule) with a Save button.
  - Success → an inline "Saved" message, plus the store user (and therefore the header) updates.
  - 409 → field error "This display name is taken."
- Layout: centered card, `max-w-sm`/`max-w-md`, 16 px side padding. Fully usable at 360 px wide.
- No agreement/payment wording is needed in M1, because there are no deals yet.

### Tests (Vitest + Testing Library + MSW)
- `test-setup.ts`: start the MSW node server with `onUnhandledRequest: 'error'`, reset handlers after each test, and stub `navigator.locks.request` as `(name, cb) => cb()`.
- Login: success → navigates to `returnTo`, and the header shows the initials.
- Login error: 401 → shows the message.
- Register: 409 field errors appear; success → auto-login → home.
- Session restore: refresh returns 200 on load → signed in. Refresh returns 401 → guest, with no spinner left.
- Refresh-and-retry: `/api/me` returns 401 once, then refresh returns 200, then `/api/me` returns 200 → the request succeeds, with exactly one refresh call. Two parallel 401s → a single refresh.
- Refresh fails → user logged out and sent to `/login`.
- Route guard: a guest on `/profile` → `/login?returnTo=%2Fprofile`. An unsafe `returnTo` (`//evil.com`) → `/`.
- Profile: save updates the header name; 409 shows the field error.

### M1-6 deploy doc
- Add a step to `docs/deploy.md` after §4 (*Connection string*): "JWT signing key".
  - Generate it with `openssl rand -base64 48` (or PowerShell `[Convert]::ToBase64String((1..48 | % { Get-Random -Max 256 }) -as [byte[]])`).
  - Add the App Service setting `Jwt__SigningKey` → Apply.
  - Keep it in a password manager. Changing it logs everyone out once access tokens expire.

### Manual checks for the user (the agent can't do these)
- [ ] Locally: `docker compose up -d`, run the API + `npm run dev`; sign up → reload (still logged in) → edit profile → log out → log in.
- [ ] Azure: add `Jwt__SigningKey`, merge and deploy, then repeat the flow above on the Azure URL.
- [ ] Mobile width (≈ 360 px) in devtools: header, forms and profile are all usable.

---

## Finally (after M1-6, own commit `M1: Update docs`)

- `docs/decisions.md`: add entries **#20 onward** (Confirmed, 2026-11-22) for Q1–Q21 above. Keep them short: one line each plus a reason where the reason isn't obvious.
- `CLAUDE.md` *How we work* and `docs/tasks/README.md` work loop: replace them with:
  1. A planning session with the user, where every decision is made.
  2. `docs/handovers/agent-handover-<Mn>.md` is written.
  3. The agent implements all tasks, one commit per task.
  4. The user reviews the commits.
  5. Push + PR.
- `CLAUDE.md` *Stack*: add Redux Toolkit/RTK Query, Formik + Yup, FluentValidation, MSW.
- `docs/architecture.md`: update the Authentication section (cookie path, reuse rule, sliding expiry, rate limit) and the Accounts tables (`NormalizedDisplayName`, `RefreshTokens` columns).
- Mark M1 `done` in `docs/tasks/README.md`. Leave M0-5 as it is.

---

## 9. Agent log

*The implementation agent records here any choice not covered above, plus any blocker.*

| Task | Decision / blocker | Why |
|---|---|---|
| M1-2 | Lockout is counted in `UserAccounts.CheckPasswordAsync` (Identity's lockout fields, our own counting) instead of `UserManager.AccessFailedAsync`/`IsLockedOutAsync`. | `UserManager` reads the system clock, so `FakeTimeProvider` couldn't drive the 15-min lockout test, and decision #19 says TimeProvider everywhere. |
| M1-2 | JwtBearer lifetime is checked by a `LifetimeValidator` that uses the injected `TimeProvider`; the expiry test runs at the API level. | §5 note; worked as intended. |
| M1-2 | `User.AllowedUserNameCharacters = ""` (any character). | `UserName` = email, and Identity's default character list rejects valid emails (e.g. with `'`). |
| M1-2 | Login validation: email format + password not empty (no length rule). | Login only needs to reject malformed input; length rules belong to register. |
| M1-2 | Emails are trimmed before lookup and storage. | Stray spaces from copy-paste shouldn't create a different account or fail login. |
| M1-2 | `Result<T>` + `AccountsError` live in Accounts `Application`; the rate-limit policy name is `SharedKernel.Endpoints.RateLimitPolicies.Auth`. | Only Accounts uses the result type so far; the policy name is shared by the host and the module. |
| M1-2 | `ProblemDetails` carry a `traceId`, so the "identical 401 body" test compares bodies without it. | `AddProblemDetails` adds a per-request trace id. |
| M1-2 | Unit tests now reference the Accounts module; the placeholder `SmokeTests` is removed. | Real unit tests exist now. |
| M1-3 | The refresh algorithm lives in the `RefreshSession` use case. `IRefreshTokenStore` offers `BeginTransactionAsync` (returns a small `IUnitOfWork`), find by hash, add, `TryRevokeAsync` (the conditional `ExecuteUpdateAsync`) and revoke-all. | Keeps the rules in Application; the store and `IUserAccounts` share the scoped `AccountsDbContext`, so the user check runs in the same transaction. |
| M1-3 | Extra tests: parallel refreshes with one token (at most one wins, and the winner's token is revoked too), unknown token → 401. | Covers the race path of step 6. |
| M1-4 | `PUT /api/me` for a suspended (or deleted) user returns 401, like `GET /api/me`. | architecture.md: every write action checks the suspended flag. |
| M1-5 | `react-router` pinned to `^7.18.6`. | npm's latest is v8; Q17 says v7. |
| M1-5 | Added `radix-ui`, `lucide-react` and `class-variance-authority`. | The shadcn CLI wrote the components but didn't install their imports. |
| M1-5 | MSW 3 calls the option `onUnhandledFrame: 'error'` (was `onUnhandledRequest`). | Renamed in MSW 3. |
| M1-5 | No RTK Query `refresh` endpoint. `refreshOnce(dispatch)` in `lib/baseQuery.ts` does the refresh with `fetch` and is used by both the session restore and the reauth wrapper. | One code path, and StrictMode's double effect shares the in-flight refresh instead of sending two (which would trip reuse detection). |
| M1-5 | The reauth wrapper skips the refresh if the access token changed while the request was in flight, and just retries. | Two parallel 401s then never cause a second refresh, even if the first refresh finished before the second 401 arrived. |
| M1-5 | Login and sign-up don't navigate themselves: they store the session, and `GuestOnly` redirects to the safe `returnTo` (or `/`). | Avoids a race between the store update and `navigate`. |
| M1-5 | `safeReturnTo` also rejects values starting with `/\`. | Browsers treat `/\evil.com` like `//evil.com`. |
| M1-5 | API base URL is `${window.location.origin}/api`. | Node's `fetch` (in Vitest) rejects relative URLs; same origin in the browser. |
| M1-5 | `/profile` shows a placeholder until M1-6. | The route and guard belong to M1-5. |
| M1-5 | `vite build` warns that the bundle is > 500 kB (≈180 kB gzipped). Left as is. | Code splitting isn't needed yet. |
| M1-5 | `npm run lint` reports one warning (`only-export-components`) in the generated shadcn `button.tsx`. Left as is. | Generated code; it's a warning, not an error. |
