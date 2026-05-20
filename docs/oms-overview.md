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
| CFW Gateway | Gateway | Customer-facing API gateway; forwards customer requests |
| Proxy Service | PS | Internal routing proxy between Gateway and backend systems |
| Sprint Connect | SC / OMS | This system — order orchestration hub |
| Warehouse System | WMS | Picks, packs, and stores goods |
| Transport System | TMS | Dispatches drivers and tracks deliveries |
| Point of Sale | POS | Calculates prices, applies promotions, issues invoices |
| Settlement Tax System | STS | Issues official ABB/Tax invoices post-PickConfirmed (Prepaid) or post-Delivered (POD) |

### 1.3 Integration Pattern

OMS integrates with external systems using two patterns:

- **Outbox** — domain events are written atomically with the order state change in the same DB transaction, then dispatched by a background worker to WMS/TMS/POS/Gateway.
- **Webhooks** — external systems POST callbacks to OMS inbound webhook endpoints. All webhook endpoints return `202 Accepted` immediately. Each handler stages an `OrderWebhookLog` entry atomically with the domain state change.

---

## 2. Order State Machine

### 2.1 Standard Flow (Post-Paid / Delivery)

```
Pending → PickStarted → PickConfirmed → Packed → OutForDelivery → Delivered
                                                                        ↓
                                                                    Returned  (via full return put-away)
```

| Transition | Trigger | Notes |
|---|---|---|
| `Pending` (initial) | `POST /orders` | |
| `PickStarted` | WMS webhook: `/webhooks/wms/pick-started` | |
| `WaveStarted` | WMS webhook: `/webhooks/wms/wave-started` | Internal event only — not a persisted status column. Valid only when order is in `PickStarted`. Triggers `WaveStartedSentToGateway` outbox event. Duplicate wave events are idempotent. |
| `PickConfirmed` | WMS webhook: `/webhooks/wms/pick-confirmed` | |
| `Packed` | WMS webhook: `/webhooks/wms/packed` | |
| `OutForDelivery` | TMS webhook: `/webhooks/tms/package-dispatched` | |
| `Delivered` | TMS webhook: `/webhooks/tms/package-delivered` | |

### 2.2 Prepaid Flow (Delivery — Slot Pre-Booked via TMS)

The prepaid flow differs from post-paid in three key ways:

1. **Direct Pending → PickStarted** — the delivery slot is booked directly via TMS before the sale order is created. OMS transitions from `Pending` directly to `PickStarted` (there is no intermediate booking-confirmed step in OMS).
2. **ABB/Tax Invoice issued after PickConfirmed** — STS sends the official ABB/Tax Invoice to SC after `PickConfirmed`, which SC forwards to WMS and Gateway before TMS dispatches.
3. **Optional Credit Note** — STS may issue a credit note after `PickConfirmed`. SC receives the webhook, stores the credit note, and forwards it to WMS and Gateway via outbox.

**Prepaid sequence:**

```
[Customer → Gateway → PS → TMS]   TimeSlotRequested (query available windows before order)
[Customer → Gateway → PS → TMS]   BookingCreated (slot locked)
[Customer → Gateway → PS → SC]    OrderCreated → Pending
SC → WMS                      SaleOrderSentToWMS (outbox)
SC → TMS                      SaleOrderSentToTMS (outbox)
WMS → SC                      PickStarted (webhook: /webhooks/wms/pick-started)
WMS → SC                      WaveStarted (webhook)
SC → Gateway                       WaveStartedSentToGateway (outbox)
[WMS → SC → POS]              POS Recalculation loop — OMS calls POS API (outbound); repeats as needed
WMS → SC                      PickConfirmed (webhook: /webhooks/wms/pick-confirmed)
SC → TMS                      PickConfirmedSentToTMS (outbox)
SC → Gateway                       PickConfirmedSentToGateway (outbox)
                               ↑ Gateway receives PickConfirmed → Gateway processes payment externally → STS notifies OMS
STS → SC                      ABBTaxInvoiceReceived (webhook) or CreditNoteReceived (webhook)
SC → WMS                      ABBTaxInvoiceSentToWMS (outbox)
SC → Gateway                       ABBTaxInvoiceSentToGateway (outbox)
[Optional] SC → WMS           CreditNoteSentToWMS (outbox)
[Optional] SC → Gateway            CreditNoteSentToGateway (outbox)
TMS → SC                      PackageDispatched (webhook: /webhooks/tms/package-dispatched) → OutForDelivery
SC → Gateway                       OutForDeliverySentToGateway (outbox)
TMS → SC                      PackageDelivered (webhook: /webhooks/tms/package-delivered) → Delivered
SC → Gateway                       DeliveredSentToGateway (outbox)
```

### 2.3 POD Flow (Pay on Delivery)

In the POD flow the customer pays at the point of delivery — cash or card. No advance invoice is issued before dispatch. STS issues the official ABB/Tax Invoice only after delivery is confirmed, and OMS forwards it to TMS and Gateway (not WMS, unlike Prepaid).

**POD sequence:**

| Step | Actor(s) | Event / Action |
|---|---|---|
| Slot query | Customer → Gateway → PS → TMS | TimeSlotRequested — available delivery windows queried |
| Slot locked | Customer → Gateway → PS → TMS | BookingCreated — delivery slot locked in TMS |
| Order created | Customer → Gateway → PS → SC | SaleOrder submitted; OMS creates order → `Pending` |
| Outbox to WMS | SC → WMS | `SaleOrderSentToWMS` outbox event |
| Outbox to TMS | SC → TMS | `SaleOrderSentToTMS` outbox event |
| Pick started | WMS → SC | `PickStarted` webhook: `/webhooks/wms/pick-started`; order → `PickStarted` |
| Wave started | WMS → SC | `WaveStarted` webhook |
| Outbox to Gateway | SC → Gateway | `WaveStartedSentToGateway` outbox event |
| POS recalc loop | WMS → SC → POS | OMS calls POS API (outbound) synchronously; repeats as needed before `PickConfirmed` |
| Pick confirmed | WMS → SC | `PickConfirmed` webhook: `/webhooks/wms/pick-confirmed`; records basket qty; partial pick supported |
| Outbox to TMS | SC → TMS | `PickConfirmedSentToTMS` outbox event (basket qty) |
| Outbox to Gateway | SC → Gateway | `PickConfirmedSentToGateway` outbox event |
| *(external)* | Gateway → external | Gateway receives `Delivered` status, handles COD/payment collection outside OMS |
| Dispatched | TMS → SC | `PackageDispatched` webhook: `/webhooks/tms/package-dispatched`; order → `OutForDelivery` |
| Outbox to Gateway | SC → Gateway | `OutForDeliverySentToGateway` outbox event |
| Pre-delivery recalc | TMS → SC | `RecalculationRequested` webhook: `/webhooks/tms/recalculation-requested`; OMS calls POS for final recalc; records `RecalcRequested` event in timeline |
| Delivered | TMS → SC | `PackageDelivered` webhook: `/webhooks/tms/package-delivered`; order → `Delivered` |
| Outbox to Gateway | SC → Gateway | `DeliveredSentToGateway` outbox event |
| *(external)* | Gateway → external → STS | Gateway receives `Delivered` status, processes externally; STS then sends invoice/credit note to OMS |
| ABB/Tax Invoice | STS → SC | `ABBTaxInvoiceReceived` webhook — issued after `Delivered` in POD |
| Outbox to TMS | SC → TMS | `ABBTaxInvoiceSentToTMS` outbox event (link to ABB/Tax Invoice) |
| Outbox to Gateway | SC → Gateway | `ABBTaxInvoiceSentToGateway` outbox event (link to ABB/Tax Invoice) |
| Credit Note (optional) | STS → SC | `CreditNoteReceived` webhook — only if credit note exists |
| Outbox to TMS (optional) | SC → TMS | `CreditNoteSentToTMS` outbox event |
| Outbox to Gateway (optional) | SC → Gateway | `CreditNoteSentToGateway` outbox event |

**Key behavioral differences vs. Prepaid:**

| Aspect | Prepaid | POD |
|---|---|---|
| WaveStarted step | Yes — WMS → SC → Gateway | Yes — WMS → SC → Gateway |
| Invoice timing | Before dispatch — STS issues ABB/Tax Invoice after `PickConfirmed` | After delivery — STS issues ABB/Tax Invoice after `Delivered` |
| ABB/Tax Invoice forwarded to | WMS + Gateway | TMS + Gateway |
| Credit Note forwarded to | WMS + Gateway | TMS + Gateway |

### 2.4 Click & Collect Flow

```
Pending → PickStarted → PickConfirmed → ReadyForCollection → Collected
```

`ReadyForCollection` is triggered by POS webhook `/webhooks/pos/pos-collection-ready`.  
`Collected` is triggered by POS webhook `/webhooks/pos/collected`.

### 2.5 Special Statuses

| Status | Meaning |
|---|---|
| `OnHold` | Manual hold (supervisor) or automatic hold (damaged goods). `pre_hold_status` saves the previous status and is restored on release. |
| `Cancelled` | Allowed from `Pending` or `OnHold` only. `PATCH /orders/{id}/cancel` transitions the order to `Cancelled` and dispatches three outbox events atomically: `OrderCancelledSentToWMS` (WMS reverses stock reservation), `OrderCancelledSentToTMS` (TMS cancels delivery booking), `OrderCancelledSentToGateway` (Gateway notifies customer). All three events appear on the order timeline. |
| `ReadyForCollection` | Click & Collect only — order packed and waiting at store counter. |
| `Collected` | Customer picked up the order at the store. |
| `Returned` | Terminal state. An order transitions from `Delivered` to `Returned` when WMS confirms put-away of a full return via `POST /webhooks/wms/put-away-confirmed`. This is distinct from the return record's own `PutAway` status — the **order** reaches `Returned`, and the **return record** reaches `PutAway`. Both transitions happen atomically in the same webhook handler. |

---

## 3. Use Cases

### 3.1 E2E Test Use Cases (UC1–UC14)

The following 14 use cases are exercised by the Cypress e2e test suite in `cypress/e2e/`. Each UC number matches its test file exactly.

| UC | E2E File | Description | Channel | BU | Payment | Terminal State |
|---|---|---|---|---|---|---|
| UC1 | `uc1-web-cmg-prepaid.cy.js` | Web / CMG / Prepaid full order flow: `Pending → PickStarted → PickConfirmed → Packed → OutForDelivery → Delivered`. STS ABB/Tax Invoice issued after PickConfirmed; forwarded to WMS + Gateway. | Web | CMG | Prepaid | Delivered |
| UC2 | `uc2-web-cfr-prepaid.cy.js` | Web / CFR / Prepaid full order flow: same as UC1 but for CFR business unit. Verifies BU data isolation. | Web | CFR | Prepaid | Delivered |
| UC3 | `uc3-tiktok-cmg-prepaid-awb.cy.js` | TikTok Marketplace / CMG / Prepaid. Standard Prepaid flow plus TikTok-specific AWB retrieval: after `OutForDelivery`, TikTok calls `GET /orders/{id}/packages` to retrieve the Air Waybill. OMS dispatches `OutForDeliveryEvent` outbox to Marketplace. | Marketplace (TikTok) | CMG | Prepaid | Delivered |
| UC4 | `uc4-web-cfr-pod.cy.js` | Web / CFR / POD full order flow: `Pending → PickStarted → PickConfirmed → Packed → OutForDelivery → (TMS pre-delivery recalc) → Delivered`. STS ABB/Tax Invoice issued after Delivered; forwarded to TMS + Gateway. | Web | CFR | POD | Delivered |
| UC5 | `uc5-web-cfr-pod-pork.cy.js` | Web / CFR / POD — weight-based fresh products (pork 841.23 g + duck 1.23 kg). POS-rounded pricing in baht: pork 106.84 + duck 48.71 = 155.55 total. TMS pre-delivery recalc confirms final actual weight before driver collects payment. | Web | CFR | POD | Delivered |
| UC6 | `uc6-web-cfr-partial-return.cy.js` | Web / CFR / POD — beef + chicken order, beef not fresh at delivery. Customer keeps chicken; returns beef (BEEF-KG, 0.5 kg, 175.00). `POST /returns` with `returnType: PartialItem`. Return status: `Requested`. | Web | CFR | POD | Delivered + Return Requested |
| UC7 | `uc7-stock-transfer.cy.js` | Stock transfer from Store A to Store B: `POST /inbound/transfer-orders → Created → WMS transfer-pick-confirmed → PickConfirmed → WMS transfer-received → Completed`. | Inbound | — | — | Completed |
| UC8 | `uc8-postpone-delivery.cy.js` | Customer postpones delivery slot (allowed from Pending). Second reschedule attempt while `OutForDelivery` returns `409 slot_change_not_allowed`. | Web | CFR | Prepaid | OutForDelivery (409 enforced) |
| UC9 | `uc9-cancel-order.cy.js` | OMS operator cancels a Pending order via the Kanban UI. `PATCH /orders/{id}/cancel` transitions to `Cancelled` and dispatches three outbox events: `OrderCancelledSentToWMS` (reverse stock reservation), `OrderCancelledSentToTMS` (cancel delivery booking), `OrderCancelledSentToGateway` (notify customer). Cancel from `PickStarted` returns `409 invalid_transition`. | Web | — | — | Cancelled |
| UC10 | `uc10-short-pick-decline.cy.js` | Short-pick: dish soap out of stock; only water delivered. `PATCH /orders/{id}/partial-pick` records soap line `shortfallQuantity: 1`. WMS packs water only. Final order `Delivered` with soap line `pickedQty: 0`. | Web | CFR | Prepaid | Delivered |
| UC11 | `uc11-substitution-refund.cy.js` | Substitution: fabric softener (89.00) → dish soap (45.00). Customer approves via `POST /orders/{id}/substitutions/{subId}/approve`. STS issues credit note for price difference (44.00). | Web | CFR | Prepaid | Delivered |
| UC12 | `uc12-full-return.cy.js` | Full return after delivery (CustomerRequest). `POST /returns → Requested`. `POST /webhooks/wms/put-away-confirmed` transitions return to `PutAway` and **transitions the linked order to `Returned`** (order status changes from `Delivered` → `Returned`). Refund initiated automatically. | Web | CFR | Prepaid | Returned |
| UC13 | `uc13-coupon-order.cy.js` | Prepaid order with coupon FRESH10 (10% PercentageDiscount). POS applies discount at recalculation; `adjustedAmount` reflects discount (e.g. 198.00 × 0.90 = 178.20). STS ABB/Tax Invoice for discounted amount. | Web | CFR | Prepaid | Delivered |
| UC14 | `uc14-prepaid-partial-return.cy.js` | Web / CFR / Prepaid — order for dish soap (×1) + water (×2). Full Prepaid flow to Delivered. Customer returns dish soap only (`returnType: PartialItem`). WMS `put-away-confirmed` transitions return to `PutAway` and initiates refund. Order remains `Delivered` (partial — water not returned). | Web | CFR | Prepaid | Delivered + Return PutAway |

### 3.2 Extended Use Case Reference

| UC | Name | Trigger | Key Flow |
|---|---|---|---|
| UC-WAVE | WaveStarted | WMS webhook: `/webhooks/wms/wave-started` | WMS starts wave picking while order is in `PickStarted`; SC forwards notification to Gateway via `WaveStartedSentToGateway` outbox event. Not a persisted status column. |
| UC-PARTPICK | Partial Pick | WMS webhook (pick-confirmed with partial quantities) | WMS picks fewer items than ordered (e.g. item out of stock or not fresh); OMS records partial quantities on order lines; remaining items are cancelled; POS recalc triggered. Partial pick cannot reduce all line quantities to zero — use Cancel Order instead. |
| UC-RESCHEDULE | Rescheduler | `POST /webhooks/tms/slot-rescheduled` | TMS notifies OMS of a customer-requested slot change. Not allowed after `OutForDelivery`; returns `409 slot_change_not_allowed` if attempted. |
| UC-PARTRETURN | Partial Item Return with Refund | Customer rejects items at delivery | Customer receives order but rejects one or more items at delivery (e.g. item not fresh). Rejected items are returned; OMS triggers partial refund via POS/STS. Only allowed after `Delivered` status. Each returned line item must reference the original order line. |
| UC-CREDITNOTE | Credit Note from STS (Prepaid) | STS webhook: `CreditNoteReceived` | STS issues credit note after `PickConfirmed`; SC receives webhook, stores credit note, forwards to WMS via `CreditNoteSentToWMS` and to Gateway via `CreditNoteSentToGateway` outbox events. Requires `X-Idempotency-Key`; duplicate events are ignored. |
| UC-POD-BRANCH | BranchNearMe (POD) | Customer → Gateway | Customer queries nearby branches via Gateway before booking a delivery slot. Gateway returns branch list from its own data source. No OMS involvement in this step. |
| UC-POD-PICKSTART | PickStarted (POD) | WMS webhook: `/webhooks/wms/pick-started` | WMS notifies OMS that picking has begun; OMS transitions order to `PickStarted`. |
| UC-POD-INVOICE | Post-Delivery Invoice (POD) | TMS webhook: `/webhooks/tms/package-delivered` | After order reaches `Delivered`, STS subsequently sends `ABBTaxInvoiceReceived` webhook; OMS forwards the invoice to TMS (`ABBTaxInvoiceSentToTMS`) and Gateway (`ABBTaxInvoiceSentToGateway`). |
| UC-POD-CREDITNOTE | Credit Note (POD) | STS webhook: `CreditNoteReceived` | Optional STS credit note issued after delivery. OMS receives webhook and dispatches `CreditNoteSentToTMS` outbox event to TMS and `CreditNoteSentToGateway` to Gateway. Requires `X-Idempotency-Key`; duplicates are ignored. |

---

## 4. Key Design Invariants

- **Monetary values** — stored as `decimal` in THB. Never use float for currency.
- **Timestamps** — ISO 8601 UTC (`timestamptz`) throughout. API responses use `Z` suffix.
- **Idempotency** — all inbound webhook handlers require `X-Idempotency-Key`. Duplicate keys are ignored and not reprocessed.
- **Outbox atomicity** — every domain event is written in the same DB transaction as the aggregate mutation. No event can be lost even if the background dispatcher crashes.
- **`pre_hold_status`** — must be saved before any `OnHold` transition and restored exactly on release. The `order_holds` log is append-only.
- **`substitution_flag`** — set to `true` when any substitution record is created; never reset.
- **Cancellation guard** — only allowed from `Pending` or `OnHold`. Returns `409 invalid_transition` otherwise.
- **Delivery slot update guard** — slot cannot be changed once the order is `OutForDelivery` or later.
- **Error envelope** — all error responses use `{ "error": "<code>", "detail": "<message>" }`.
- **Bearer JWT** — required on all endpoints; 1-hour expiry; obtained via `POST /auth/token`.
- **`source_order_id`** — the external system's reference; used for idempotent order creation.
- **WaveStarted gate** — the `WaveStarted` webhook is only valid when the order is in `PickStarted` status. Duplicate wave events are idempotent and do not re-trigger the outbox event.
- **Credit Note idempotency** — credit note webhooks from STS require `X-Idempotency-Key`. Duplicate credit note events are ignored and not reprocessed.
- **Partial Pick guard** — partial pick cannot reduce picked quantity to zero across all order lines. If all lines would reach zero, the order must be fully cancelled via Cancel Order instead.
- **Partial Return guard** — partial item return is only allowed after `Delivered` status. Each returned line item must reference the original order line ID.
- **STS routing** — ABB/Tax Invoice and Credit Note events from STS are routed based on `payment_flow`: for `PRE_PAID`, both WMS and Gateway receive the events; for `PAY_ON_DELIVERY`, both TMS and Gateway receive the events. Routing is enforced via `config.outbox_routing_rules` rows keyed on `(trigger_event, payment_flow)`.

---

## 5. Multi-BU and Multi-Channel Routing

The OMS supports multiple business units (BUs) and multiple channel types. Routing of outbox events is driven by the `config.outbox_routing_rules` table, keyed on `(channel_type, business_unit, trigger_event)`.

### Marketplace-Specific Routing

- **TikTok Marketplace**: Two outbox events are required:
  1. At `PickConfirmed` — OMS dispatches `PickConfirmedSentToTMS` via `tiktok.pick-confirm` to notify TikTok of the confirmed basket.
  2. At `OutForDelivery` — OMS dispatches `OutForDeliverySentToTikTok` via `tiktok.get-awb` to call TikTok's AWB API and retrieve the Air Waybill (shipment tracking number). This step is TikTok-specific; standard orders only send `OutForDeliverySentToGateway`.
- **Lazada Marketplace**: At `Packed` (PackConfirmed), OMS must send data to Lazada's API via the marketplace adapter.
- These are driven by `outbox_routing_rules` entries where `channel_type = 'Marketplace'` and `business_unit IN ('TikTok', 'Lazada')`.

### Gateway-Specific Routing

- **Gateway A**: Receives `WaveStartedSentToGateway` outbox event (opted in via routing rule).
- **Gateway B**: Does NOT receive wave status updates (no routing rule entry for WaveStarted).
- This is controlled by presence or absence of an `outbox_routing_rules` row for the `WaveStartedSentToGateway` trigger event per gateway business unit.

### Payment Flow as a Routing Dimension

`payment_flow` on the order is also a routing dimension evaluated by `config.outbox_routing_rules`. Specifically:

- `payment_flow = 'PAY_ON_DELIVERY'` — STS invoice events (`ABBTaxInvoiceReceived`, `CreditNoteReceived`) are routed to TMS and Gateway.
- `payment_flow = 'PRE_PAID'` — STS invoice events are routed to WMS and Gateway.

This is enforced via `outbox_routing_rules` rows keyed on `(trigger_event, payment_flow)`. No conditional logic in application code — routing is entirely table-driven.

### Business Unit Data Isolation

- Each BU (e.g., CMG, CFR) operates in strict isolation: an operator or system acting on behalf of CMG may only read and modify orders belonging to CMG.
- OMS enforces this at the application layer: every API request carries a `business_unit` claim in the Bearer JWT. The order query layer filters by `business_unit` and rejects cross-BU mutations with `403 forbidden_business_unit`.
- Example: CMG operators cannot view, cancel, or modify CFR orders, and vice versa.
