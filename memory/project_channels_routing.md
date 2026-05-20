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
| GatewayA | `WaveStartedSentToGateway` | Yes | Has a row in `outbox_routing_rules` — wave start events forwarded |
| GatewayB | `WaveStartedSentToGateway` | No | No matching row — outbox worker skips dispatch silently |

Opt-in/opt-out is controlled entirely by the presence or absence of a row in `outbox_routing_rules`. This is the canonical pattern for per-Gateway feature flags.

**Implementation verified 2026-05-16.** Requirement: "After receiving WaveStarted webhook from WMS, OMS must send outbox to Gateway."
- `api/Features/Webhooks/Wms/WaveStarted.cs` — handler calls `adapterService.Dispatch(... "WaveStartedSentToGateway" ...)` after validating `order.Status == OrderStatus.PickStarted`.
- `InMemoryStore.SeedRoutingRules()` — routing rule seeded: `("Gateway", "*", "*", "WaveStartedSentToGateway", "Gateway", "Gateway.wave-started", 1)`.
- `InMemoryStore.SeedEndpointConfigs()` — endpoint config seeded: `"Gateway.wave-started"` → `{mock}/Gateway/api/status-update` (same endpoint as OutForDelivery and Delivered), auth: StaticToken via `x-api-key` header.
- State machine guard is correct: `WaveStarted` is rejected unless `order.Status == OrderStatus.PickStarted`.

**Payload fix 2026-05-16.** Requirement: "In WaveStartedEvent, request body not match with spec."
- Root cause: WaveStarted handler was forwarding raw inbound fields `{orderId, waveId, startedAt}` to Gateway instead of the standard Gateway status update format.
- Fix: `WaveStarted.cs` now calls `GatewayUpdateStatusPayload.Build(order, payment, "WAVE_STARTED")` — same payload builder as OutForDelivery and Delivered, with `order_status = "WAVE_STARTED"`.
- Fix: `GatewayUpdateStatusPayload.Build()` now accepts `orderStatus` parameter (default `"DELIVERED"`) to support reuse across multiple status transitions.
- Outbound payload to Gateway: `{order_id, sale_channel, sale_source, order_status: "WAVE_STARTED", updated_at, updated_by: "OMS", payments[]}` — identical shape to Integration 2 (OutForDelivery/Delivered).
- Routing rule scope is `"*"` (wildcard) — all channel types dispatch `WaveStartedSentToGateway`, consistent with `OutForDelivery` and `Delivered`. The `"Gateway"` channel type restriction was incorrect; it confused `channelType = "Gateway"` (an OMS channel value) with the Gateway external system.

---

## Multi-BU Workflow

Each Business Unit can have a different workflow for the same domain event. Per-channel dispatch targets are driven by `outbox_routing_rules`.

Example: an SSP business unit can process PickConfirm at an external system and confirm product packing within OMS — rather than OMS driving WMS directly.

---

## Business Unit Data Isolation

OMS enforces strict data isolation between Business Units. This is an application-layer invariant — not a database-level constraint.

**Rules:**
- The application layer filters all queries by `business_unit`. An operator with BU `CMG` cannot see or mutate orders belonging to BU `CFR`.
- Cross-BU mutations return `403 forbidden_business_unit`.
- Known isolated BUs: `CMG`, `CFR` (and others as new BUs onboard).

**How to apply:** Any new query, handler, or endpoint must include a `business_unit` filter. BU isolation is an application-level invariant; it does not require JWT for its correctness guarantee in the current implementation.

---

## API Authentication

**Removed as of 2026-05-19.** The API Blueprint no longer documents Bearer JWT authentication. The `POST /auth/token` endpoint, the top-level `**Auth:** Bearer JWT` metadata line, and all per-endpoint `**Auth:** Bearer JWT` annotations (previously scattered in the Configuration Management group) have been removed from `docs/oms-api-blueprint.md`.

The `Security` layer in `memory/project_architecture.md` still notes JWT per channel and HMAC-SHA256 per webhook integration as architectural features, but these are not reflected in the API Blueprint documentation.

---

## POD (Pay on Delivery) Routing

POD orders use `paymentMethod = 'POD'` on the order. This changes outbox routing for STS events:

| Event | Prepaid Target | POD Target |
|---|---|---|
| ABB/Tax Invoice from STS | WMS (`ABBTaxInvoiceSentToWMS`) + Gateway (`ABBTaxInvoiceSentToGateway`) | TMS (`ABBTaxInvoiceSentToTMS`) + Gateway (`ABBTaxInvoiceSentToGateway`) |
| Credit Note from STS | WMS (`CreditNoteSentToWMS`) + Gateway (`CreditNoteSentToGateway`) | TMS (`CreditNoteSentToTMS`) + Gateway (`CreditNoteSentToGateway`) |
| Invoice trigger | At PickConfirmed (pre-dispatch) | At Delivered (`DeliveredSentToPOS`) |
| PickStarted outbox | `PickStartedSentToTMS` → TMS | `PickStartedSentToTMS` → TMS |

### STS Invoice Forwarding (POD)
- Invoice link (URL to PDF) is forwarded — not just amount
- `ABBTaxInvoiceSentToTMS` carries `invoiceLink`
- `ABBTaxInvoiceSentToGateway` carries `invoiceLink`

### No WaveStarted in POD
POD flow does not include a WaveStarted step. GatewayA's WaveStarted opt-in routing rule does not apply to POD orders.

### STS Webhook Endpoint Pairs — Canonical vs Legacy
The blueprint contains two pairs of STS webhook endpoints:
- **Legacy (less detail):** `POST /webhooks/sts/abb-tax-invoice` and `POST /webhooks/sts/credit-note`
- **Canonical (full routing detail):** `POST /webhooks/sts/abb-tax-invoice-received` and `POST /webhooks/sts/credit-note-received`

The `-received` suffix variants are the authoritative endpoints. They contain the per-payment-method routing breakdown (Prepaid → WMS+Gateway, POD → TMS+Gateway) and the corrected field name (`amount` not `creditAmount`). When implementing or documenting STS webhook handling, always refer to the `-received` variants.

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
- **Pork** (หมู): 127 THB/kg; customer buys 841.23 g (0.84123 kg) → 106.84 THB (POS-rounded)
- **Duck** (เป็ด): sold in 2.5 kg packs at 99 THB/pack (39.60 THB/kg); customer buys 1.23 kg → 48.71 THB (POS-rounded)
- Combined total: 155.55 THB
- Both lines picked and packed together; `actualWeight` in pre-delivery recalc is the sum of both quantities

---

## Configuration-Driven Outbox Dispatch (implemented)

Routing is now fully driven by `config.outbox_routing_rules` at dispatch time. The outbox worker calls `InMemoryStore.GetRoutingRules(channelType, businessUnit, triggerEvent)` to resolve targets rather than applying hardcoded per-channel logic.

**Matching algorithm:**
- Exact match on `(channel_type, business_unit)` takes precedence over wildcard `*` entries.
- All rules whose `is_active = true` and that match the trigger are dispatched — one outbox event per matching rule.
- Results are processed in ascending `execution_order`.
- If no rule matches, no outbox event is dispatched. This is the intentional opt-out mechanism (absence of a row = no dispatch).

**Order handler pattern:**
- Handlers call `ApiResult.BuildOutboxEvents()` instead of hardcoded `ApiResult.OutboxEvent()` calls.
- `BuildOutboxEvents()` looks up matching routing rules and emits one outbox entry per rule, so a single domain transition can fan out to multiple targets if the configuration warrants it.

**Management endpoints (documented in `docs/oms-api-blueprint.md`, Group: Configuration Management):**

| Method | Path | Purpose |
|---|---|---|
| GET | `/config/outbox-routing-rules` | List all rules |
| GET | `/config/outbox-routing-rules/{ruleId}` | Get single rule |
| POST | `/config/outbox-routing-rules` | Create new rule |
| PUT | `/config/outbox-routing-rules/{ruleId}` | Replace rule |
| DELETE | `/config/outbox-routing-rules/{ruleId}` | Soft-delete (sets `is_active = false`) |

**When to apply:** Any time a new domain event needs to be routed to an external system, add a row to `outbox_routing_rules` rather than changing domain handler code.

---

## Known Use Cases

| Use case | Notes |
|---|---|
| Create Order | `POST /orders` dispatches two outbox events for all orders: `SaleOrderSentToWMS` → WMS and `SaleOrderSentToTMS` → TMS (for transport scheduling at creation time) |
| Cancel Order | Allowed from Pending, OnHold |
| Partial Pick | Picker takes fewer units than requested; `shortfall_reason` recorded; POS recalculates |
| Partial Item Return | Customer rejects subset of delivered items; triggers partial refund and CreditNoteSentToWMS |
| Return Order | Post-delivery return with full refund |
| Reschedule | Change delivery slot before OutForDelivery via Rescheduler capability |
| Customer keeps only part of order (e.g. chicken yes, beef no because not fresh) | Partial Item Return — customer rejects specific lines, OMS voids them, POS recalculates |
| Wave Start forwarding | WMS sends WaveStarted; OMS logs in order_webhook_logs; dispatches WaveStartedSentToGateway for opted-in Gateways |
| POD Order flow | payment_method='POD'; invoice triggered at Delivered not PickConfirmed; STS invoice/credit note routed to TMS+Gateway instead of WMS |
