# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Repository Is

This is a **design-first documentation repository** for the Sprint Connect Order Management System (OMS) — a modular monolith planned in .NET. There is no compiled application code here yet. The repository contains:

- `input/` — Authoritative domain specifications (API blueprint, ER diagrams, data dictionary, field mappings)
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

## Domain Architecture

The system is a **modular monolith** with five bounded contexts, each owning its own MySQL schema:

| Context | Schema | Core responsibility |
|---|---|---|
| Order | `orders.*` | Order lifecycle, lines, packages, delivery slots |
| Payment | `payment.*` | Invoices, transactions, credit notes, fees, promotions |
| Returns | `returns.*` | Return requests, conditions, put-away, refunds |
| Inbound | `inbound.*` | Purchase orders, transfer orders, goods receipts |
| Configuration | `config.*` | Stores, business units, rollout policies |

**Cross-context communication** uses the **Outbox pattern** — domain events are staged atomically with aggregate mutations and dispatched by a background worker to WMS, TMS, and POS systems.

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

## Key Reference Files

- `input/oms-api-blueprint.md` — 36+ REST endpoints in Apiary FORMAT 1A; canonical API spec
- `input/oms-er-diagrams.md` — Mermaid ER diagrams and service architecture decisions
- `input/oms-data-dictionary.md` — plain-language definitions for every table and column
- `input/oms-api-field-mapping.md` — maps every API field to its database column

## Web UI Prototype

`web-ui/index.html` is a self-contained React SPA (CDN imports, no build step). Open directly in a browser. It has three views:

- **Kanban Board** — order cards across status columns using sample `data/orders.json`
- **Order Timeline** — chronological domain/webhook/outbox events
- **Stock Flow** — multi-location stock movement (inbound POs → available → picks)

The prototype uses hardcoded local JSON; it is not wired to any backend.
