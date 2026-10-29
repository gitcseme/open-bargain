# Open Bargain — MVP Product Requirements

**Document type:** MVP PRD and implementation handoff  
**Status:** Ready for implementation planning  
**Product:** Open Bargain  
**Initial market:** Bangladesh (BDT); keep currency handling extensible  
**Platform:** Mobile-first web application

## 1. Product definition

Open Bargain is a marketplace organized around scheduled, live bargain rooms. A seller creates a room for a product, adds its details and available quantity, and schedules when negotiation will begin. Interested buyers join public rooms or enter private rooms by invitation. When the room is live, buyers can see offers, ask product questions in a shared Q&A, and make offers. The seller can accept deals with multiple buyers, subject to remaining stock.

**MVP promise:** Join a live product room, get answers in public, and bargain transparently with the seller and other interested buyers.

The room is the primary product experience. This MVP is not intended to be a general classifieds site with bargaining added as a secondary feature.

## 2. MVP goals

- Let sellers create and schedule a product-specific bargain room.
- Let buyers discover and join public rooms, and join private rooms through invitations.
- Notify joined buyers about room start and meaningful room updates.
- Support visible, structured offers and public product Q&A during a live room.
- Let one seller make multiple deals while preventing accepted quantities from exceeding available stock.
- Preserve price fairness: once the seller accepts a per-unit price in a room, the seller cannot accept a lower per-unit price later in that room.
- Record room and deal outcomes clearly.

## 3. Out of scope

- In-app payment, escrow, delivery, or dispute resolution.
- Native mobile applications.
- Auctions, automatic bidding, group-buy price unlocks, or multiple bargaining modes.
- Private one-to-one negotiation inside a room.
- AI pricing or negotiation assistance.
- Advanced reputation scoring, recommendations, advertising, or historical market-price analytics.
- Complex seller storefronts or bulk inventory management.

An accepted deal records an agreement; it does not mean payment or fulfillment has occurred. The UI must communicate this distinction.

## 4. User roles

### Seller

- Creates and manages product rooms.
- Sets product information, available quantity, room visibility, scheduled start, and end time.
- Answers public questions.
- Reviews offers and accepts or rejects them.
- Can accept deals with multiple buyers up to available stock.

### Buyer

- Browses public rooms and views room details.
- Joins public rooms or enters an invited private room.
- Receives notifications for rooms they joined.
- Views visible offers, asks public questions, and submits offers while the room is live.
- Can view their own offer and deal status.

### Observer

An authenticated user viewing a public room without joining it. Observers may see public product information and room activity, but cannot submit offers or questions until they join. (MVP default; see decisions below.)

## 5. Core room lifecycle

Room states:

1. **Scheduled** — created and published, start time is in the future.
2. **Live** — negotiation is open between scheduled start and end, unless stock is exhausted or the seller closes the room.
3. **Ended** — time expired or seller closed the room; no new offers or questions.
4. **Sold out** — available quantity is fully allocated to accepted deals; no new offers or deals.
5. **Cancelled** — seller or admin cancelled the room before completion.

The system is authoritative for state and timestamps. Clients show the server-supplied start/end times and current state. Refreshing or reconnecting must not create a second room or duplicate an event.

### Seller room creation

Required fields:

- Room/topic title (for example, “iPhone 16 Pro”).
- Product description.
- At least one product image; video is optional.
- Category.
- Condition (new/used; allow a short condition note).
- Total available quantity (positive integer).
- Currency and asking price per unit.
- Pickup location and pickup/delivery information.
- Visibility: public or private.
- Scheduled start date/time and room end date/time.

The seller can preview and edit a scheduled room. Once live, the seller can edit non-price descriptive details, answer Q&A, manage offers, and close the room. The seller cannot change the room’s available quantity below the amount already allocated to accepted deals.

For a private room, the seller can invite registered users. Only invitees can see or join the room. Private room content and activity must not appear in public discovery or public share previews.

## 6. Buyer room participation

- Public rooms appear in discovery with title, image, category, asking price, location, start/end time, and state.
- Buyers can join a scheduled room before it starts. Joining registers them for room notifications.
- Buyers can join a live room and participate immediately.
- Joining is distinct from merely viewing. A joined buyer can submit offers and questions.
- Users cannot join a cancelled, ended, or sold-out room for active participation.
- A public room can be viewed without joining, subject to authentication policy; offer and question actions require login and joining.

## 7. Offers and deal rules

An offer is a structured proposal containing:

- Buyer and room.
- Per-unit price in the room currency.
- Requested quantity (positive integer).
- Optional short message.
- Server timestamp and status.

Offer statuses: **pending**, **countered**, **accepted**, **rejected**, **declined**, **withdrawn**, or **expired**. Withdrawal is supported while an offer is pending and stays visible in the event history.

### Offer behavior

- Offers are visible to room participants. Buyers appear under a pseudonym by default and may choose to show their real display name in a room. Never expose buyer contact details.
- Offers must be at or above the room’s current minimum acceptable per-unit price. The minimum is seller-configured and hidden from buyers. If no minimum is configured, use the asking price as the minimum. The product must validate this rule on the server.
- A buyer may have only one open (pending or countered) offer per room at a time. They may make a new offer once the previous one is closed, subject to reasonable rate limits.
- **Stock hold:** submitting an offer holds its requested quantity for 5 minutes. An offer cannot request more than the currently available quantity (total − sold − held).
- Seller actions are **accept**, **counter**, or **reject**:
  - Accept confirms the deal for the requested quantity at the offered per-unit price.
  - Counter proposes a different quantity (fewer, or more if stock is available) at the same price. The hold moves to the new quantity and restarts at 5 minutes; the buyer then accepts (deal confirmed) or declines.
  - Reject releases the hold.
- Stock is permanently reduced only when a deal is confirmed. Rejected, declined, withdrawn, and expired offers release their hold.
- If the hold period passes without a confirmed deal, the offer expires and its hold is released.
- Holds and deals are allocated atomically. Simultaneous requests must never make sold + held exceed the room’s total quantity.
- When sold quantity reaches the total, mark the room sold out and disable new offers and deals.
- A deal records buyer, seller, room, accepted quantity, per-unit price, total amount, currency, and timestamp. Do not represent the deal as paid or fulfilled.

### Price fairness rule

Within one room, after a deal is confirmed at per-unit price **P**, no later deal in that room can be below **P** per unit. The highest confirmed price becomes the room’s **price floor**, which is **visible to everyone in the room**. Offers below the floor are rejected by server validation. An open offer that falls below a newly raised floor can no longer be confirmed and expires. The seller may accept offers at the floor or above it.

This rule is per-unit. Quantity differences do not silently change the floor. If the seller wants to sell at a lower price, the seller must close the room and create a new room with the revised terms. The room event history must make accepted prices, quantities, and remaining stock understandable.

## 8. Live public Q&A

- Joined buyers can post a question while the room is live.
- Questions and seller replies are visible to all room participants, with pseudonymous buyer names.
- Seller replies are visibly identified as the seller.
- Q&A is separate from offers; a question does not constitute an offer or deal.
- Questions are timestamped and remain in the room history.
- Disable new questions when a room ends, sells out, or is cancelled. Existing Q&A remains readable to authorized viewers.

## 9. Room activity and real-time updates

The live room must show:

- Product title, photos/video, description, condition, pickup location, seller profile summary.
- Room state and countdown to scheduled start or end.
- Asking price, price floor (if any), and quantity: total, sold, held, and available.
- Joined participant count (may be approximate).
- Chronological activity stream for offers and their state changes, deals, questions, seller answers, room start, room end, and sold-out state.
- Offer composer and Q&A composer when the current user is eligible.

Activity ordering and timestamps are server-authoritative. The interface must prevent duplicate submissions and communicate loading, failure, and reconnection states. A user must not see contradictory stock or room states after reconnecting.

## 10. Notifications

Notify joined buyers about:

- Room starting (including a reminder before start, if supported).
- Seller edit to material room details or schedule.
- New seller announcement or answer to a question they asked.
- Their offer being accepted, countered, rejected, or expired (and, for sellers, a counter being accepted or declined).
- Their deal being recorded.
- Room ending soon, ended, sold out, or cancelled.

MVP delivery can use in-app notifications and email. Browser push/SMS/WhatsApp are out of scope unless already available in the implementation stack. Respect notification preferences and avoid notifying users about every unrelated Q&A message by default.

## 11. Discovery and account basics

Minimum app surfaces:

- Public discovery page with scheduled and live rooms.
- Room detail page with shareable URL for public rooms.
- Create/edit scheduled room flow.
- User sign-up, login, and basic profile with display name and avatar.
- My rooms (seller) and Joined rooms / My offers / My deals (buyer).
- In-app notification list.

Discovery filters for MVP: category, location, and scheduled/live state. Search by room title is recommended. Private rooms must never be discoverable through search or public feeds.

## 12. Trust, moderation, and admin minimum

- Users can report a room or user with a reason.
- Admin can search users and rooms, review reports, remove a room, and suspend a user.
- Keep an auditable event history for room state changes, offer state changes, accepted deals, quantity allocation, and moderation actions.
- Apply basic server-side rate limits to offer and question creation.
- Do not display private contact details publicly.

## 13. MVP success measures

Track:

- Rooms created, published, started, and cancelled.
- Scheduled-room join rate and attendance at start.
- Time from room start to first offer.
- Offers and questions per live room.
- Share visits and visitor-to-join conversion for public rooms.
- Accepted deals per room and quantity sold.
- Percentage of live rooms with at least one offer and at least one deal.
- Repeat participation by buyers and repeat room creation by sellers.
- Cancellation, report, and offer rejection rates.

The primary validation question is: **Do scheduled rooms bring multiple interested buyers together to make visible offers and produce real seller-buyer agreements?**

## 14. Acceptance criteria

An implementation is MVP-ready when:

1. A seller can create a complete public or private room with product media, quantity, location, and a future schedule.
2. A buyer can discover and join a public room, or join a private room only with a valid invitation.
3. Joined buyers are notified when a scheduled room starts and when their own offer/deal status changes.
4. A live room displays synchronized product, schedule, inventory, visible offer activity, and public Q&A.
5. A joined buyer can submit an offer with price and quantity and ask a question while the room is live.
6. A seller can answer questions and accept, counter, or reject offers; a buyer can accept or decline a counter.
7. Offers hold stock for a limited time, and multiple deals can be confirmed without sold + held units ever exceeding the total, including under concurrent requests.
8. Once a deal is confirmed at a price, the server prevents later deals below that room’s visible price floor.
9. Ended, cancelled, and sold-out rooms reject new offers and clearly show their final state.
10. Deals are recorded as agreements, not as completed payments or fulfillment.

## 15. Decisions and implementation assumptions

These defaults make the PRD actionable. Confirm or revise them during implementation planning if product discovery requires it:

- **Market/currency:** Launch in Bangladesh with BDT; design currency as room data.
- **Identity:** Buyers use pseudonyms by default and may choose to show their real display name per room; seller is clearly identified. Private rooms require account login and invitation.
- **Visibility:** Anyone may view public room details; only authenticated joined users may offer or ask questions.
- **Minimum price:** Seller may set a hidden minimum per-unit price; default to asking price if not set. The price floor from confirmed deals is visible.
- **Stock hold and counter:** An offer holds its quantity for a limited time. The seller may counter with a different quantity; the buyer confirms or declines.
- **Room duration:** Seller sets start and end times; no automatic extension.
- **Offer expiry:** Open offers expire when their hold period passes or the room ends, sells out, or is cancelled. The room displays this rule before submission.
- **Accepted deal:** Records a non-payment agreement; buyer and seller arrange payment and pickup outside the MVP.
- **Notifications:** In-app plus email are the initial channels; browser push is optional.
- **Moderation:** Basic reports and manual admin actions are included; advanced fraud detection is deferred.

## 16. Core user journey

**Seller:** Create account → create product room → set quantity, visibility, schedule, and terms → publish/share room → answer questions → review visible offers → accept/reject offers → room sells out or ends → coordinate payment and pickup outside the app.

**Buyer:** Discover or receive invitation → join scheduled room → receive start notification → review product and Q&A → submit offer with price and quantity → receive decision → view recorded deal if accepted → coordinate payment and pickup outside the app.

## 17. Product principle

Keep the room focused on the product and the live bargain. Every feature in the MVP should help buyers gather, understand the item, make offers, or help the seller close fair, inventory-aware deals.
