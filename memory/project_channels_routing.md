---
name: OMS Channel Types & Dynamic Outbox Routing
description: Allowed channel_type values, dynamic outbox routing per marketplace/BU, multi-BU workflow requirements, and BU data isolation rules
type: project
originSessionId: bcac1f59-0cde-4d22-a547-3586e05d3541
---
## Channel Types

The `channel_type` field on `orders` determines which business rules and routing apply.

**Full allowed values:** `Gateway`, `Marketplace`, `Kiosk`, `POSTerminal`, `BulkImport`, `Web`, `App`, `POS`, `CallCenter`

**Why:** Extended from the original set (`Web`, `App`, `POS`, `CallCenter`) to cover all entry points described in `request/oms-system-architecture.md`. Marketplace channels (Lazada, TikTok, etc.) drive dynamic outbox routing.

**How to apply:** Always use these values when writing API examples, ER diagrams, or data dictionary entries for `channel_type`. Never invent new values without updating both the docs and this memory.

---

## Dynamic Outbox Routing

Different marketplace channels and business units may require calling different external APIs after the same domain event. Routing is driven by the `config.outbox_routing_rules` table keyed on `(channel_type, business_unit, trigger_event)` → `(target_system, endpoint_key, execution_order)`.

**Why:** Hard-coding per-channel outbox targets in domain code violates Open/Closed Principle and makes adding new marketplaces a code change. The routing table makes it configuration.

**How to apply:** When designing new outbox events, ask whether the target system is fixed (all channels → same target) or dynamic (per-channel routing). If dynamic, the outbox worker looks up `outbox_routing_rules` before dispatching.

### Multi-Marketplace Channel Routing

| Marketplace | Trigger Event | Target | endpoint_key | Notes |
|---|---|---|---|---|
| TikTok | `PickConfirmedSentToTMS` | `Marketplace` | `tiktok.pick-confirm` | TikTok API called via MarketplaceAdapter after pick confirmed |
| Lazada | `PackConfirmedSentToLazada` | `Marketplace` | `lazada.pack-confirm` | Lazada API called via MarketplaceAdapter when order is packed |

The `MarketplaceAdapter` ACL resolves `endpoint_key` to a real URL and handles marketplace authentication/payload transformation. Different marketplace BUs receive different events — routing is fully driven by the `outbox_routing_rules` table. No code change is needed to add a new marketplace.

### Multi-Gateway Routing

| Gateway | Event | Opted In | Notes |
|---|---|---|---|
| GatewayA | `WaveStartedSentToGW` | Yes | Has a row in `outbox_routing_rules` — wave start events forwarded |
| GatewayB | `WaveStartedSentToGW` | No | No matching row — outbox worker skips dispatch silently |

Opt-in/opt-out is controlled entirely by the presence or absence of a row in `outbox_routing_rules`. This is the canonical pattern for per-Gateway feature flags.

---

## Multi-BU Workflow

Each Business Unit can have a different workflow for the same domain event. This is governed by `fulfillment_routing_rules` (`requires_booking`, `requires_tms`, `initial_pick_status`) and extended by `outbox_routing_rules` for per-channel dispatch targets.

Example: an SSP business unit can process PickConfirm at an external system and confirm product packing within OMS — rather than OMS driving WMS directly.

---

## Business Unit Data Isolation

OMS enforces strict data isolation between Business Units. This is an application-layer invariant — not a database-level constraint.

**Rules:**
- The JWT claim `business_unit` is required on all API requests.
- The application layer automatically filters all queries by `business_unit` from the JWT. An operator with BU `CMG` cannot see or mutate orders belonging to BU `CFR`.
- Cross-BU mutations (attempting to write/update an order whose `business_unit` does not match the JWT claim) return `403 forbidden_business_unit`.
- Known isolated BUs: `CMG`, `CFR` (and others as new BUs onboard).

**How to apply:** Any new query, handler, or endpoint must include a `business_unit` filter derived from the JWT. Never allow a caller to pass `business_unit` as a query parameter — always read from the validated token.

---

## POD (Pay on Delivery) Routing

POD orders use `paymentMethod = 'POD'` on the order. This changes outbox routing for STS events:

| Event | Prepaid Target | POD Target |
|---|---|---|
| ABB/Tax Invoice from STS | WMS (`ABBInvoiceSentToWMS`) | TMS + GW (`ABBTaxInvoiceSentToTMS`, `ABBTaxInvoiceSentToGW`) |
| Credit Note from STS | WMS (`CreditNoteSentToWMS`) | TMS (`CreditNoteSentToTMS`) |
| Invoice trigger | At PickConfirmed (pre-dispatch) | At Delivered (`DeliveredSentToPOS`) |
| PickStarted outbox | (no TMS event) | `PickStartedSentToTMS` → TMS |

### STS Invoice Forwarding (POD)
- Invoice link (URL to PDF) is forwarded — not just amount
- `ABBTaxInvoiceSentToTMS` carries `invoiceLink`
- `ABBTaxInvoiceSentToGW` carries `invoiceLink`

### No WaveStarted in POD
POD flow does not include a WaveStarted step. GatewayA's WaveStarted opt-in routing rule does not apply to POD orders.

### TMS Recalculation Loop (POD)
After PickConfirmed, TMS may trigger additional POS recalculations (e.g. delivery fee adjustments) before dispatching. This loop ends when TMS sends `PackageDispatched`.

### Pre-Delivery Recalculation (POD — OutForDelivery → Delivered)
After `OutForDelivery`, TMS sends `POST /webhooks/tms/recalculation-requested` to notify OMS that the driver is at the door and final weights/quantities are confirmed. OMS calls POS outbound for a final price recalculation and records `PosRecalcCalled` in the order timeline (`RecalcRequested` event). This step happens **before** `PackageDelivered` and is POD-specific. Prepaid orders do not have this step.

- Webhook payload: `{ trackingId, reason, actualWeight?, requestedAt }`
- OMS response: `{ accepted: true, adjustedAmount: <number> }`
- Timeline event recorded: `PosRecalcCalled`
- For weight-based products: `actualWeight` carries the combined actual weight of all lines

### UC5: Weight-Based Dual-Product POD (CFR/Web)
Two weight-based fresh products in a single POD order:
- **Pork** (หมู): 127 THB/kg = 12700 satang/kg; customer buys 841.23 g (0.84123 kg) → 10684 satang (POS-rounded)
- **Duck** (เป็ด): sold in 2.5 kg packs at 99 THB/pack = 3960 satang/kg; customer buys 1.23 kg → 4871 satang (POS-rounded)
- Combined total: 15555 satang
- Both lines picked and packed together; `actualWeight` in pre-delivery recalc is the sum of both quantities

---

## Known Use Cases

| Use case | Notes |
|---|---|
| Create Order | Standard flow |
| Cancel Order | Allowed from Pending, BookingConfirmed, OnHold |
| Partial Pick | Picker takes fewer units than requested; `shortfall_reason` recorded; POS recalculates |
| Partial Item Return | Customer rejects subset of delivered items; triggers partial refund and CreditNoteSentToWMS |
| Return Order | Post-delivery return with full refund |
| Reschedule | Change delivery slot before OutForDelivery via Rescheduler capability |
| Customer keeps only part of order (e.g. chicken yes, beef no because not fresh) | Partial Item Return — customer rejects specific lines, OMS voids them, POS recalculates |
| Wave Start forwarding | WMS sends WaveStarted; OMS records in order_wave_events; dispatches WaveStartedSentToGW for opted-in Gateways |
| POD Order flow | payment_method='POD'; invoice triggered at Delivered not PickConfirmed; STS invoice/credit note routed to TMS+GW instead of WMS |
