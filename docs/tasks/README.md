# Tasks

Features (M0–M10) are done in order. Each feature file breaks into small tasks. A feature gets its detailed tasks just before work starts on it.

Work loop for each task:
1. Plan.
2. User approves.
3. Implement with tests.
4. Build + tests pass.
5. Self-review.
6. User reviews the diff.
7. Commit.

Status: `todo` · `in progress` · `done`

| Feature | File | Status |
|---|---|---|
| M0 Walking skeleton | [M0-walking-skeleton.md](M0-walking-skeleton.md) | in progress |
| M1 Accounts (sign-up, JWT login, profile) | — | todo |
| M2 Create/edit room + media | — | todo |
| M3 Discovery, join, private invitations | — | todo |
| M4 Room lifecycle timer + event history | — | todo |
| M5 Offers, holds, counters, deals, price floor | — | todo |
| M6 Live room (SignalR) + Q&A | — | todo |
| M7 Notifications (in-app, email) | — | todo |
| M8 My rooms / offers / deals | — | todo |
| M9 Reports + admin | — | todo |
| M10 Metrics events | — | todo |

## M0 tasks

| Task | Status |
|---|---|
| M0-1 Backend solution | done |
| M0-2 PostgreSQL connection | todo |
| M0-3 Frontend | todo |
| M0-4 CI | todo |
| M0-5 Deploy to Azure | todo |

## PRD acceptance criteria → features

| PRD §14 criterion | Feature(s) |
|---|---|
| 1. Seller creates a complete public/private room | M2 |
| 2. Discover/join public room; private only with invitation | M3 |
| 3. Notified on room start and own offer/deal changes | M7 |
| 4. Live room shows synced product, schedule, stock, offers, Q&A | M6 |
| 5. Buyer submits offer and asks questions while live | M5, M6 |
| 6. Seller answers, accepts/counters/rejects; buyer accepts/declines counter | M5, M6 |
| 7. Holds + multiple deals never exceed stock under concurrency | M5 |
| 8. No deal below the visible price floor | M5 |
| 9. Ended/cancelled/sold-out rooms reject offers and show final state | M4, M5 |
| 10. Deals shown as agreements, not payments | M5, M8 |
