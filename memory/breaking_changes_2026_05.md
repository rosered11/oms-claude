---
name: Breaking Changes — May 2026
type: decision
---
Five breaking changes were applied to the OMS in May 2026. All docs under `docs/` were updated to reflect them.

**1. State machine simplified — BookingConfirmed, Invoiced, Paid removed.**

New state machine:
```
Pending → PickStarted → PickConfirmed → Packed → OutForDelivery → Delivered
              ↕                    ↕
            OnHold              Cancelled
```
Plus side/terminal states: `ReadyForCollection`, `Collected`, `Returned`.

- `BookingConfirmed` removed: delivery booking now happens externally via TMS before order creation.
- `Invoiced` removed: invoicing handled by POS/STS outside OMS.
- `Paid` removed: payment confirmation handled by Gateway/POS outside OMS.
- Cancellation now only allowed from `Pending` or `OnHold` (not `BookingConfirmed`).
- `POST /webhooks/wms/booking-confirmed` endpoint removed.
- `POST /webhooks/pos/invoiced` endpoint removed.
- `POST /webhooks/pos/payment-confirmed` endpoint removed.

**2. `ApiResult.DispatchOutbox` renamed to `ApiResult.BuildOutboxEvents`.**

Update any code snippet in docs that uses the old name.

**3. `payment_flow` changed from bool to string.**

- Old: `bool payment_flow` (`true` = prepaid, `false` = pay-on-delivery)
- New: `VARCHAR(50) payment_flow` — values: `"PRE_PAID"` or `"PAY_ON_DELIVERY"` (extensible)
- The mapping in outbox payloads is now a direct pass-through — no boolean-to-string conversion needed.

**4. Monetary values changed from satang to baht.**

- Old: stored in satang (integer), e.g. `245000` for ฿2,450.
- New: stored in baht with decimal precision, e.g. `2450.00` for ฿2,450.
- No `/100` conversion in outbox payload builders.
- DB types for monetary columns should be `DECIMAL(10,2)`.

**5. `GET /branches/nearby` removed.**

The endpoint `GET /branches/nearby` (Group: Branches) no longer exists in OMS.

**Why:** These changes remove external system responsibilities (delivery booking, invoicing, payment) from the OMS state machine, making the OMS lifecycle end cleanly at `Delivered` or `Collected`. The `payment_flow` and monetary changes align the data model with external API contracts.

**When to apply:** Always use the new state machine, new `payment_flow` string values, and baht monetary amounts in all documentation and code. Never reference `BookingConfirmed`, `Invoiced`, or `Paid` as OMS order states.
