# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Repository Is

This repository contains the Sprint Connect Order Management System (OMS) — a modular monolith in .NET 10. It contains:

- `docs/` — Authoritative domain specifications (API blueprint v2.0, ER diagrams, data dictionary, field mappings, ubiquitous language)
- `api/` — ASP.NET Core Web API (.NET 10) — in-memory implementation of all OMS endpoints
- `web-ui/` — A standalone React+Tailwind prototype using local JSON files (no live API connection)
- `.claude/agents/` — Specialized domain agents for DDD review, documentation, and feature development

## Specialized Agents

Seven domain agents are configured in `.claude/agents/`. Invoke them for domain-specific work:

| Agent | Use for |
|---|---|
| `OrderOrchestrator` | Order lifecycle, Saga coordination, cross-context event flow |
| `FulfillmentAgent` | Delivery slots, WMS integration, shipment tracking |
| `InventoryAgent` | Stock reservation, release, commitment, oversell prevention |
| `PaymentAgent` | Payment auth/capture/refund, gateway ACL design |
| `NotificationAgent` | Event-to-notification mapping, idempotent delivery |
| `DDDReviewer` | Aggregate boundary leaks, anemic model, invariant gaps |
| `DocumentationAgent` | API Blueprint, ER diagrams, ubiquitous language glossary |

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | C# .NET 10, ASP.NET Core Web API |
| Database | MySQL — 4 schemas (`orders`, `payment`, `returns`, `config`) + separate audit DB |
| Cache | Redis — read model projections only (CQRS query side) |
| Deployment | Kubernetes — 2 replicas `oms-api` (stateless), 1 replica `oms-outbox-worker` (single-writer) |
| Security | JWT per channel, HMAC-SHA256 per webhook integration, HashiCorp Vault for secrets |

**Scale:** ~70,000 order lines / day (~50,000 orders), peak 3–5 orders/second.

## Domain Architecture

The system is a **modular monolith** with five bounded contexts, each owning its own MySQL schema:

| Context | Schema | Core responsibility |
|---|---|---|
| Order | `orders.*` | Order lifecycle, lines, packages, delivery slots |
| Payment | `payment.*` | Invoices, transactions, credit notes, fees, promotions |
| Returns | `returns.*` | Return requests, conditions, put-away, refunds |
| Inbound | `inbound.*` | Purchase orders, transfer orders, goods receipts |
| Configuration | `config.*` | Stores, business units, rollout policies, outbox routing rules |

**Cross-context communication** uses the **Outbox pattern** — domain events are staged atomically with aggregate mutations and dispatched by the `oms-outbox-worker` (single-writer, 1 replica) to external systems via ACL adapters: `WmsAdapter`, `TmsAdapter`, `PosAdapter`, `StsAdapter`. No message broker is used.

**Cross-module access:** by ID only — no cross-schema JOINs anywhere in the codebase.

**Dynamic outbox routing:** the `config.outbox_routing_rules` table maps `(channel_type, business_unit, trigger_event)` to target systems and endpoint keys. Different marketplace channels (Lazada, TikTok, etc.) can route the same domain event to different APIs.

## Order Channels

The `channel_type` field determines business rules and routing. Allowed values:

`Gateway` · `Marketplace` · `Kiosk` · `POSTerminal` · `BulkImport` · `Web` · `App` · `POS` · `CallCenter`

## Order State Machine

The core state machine (enforced as an invariant — no ad-hoc status writes):

```
Pending → BookingConfirmed → PickStarted → PickConfirmed → Packed →
OutForDelivery → Delivered → Invoiced → Paid
                ↕                    ↕
              OnHold              Cancelled
```

Key invariants:
- `pos_recalc_pending = true` blocks packing (POS must confirm final price first)
- `pre_hold_status` is saved before transitioning to `OnHold` and restored on resume
- Substitutions (`substitution_flag`) require customer consent before picking alternate SKUs

## Key Conventions

- **Monetary values** — always in smallest currency unit (satang for THB)
- **Timestamps** — ISO 8601 UTC throughout
- **Idempotency** — all inbound webhook handlers require an idempotency key
- **Error envelope** — consistent `{ error_code, message, trace_id }` shape on all errors
- **Bearer JWT** — required on all endpoints except public auth
- **`source_order_id`** — the external system's reference; used for idempotent order creation

## Memory

All persistent memory for this project lives in `memory/` at the root of this repository and is tracked in Git. **Never write memory to `~/.claude/` or any other location.** Always read from and write to `memory/` only.

| File | Contents |
|---|---|
| `memory/MEMORY.md` | Index — list every memory file here when you add one |
| `memory/project_architecture.md` | Tech stack, scale, deployment, key architectural decisions |
| `memory/project_channels_routing.md` | Channel types, dynamic outbox routing, multi-BU workflow |
| `memory/feedback_pk_type.md` | PK/FK type convention (bigint, never uuid) |

When you learn something worth remembering (a decision, a constraint, a correction), write it to an appropriately named file under `memory/` and add it to the index in `memory/MEMORY.md`.

## Key Reference Files

- `docs/oms-api-blueprint.md` — REST endpoints (canonical API spec, v2.0)
- `docs/oms-overview.md` — System overview, use cases, state machine, design invariants
- `docs/oms-er-diagrams.md` — Mermaid ER diagrams and service architecture decisions
- `docs/oms-data-dictionary.md` — Plain-language definitions for every table and column
- `docs/oms-api-field-mapping.md` — Maps every API field to its database column
- `docs/oms-ubiquitous-language.md` — Domain glossary

## Web UI Prototype

`web-ui/index.html` is a self-contained React SPA (CDN imports, no build step). Open directly in a browser. It has three views:

- **Kanban Board** — order cards across status columns using sample `data/orders.json`
- **Order Timeline** — chronological domain/webhook/outbox events
- **Stock Flow** — multi-location stock movement (inbound POs → available → picks)

The prototype uses hardcoded local JSON; it is not wired to any backend.
