---
name: OMS System Architecture
description: Tech stack, scale, deployment topology, and key architectural constraints for the Sprint Connect OMS
type: project
originSessionId: bcac1f59-0cde-4d22-a547-3586e05d3541
---
The chosen architecture is a **Modular Monolith** with DDD + CQRS + Outbox + ACL adapters. Microservices were rejected because the team is small, the order lifecycle requires atomic transactions across Order/Payment/Returns, and 70K order lines/day is well within single-instance MySQL capacity.

**Why:** Architectural decision recorded in `request/oms-system-architecture.md`. Clean module boundaries now enable future service extraction without rewriting integration contracts.

**How to apply:** Always design within the modular monolith constraint. Do not propose message queues, distributed transactions, or cross-schema JOINs. Any new external integration must go through an ACL adapter.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | C# .NET 10, ASP.NET Core Web API |
| Database | MySQL (single instance, 4 schemas: orders, payment, returns, config) + separate audit DB |
| Cache | Redis — read model projections only (CQRS query side) |
| Deployment | Kubernetes — 2 replicas for `oms-api` (stateless), 1 replica for `oms-outbox-worker` (single-writer) |
| Security | JWT per channel, HMAC-SHA256 per integration, HashiCorp Vault for secrets |

> **Note:** The architecture source doc contains an inconsistency — "MySql" in the tech list but "PostgreSQL" in the DB layout paragraph. CLAUDE.md is authoritative: **MySQL**.

---

## Scale

- ~70,000 order **lines** per day
- ~50,000 **orders** per day
- Peak throughput: **3–5 orders/second**

---

## Deployment Constraints

- `oms-api`: stateless, 2 replicas, scales horizontally
- `oms-outbox-worker`: **single-writer** — must run as exactly 1 replica to prevent duplicate event publishing
- No message broker (Kafka, RabbitMQ, etc.) — Outbox polling worker calls ACL adapters directly via HTTP; sufficient at this scale
- Cross-module access by **ID only** — no cross-schema JOINs anywhere

---

## ACL Adapters (Anti-Corruption Layer)

One adapter per external system. Domain code never calls HTTP directly.

| Adapter | External System |
|---|---|
| `WmsAdapter` | WMS |
| `TmsAdapter` | TMS |
| `PosAdapter` | POS |
| `StsAdapter` | Settlement & Tax System (STS) |
