---
name: Outbox Dispatch Paths — ApiResult.BuildOutboxEvents vs OutboxAdapterService.Dispatch
type: decision
date: 2026-05-18
source: bug fix in PickConfirmedHandler; api/Shared/ApiResult.cs; api/Infrastructure/OutboxAdapterService.cs
---

## Rule

Any handler whose outbox events must appear in the outbox monitor (i.e. visible in `GET /outbox/dispatch-logs`) **must** call `OutboxAdapterService.Dispatch`. Using `ApiResult.BuildOutboxEvents` in that context silently omits the dispatch log — no `OutboxDispatchLog` record is created, no HTTP call is made, and the event is invisible to the outbox monitor and the retry mechanism.

---

## Two Dispatch Paths

### 1. `OutboxAdapterService.Dispatch` — full outbox path

```csharp
// inject: OutboxAdapterService adapterService
var payload = JsonSerializer.Serialize(GatewayUpdateStatusPayload.Build(o, payment, "PICK_CONFIRMED"));
foreach (var evt in adapterService.Dispatch(orderId, channelType, subChannel, businessUnit,
    "PickConfirmedEvent", payload))
    store.AppendEvent(orderId, evt);
```

**What it does:**
- Looks up matching `outbox_routing_rules` rows for `(channelType, subChannel, businessUnit, triggerEvent)`.
- For each matched rule, resolves the `OutboxEndpointConfig` by `endpoint_key`.
- Authenticates (StaticToken or OAuth2ClientCredentials) and makes the actual HTTP POST.
- Writes an `OutboxDispatchLog` entry with status, HTTP code, request/response payloads, attempt count, and timestamps.
- Appends a `TimelineEventDto` (phase: `outbound`, type: `outbox`) for the order timeline.
- Failed dispatches are retryable via `POST /outbox/dispatch-logs/{logId}/retry`.

**When to use:** Any event that must reach an external system (WMS, TMS, Gateway, Marketplace, STS). Required whenever the dispatch needs to be auditable, monitored, or retried.

---

### 2. `ApiResult.BuildOutboxEvents` — lightweight timeline annotation path

```csharp
// no service injection needed — static helper
foreach (var evt in ApiResult.BuildOutboxEvents(store, channelType, subChannel, businessUnit,
    "OrderCreatedEvent", $"SC → {{target}}: Sale Order {num}"))
    store.AppendEvent(id, evt);
```

**What it does:**
- Looks up matching `outbox_routing_rules` rows (same matching logic).
- For each matched rule, creates a `TimelineEventDto` only — no HTTP call, no `OutboxDispatchLog`.
- The event appears in the order timeline UI but is invisible in the outbox monitor.

**When to use:** Only for internal timeline annotation where no actual external call is needed. Currently used only in `CreateOrder` to record the outbound intent. Do **not** use this path for any event that needs to be delivered to an external system.

---

## Bug Reference — PickConfirmedHandler (fixed 2026-05-18)

`PickConfirmedHandler` was incorrectly using `ApiResult.BuildOutboxEvents`. This meant `PickConfirmedEvent` dispatches were written only as order timeline events, not as `OutboxDispatchLog` entries. The outbox monitor showed no record of these dispatches, and the Gateway/TMS calls were never made.

**Fix applied:**
- `api/Features/Webhooks/Wms/PickConfirmed.cs` — handler now injects `OutboxAdapterService`, builds a `GatewayUpdateStatusPayload` with status `"PICK_CONFIRMED"`, and calls `adapterService.Dispatch`.
- `api/Infrastructure/InMemoryStore.cs` — wildcard routing rule added: `("*", "*", "*", "PickConfirmedEvent", "Gateway", "Gateway.pick-confirm", 2)`.
- `api/Infrastructure/InMemoryStore.cs` — endpoint config `"Gateway.pick-confirm"` seeded, pointing to `{mock}/Gateway/api/status-update` with `StaticToken` auth.

---

## Quick Decision Guide

| Requirement | Use |
|---|---|
| External system must receive an HTTP call | `OutboxAdapterService.Dispatch` |
| Event must appear in outbox monitor (`/outbox/dispatch-logs`) | `OutboxAdapterService.Dispatch` |
| Retry on failure is needed | `OutboxAdapterService.Dispatch` |
| Timeline annotation only (no external call) | `ApiResult.BuildOutboxEvents` |

---

## Supporting Infrastructure Checklist

When adding a new event dispatched via `OutboxAdapterService.Dispatch`, ensure:

1. A routing rule row exists in `InMemoryStore.SeedRoutingRules()` matching `(channelType, subChannel, businessUnit, triggerEvent)` — or a wildcard `("*", "*", "*", ...)` fallback.
2. An endpoint config exists in `InMemoryStore.SeedEndpointConfigs()` for every `endpoint_key` referenced by matching rules.
3. The handler injects `OutboxAdapterService` (not just `InMemoryStore`).
4. The payload is serialized before passing to `Dispatch` — the method accepts `string requestPayload`, not an object.
