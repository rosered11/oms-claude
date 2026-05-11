# Sprint Connect OMS — System Overview

**Version:** 2.0  
**Host:** `https://api.sprintconnect.io/v1`

---

## 1. System Overview

Sprint Connect OMS is an Order Management System that orchestrates outbound customer orders (Delivery, Express, Click & Collect) and inbound warehouse operations (Purchase Orders, Transfer Orders, Returns, Damaged Goods). It operates as a **modular monolith** composed of five bounded contexts, each owning a dedicated MySQL schema.

### 1.1 Bounded Contexts

| Module | Schema | Responsibility |
|---|---|---|
| Order | `orders` | Order lifecycle, state machine, webhooks, outbox, delivery slots |
| Payment | `payment` | Invoices, credit notes, POS recalculations, fees, promotions |
| Returns | `returns` | Customer returns, put-away, refunds |
| Config | `config` | Store locations, business units, rollout policies, routing rules |
| Inbound | `inbound` | Purchase orders, transfer orders, damaged goods |

### 1.2 System Actors

| Actor | Key | Role |
|---|---|---|
| Customer | C | Places and receives orders |
| CFW Gateway | GW | Customer-facing API gateway; forwards customer requests |
| Proxy Service | PS | Internal routing proxy between GW and backend systems |
| CHG Backend | CBE | Manages delivery booking slots |
| Sprint Connect | SC / OMS | This system — order orchestration hub |
| Warehouse System | WMS | Picks, packs, and stores goods |
| Transport System | TMS | Dispatches drivers and tracks deliveries |
| Point of Sale | POS | Calculates prices, applies promotions, issues invoices |
| Settlement Tax System | STS | Issues official ABB/Tax invoices post-delivery (Prepaid) |

### 1.3 Integration Pattern

OMS integrates with external systems using two patterns:

- **Outbox** — domain events are written atomically with the order state change in the same DB transaction, then dispatched by a background worker to WMS/TMS/POS/GW.
- **Webhooks** — external systems POST callbacks to OMS inbound webhook endpoints. All webhook endpoints return `202 Accepted` immediately. Each handler stages an `OrderWebhookLog` entry atomically with the domain state change.

---

## 2. Order State Machine

### 2.1 Standard Flow (Post-Paid / Delivery)

```
Pending → BookingConfirmed → PickStarted → PickConfirmed → Packed → OutForDelivery → Delivered → Invoiced → Paid
```

| Transition | Trigger |
|---|---|
| `Pending` (initial) | `POST /orders` |
| `BookingConfirmed` | WMS webhook: `/webhooks/wms/booking-confirmed` |
| `PickStarted` | WMS webhook: `/webhooks/wms/pick-started` |
| `PickConfirmed` | WMS webhook: `/webhooks/wms/pick-confirmed` |
| `Packed` | WMS webhook: `/webhooks/wms/packed` |
| `OutForDelivery` | TMS webhook: `/webhooks/tms/package-dispatched` |
| `Delivered` | TMS webhook: `/webhooks/tms/package-delivered` |
| `Invoiced` | POS webhook: `/webhooks/pos/invoiced` |
| `Paid` | POS webhook: `/webhooks/pos/payment-confirmed` |

### 2.2 Prepaid Flow (Delivery — Slot Pre-Booked via TMS/CBE)

The prepaid flow differs from post-paid in three key ways:

1. **No BookingConfirmed step** — the delivery slot is booked directly via TMS/CBE before the sale order is created. OMS transitions from `Pending` directly to `PickStarted`.
2. **ABB/Tax Invoice issued before dispatch** — SC sends the invoice to WMS after `PickConfirmed` and before TMS dispatches.
3. **STS settles tax invoice post-delivery** — STS sends the official ABB/Tax Invoice to SC after `Delivered`, which SC forwards to GW (customer).

**Prepaid sequence:**

```
[Customer → GW → PS → TMS → CBE]  TimeSlotRequested
[Customer → GW → PS → TMS → CBE]  BookingCreated (slot locked)
[Customer → GW → PS → SC]         OrderCreated → Pending
SC → CBE                           SaleOrderSentToCBE (outbox)
SC → WMS                           SaleOrderSentToWMS (outbox)
WMS → SC                           PickStarted (webhook) — no BookingConfirmed step
SC → TMS                           PickStartedEvent (outbox)
[WMS → SC → POS → SC → WMS]       POS Recalculation loop (repeats as needed)
WMS → SC                           PickConfirmed (webhook)
SC → POS                           PickConfirmedEvent (outbox)
SC → WMS                           ABBInvoiceSentToWMS (outbox) ← BEFORE dispatch
SC → TMS                           PickConfirmedSentToTMS (outbox)
TMS → SC                           PackageDispatched (webhook) → OutForDelivery
SC → GW                            OutForDeliveryNotified (outbox)
TMS → SC                           PackageDelivered (webhook) → Delivered
SC → GW                            DeliveredNotified (outbox)
STS → SC                           ABBTaxInvoiceReceived (webhook)
SC → GW                            TaxInvoiceForwarded (outbox)
```

### 2.3 Click & Collect Flow

```
Pending → BookingConfirmed → PickStarted → PickConfirmed → ReadyForCollection → Collected → Invoiced → Paid
```

`ReadyForCollection` is triggered by POS webhook `/webhooks/pos/pos-collection-ready`.  
`Collected` is triggered by POS webhook `/webhooks/pos/collected`.

### 2.4 Special Statuses

| Status | Meaning |
|---|---|
| `OnHold` | Manual hold (supervisor) or automatic hold (damaged goods). `pre_hold_status` saves the previous status and is restored on release. |
| `Cancelled` | Allowed from `Pending`, `BookingConfirmed`, or `OnHold` only. Triggers `OrderCancelledEvent` → WMS reverses stock reservation. |
| `ReadyForCollection` | Click & Collect only — order packed and waiting at store counter. |
| `Collected` | Customer picked up the order at the store. |
| `Returned` | Full or partial return processed. |

---

## 3. Use Cases

| UC | Name | Trigger | Key Flow |
|---|---|---|---|
| UC1 | Create Order | `POST /orders` | Customer creates outbound order; status → `Pending`; outbox → WMS/CBE |
| UC2 | Booking Confirmed | WMS webhook | WMS reserves stock; status → `BookingConfirmed` |
| UC3 | Pick Started | WMS webhook | Picker begins collecting; status → `PickStarted` |
| UC4 | Pick Confirmed + Packed | WMS webhooks | Picked qty recorded; POS recalc if needed; status → `PickConfirmed` → `Packed` |
| UC5 | Substitution | WMS webhook | WMS offers alternative SKU; customer approves/rejects; POS recalc triggered |
| UC6 | Hold / Release Hold | `PATCH /orders/{id}/hold`, `/release-hold` | Saves `pre_hold_status`; restores on release |
| UC7 | Out for Delivery | TMS webhook | Driver dispatched; status → `OutForDelivery`; customer notified |
| UC8 | Delivered | TMS webhook | Package delivered; invoice generation triggered |
| UC9 | Cancel Order | `PATCH /orders/{id}/cancel` | Allowed from `Pending`, `BookingConfirmed`, `OnHold` only |
| UC10 | Click & Collect Ready | POS webhook | Order ready at store; customer notified |
| UC11 | Collected | POS webhook | Customer collects; invoice triggered |
| UC12 | Invoiced | POS webhook | POS issues fiscal invoice; status → `Invoiced` |
| UC13 | Payment Confirmed | POS webhook | Payment received; status → `Paid` |
| UC14 | Return | `POST /returns` + WMS webhooks | ReturnRequested → PickupScheduled → PickedUp → Received → Inspected → PutAway → Refunded |
| UC15 | POS Recalculation | WMS/POS webhooks or `POST /orders/{id}/recalculate` | Sets `pos_recalc_pending = true`; POS returns adjusted amounts; flag cleared on completion |
| UC16 | Order Detail & Timeline | `GET /orders/{id}`, `/timeline` | Full order detail with event history across domain/webhook/outbox |
| UC17 | List Orders | `GET /orders` | Paginated list; filter by status, store, type |
| UC18 | List Packages | `GET /orders/{id}/packages` | Package and tracking info |
| UC19 | Delivery Slot | `GET/PATCH /orders/{id}/delivery-slot` | View or reschedule slot; not allowed after OutForDelivery |
| UC20 | Package Damaged | TMS webhook | Driver reports damage; status → `OnHold`; goods returned to warehouse |
| UC21 | Purchase Order | `POST/GET /inbound/purchase-orders` + WMS webhooks | PO Created → FullyReceived → Closed |
| UC22 | Transfer Order | `POST/GET /inbound/transfer-orders` + WMS webhooks | Created → PickConfirmed → InTransit → Received → Completed |
| UC23 | Damaged Goods Receipt | WMS webhooks | Damaged return checked in; items inspected; order placed OnHold |
| UC24 | Stock Ledger | `GET /stock/{sku}/ledger` | Read-only per-SKU stock movement view |
| UC25 | Time Slot Request (Prepaid) | Customer → GW → PS → TMS → CBE | Available delivery windows queried before order creation |
| UC26 | Delivery Booking (Prepaid) | Customer → GW → PS → TMS → CBE | Slot booked before sale order; OMS skips BookingConfirmed |
| UC27 | POS Recalculation (Prepaid) | WMS → SC → POS (mid-pick) | Repeats multiple times during picking; not limited to substitutions |
| UC28 | Pre-Delivery Invoice (Prepaid) | SC → WMS after PickConfirmed | ABB/Tax Invoice issued before TMS dispatch |
| UC29 | STS Tax Invoice Settlement | STS → SC → GW after Delivered | Official ABB/Tax Invoice forwarded to customer post-delivery |

---

## 4. Key Design Invariants

- **Monetary values** — stored as `decimal` in THB. Never use float for currency.
- **Timestamps** — ISO 8601 UTC (`timestamptz`) throughout. API responses use `Z` suffix.
- **Idempotency** — all inbound webhook handlers require `X-Idempotency-Key`. Duplicate keys are ignored and not reprocessed.
- **Outbox atomicity** — every domain event is written in the same DB transaction as the aggregate mutation. No event can be lost even if the background dispatcher crashes.
- **`pre_hold_status`** — must be saved before any `OnHold` transition and restored exactly on release. The `order_holds` log is append-only.
- **`pos_recalc_pending`** — must be `false` before the order can transition to `Packed`. The packing workflow is blocked while this flag is `true`.
- **`substitution_flag`** — set to `true` when any substitution record is created; never reset.
- **Cancellation guard** — only allowed from `Pending`, `BookingConfirmed`, or `OnHold`. Returns `409 invalid_transition` otherwise.
- **Delivery slot update guard** — slot cannot be changed once the order is `OutForDelivery` or later.
- **Error envelope** — all error responses use `{ "error": "<code>", "detail": "<message>" }`.
- **Bearer JWT** — required on all endpoints; 1-hour expiry; obtained via `POST /auth/token`.
- **`source_order_id`** — the external system's reference; used for idempotent order creation.
