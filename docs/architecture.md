# Architecture

Plain overview of how Open Bargain is built. Keep this short and update it when something real changes.

## Shape

It is a **modular monolith**: one app, one deployment to Azure App Service and one PostgreSQL database. Inside, the code is split into modules with clear boundaries. Each module follows **clean architecture** internally.

The host project `src/OpenBargain.Api` contains no business logic. It:
- starts the app
- sets up JWT auth
- wires up the modules
- hosts the SignalR hub
- serves the built React app from `src/web`

## Projects

```
src/
  OpenBargain.Api/                          # host
  OpenBargain.SharedKernel/                 # tiny shared basics: base entity, Result, IClock
  Modules/
    Rooms/
      OpenBargain.Modules.Rooms/            # the module itself; everything here is internal
        Domain/
        Application/
        Infrastructure/
        Endpoints/
      OpenBargain.Modules.Rooms.Contracts/  # the module's public API: IRoomsApi + request/response types
    Accounts/  Bargaining/  QA/  Notifications/  Admin/   # same shape
  web/                                      # React + Vite
tests/
  OpenBargain.UnitTests/                    # domain and application logic
  OpenBargain.IntegrationTests/             # API + real PostgreSQL
  OpenBargain.ArchitectureTests/            # enforce the boundary rules below
```

Modules are created when their first task starts, not up front.

## Module boundaries

| Module | Owns |
|---|---|
| **Accounts** | Users, sign-up, login, JWT + refresh tokens, profile (display name, avatar), roles, suspension flag |
| **Rooms** | Rooms, media, schedule, visibility, invitations, discovery, joining, participants (pseudonym / show real name), room state, room event history |
| **Bargaining** | Offers, counters, stock holds, deals, sold quantity, price floor |
| **QA** | Questions and seller answers |
| **Notifications** | In-app notifications, email, preferences |
| **Admin** | Reports, admin actions (remove room, suspend user) |

Rules:
- Each module exposes **one public API**: an interface such as `IRoomsApi` in its `.Contracts` project.
- A module may reference **only other modules' `.Contracts` projects**, never the module projects themselves. Classes inside a module are `internal`, so the compiler enforces this.
- A module never reads or writes another module's tables. Each module has its own `DbContext` and its own PostgreSQL schema (`rooms`, `bargaining`, ...).
- Calls between modules are in-process method calls, not HTTP calls. If a module is ever split into its own service, its `.Contracts` API is where it gets cut.

## Clean architecture inside a module

There are four layers. Dependencies point inward only.

```
Endpoints  ──►  Application  ──►  Domain
Infrastructure ──► Application, Domain
```

- **Domain:** entities and business rules, for example offer state changes, room state changes and price rule checks. No dependencies on EF Core, ASP.NET or anything else outside.
- **Application:** use cases, one class per action (`CreateOffer`, `AcceptOffer`, `CounterOffer`, ...). It also defines the interfaces it needs from outside (data access, blob storage, email, clock). It implements the module's public API.
- **Infrastructure:** implements those interfaces with EF Core, the PostgreSQL advisory lock, Azure Blob Storage and email.
- **Endpoints:** HTTP endpoints (minimal APIs) that check auth, call a use case and map the result to a response.

Keep it simple:
- use plain classes and constructor injection
- no MediatR, no generic repositories, no CQRS frameworks
- add a pattern only when a real need shows up

`OpenBargain.ArchitectureTests` checks these rules:
- Domain depends on nothing else
- Application doesn't depend on Infrastructure or Endpoints
- modules reference only other modules' Contracts

## Main tables (by module)

- **Accounts**
  - **Users**: email, password hash, display name, avatar, role (user/admin), suspended flag
  - **RefreshTokens**: user, token hash, expires at, revoked at
- **Rooms**
  - **Rooms**:
    - seller, title, description, category, condition + note, location, pickup info
    - currency, asking price per unit, hidden minimum price (optional)
    - total quantity, visibility (public/private), start time, end time, state
  - **RoomMedia**: room, Blob Storage URL, type (image/video), order
  - **RoomParticipants**: room, user, joined at, pseudonym in this room, `ShowRealName` (default false)
  - **Invitations**: private room, invited user
  - **RoomEvents**: room, sequence number (increasing per room), type, data (JSON), created at. Append-only; this is the audit history and the activity stream. Other modules add events through `IRoomsApi`.
- **Bargaining**
  - **RoomStock**: room, sold quantity, price floor (null until the first deal)
  - **Offers**:
    - room, buyer, price per unit, requested quantity, message
    - counter quantity, held quantity, hold expires at
    - status, created at
  - **Deals**: room, offer, buyer, seller, quantity, price per unit, total, currency, created at
- **QA**
  - **Questions**: room, buyer, text, created at, answer text, answered at
- **Notifications**
  - **Notifications**: user, type, room, text, read flag, created at
  - **NotificationPreferences**: user, settings
- **Admin**
  - **Reports**: reporter, target (room or user), reason, status
  - **AdminActions**: admin, action, target, reason, created at

## Transactions across modules

Some actions touch more than one module. For example, confirming a deal:
- updates the offer and stock (Bargaining)
- adds a room event (Rooms)
- may mark the room sold out (Rooms)

To keep that in one transaction, all module `DbContext`s in one request **share the same database connection and transaction**. The use case that starts the action opens the transaction. Other modules' APIs called during it join it automatically.

## Stock: sold, held, available

Every room's stock splits into three parts:
- **Sold**: the sum of the room's deals. Permanent.
- **Held**: the sum of held quantities on open offers (Pending or Countered). Temporary.
- **Available** = total − sold − held. This is what a new offer can take.

The room page shows total, sold, held and available.

## Room states

- **Scheduled**: published, start time in the future
- **Live**: between start and end time
- **Ended**: end time passed, or the seller closed the room
- **SoldOut**: sold quantity equals total quantity
- **Cancelled**: cancelled by the seller or an admin before it finished

Ended, SoldOut and Cancelled are final. When a room reaches any of them, all open offers expire and their holds are released.

## Offers: hold, then confirm

1. **Buyer offers** a price and a quantity.
   - The quantity must not exceed the available stock.
   - That many units are **held** for the buyer for **5 minutes**.
   - The offer is **Pending**.
   - A buyer can have only **one open offer** (Pending or Countered) per room at a time.
2. The **seller** then does one of three things:
   - **Accept**: the deal is confirmed straight away.
   - **Counter**: proposes a different quantity at the same price.
     - The quantity can be fewer or more than requested; more only if it fits in held + available.
     - The hold changes to the new quantity and restarts at 5 minutes, so the buyer has time to answer.
     - The offer becomes **Countered** and the buyer must answer.
   - **Reject**: the offer becomes **Rejected** and the hold is released.
3. When an offer is **Countered**, the buyer either:
   - **Accepts**: the deal is confirmed.
   - **Declines**: the offer becomes **Declined** and the hold is released.
4. **Deal confirmed** means:
   - a Deal is saved
   - the hold turns into sold quantity
   - the price floor is updated
   - the offer becomes **Accepted**
5. Before the seller answers, the buyer can **withdraw** the offer, which releases the hold.
6. When the hold runs out with no deal, the offer becomes **Expired**. This happens if the seller doesn't answer, or the buyer doesn't answer a counter.

Offer states: Pending, Countered, Accepted, Rejected, Declined, Withdrawn, Expired. Only Pending and Countered hold stock.

## Price rules

- **Hidden minimum:** the seller's minimum price is never shown. If no minimum is set, the asking price is used.
- **Price floor:**
  - The floor is the highest per-unit price of any deal in the room so far. It is **shown to everyone** in the room.
  - A new offer must be at or above both the hidden minimum and the floor.
  - Every deal confirmation checks the floor again. An open offer that has fallen below the floor can no longer become a deal, and it is expired at that moment.

## Concurrency: one lock per room

All changes to a room's stock run in **one database transaction** that first takes a **PostgreSQL advisory lock for that room**: `pg_advisory_xact_lock(key)`, where the key is derived from the room id. The lock is released automatically when the transaction ends.

These changes are:
- creating an offer
- accepting or countering it
- confirming a counter
- rejecting, declining or withdrawing it
- expiring holds
- room state changes

While the lock is held:
1. Check the room state, the clock, the offer status and the price rules.
2. Recalculate the available stock from the database and check the quantity.
3. Write the changes and the RoomEvents.
4. Commit.

Only **after** commit, push the new events to the room over SignalR and create notifications.

Every path that changes stock must take the same lock. An automated test fires simultaneous offers and accepts at a real PostgreSQL database and proves stock is never over-allocated.

## Authentication (JWT)

- **Users and passwords:** ASP.NET Core Identity stores users and hashes passwords.
- **Login:** returns a short-lived **JWT access token** (15 minutes) and a **refresh token** (7 days).
  - The access token is kept in memory in the React app and sent as `Authorization: Bearer ...`.
  - The refresh token is sent as an httpOnly, secure cookie. It is stored hashed in the database, and each use issues a new one and revokes the old one.
- **SignalR** receives the access token as the `access_token` query parameter, the standard way for websockets.
- **Roles:** the JWT carries the user id, display name and role (`Admin` for admins).
- **Suspension:** a suspended user's refresh tokens are revoked, so they are locked out within 15 minutes at most. Every write action also checks the suspended flag.
- **Signing key:** stored in App Service settings, never in the repo.

## Background timer

A simple timer in the app (a `BackgroundService` running every ~15 seconds) does three things:
- moves rooms from Scheduled to Live, and from Live to Ended, when their times pass
- expires offers whose hold has run out
- sends reminders

Every write also checks the clock itself. So if the timer is a few seconds late, an offer after the end time or a confirmation after the hold expires is still rejected.

## Live updates

- Each room has a SignalR group. Viewers of a room join that group.
- After each committed change, the server sends the new RoomEvent(s) to the group.
- Each event carries the room's sequence number. If the client sees a gap, or it reconnects, it reloads the room from the API. Simple and always correct.
- Clients never decide state themselves. They show what the server sends.

## Identity in rooms

- In every room, buyers appear under a pseudonym by default (for example "Buyer 7").
- A buyer can choose to show their real display name in a room. That only changes what is shown; contact details are never exposed.
- The seller is always shown as the seller, with their profile summary.

## Privacy

- Every discovery, search and share-preview query filters to public rooms.
- Private room endpoints check that the user is the seller, an invitee or an admin. Anyone else gets "not found", not "forbidden", so the room's existence is not revealed.

## Hosting and delivery

- **GitHub Actions:** build + test on every pull request; on merge to `main`, build and deploy to Azure App Service.
- **Azure:**
  - App Service runs the app
  - Azure Database for PostgreSQL (Flexible Server)
  - Blob Storage for media
  - Communication Services for email
- **Configuration:** connection strings, the JWT signing key and other secrets live in App Service settings, never in the repo.

## Deferred until needed
- Azure SignalR Service (for scaling past one instance)
- a job library such as Hangfire
- infrastructure-as-code (Bicep)
- staging slots
- an in-process event bus between modules
- splitting modules into separate services
