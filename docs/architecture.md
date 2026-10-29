# Architecture

Plain overview of how Open Bargain is built. Keep this short and update it when something real changes.

## Shape

One ASP.NET Core app (`src/OpenBargain.Api`) does three things:
- serves the REST API
- serves the SignalR hub for live rooms
- serves the built React app from `src/web`

There is one PostgreSQL database and one deployment to Azure App Service.

## Modules

The code is split into **modules** (folders under `Modules/`). Each module owns its tables and logic and exposes **one public API**. Other modules talk to it only through that API.

```
Modules/Rooms/
  RoomsApi.cs        # public API: the only thing other modules may call
  Contracts/         # public request/response types used by that API
  ...                # everything else is internal to the module
```

Rules:
- A module never reads or writes another module's tables.
- A module never uses another module's internal classes, only its public API and `Contracts`.
- Module APIs are called in-process: a method call, no HTTP between modules. This keeps calls fast and lets a single database transaction cover a whole operation. If a module is ever split out into its own service, its public API is the line where it gets cut.

| Module | Owns |
|---|---|
| **Accounts** | Sign-up, login, profile (display name, avatar) |
| **Rooms** | Room create/edit, media, schedule, visibility, invitations, discovery, joining, participants (pseudonym / show real name), room state, room event history |
| **Bargaining** | Offers, counter-offers, stock holds, deals, price floor |
| **QA** | Questions and seller answers |
| **Notifications** | In-app notifications, email, preferences |
| **Admin** | Reports, removing rooms, suspending users |

## Main tables

- **Users**: account, display name, avatar, role (user/admin), suspended flag
- **Rooms**:
  - seller, title, description, category, condition + note, location, pickup info
  - currency, asking price per unit, hidden minimum price (optional)
  - total quantity, price floor (null until the first deal)
  - visibility (public/private), start time, end time, state
- **RoomMedia**: room, Blob Storage URL, type (image/video), order
- **RoomParticipants**: room, user, joined at, pseudonym in this room, `ShowRealName` (default false)
- **Invitations**: private room, invited user
- **Offers**:
  - room, buyer, price per unit, requested quantity, message
  - counter quantity (set when the seller proposes a different quantity)
  - held quantity, hold expires at
  - status, created at
- **Deals**: room, offer, buyer, seller, quantity, price per unit, total, currency, created at
- **Questions**: room, buyer, text, created at, answer text, answered at
- **RoomEvents**: room, sequence number (increasing per room), type, data (JSON), created at. Append-only; this is the audit history and the activity stream.
- **Notifications**: user, type, room, text, read flag, created at
- **Reports**: reporter, target (room or user), reason, status

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

## Identity

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
- **Configuration:** connection strings and secrets live in App Service settings, never in the repo.

## Deferred until needed
- Azure SignalR Service (for scaling past one instance)
- a job library such as Hangfire
- infrastructure-as-code (Bicep)
- staging slots
- splitting modules into separate services
