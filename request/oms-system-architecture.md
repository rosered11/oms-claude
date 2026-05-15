# OMS System Architecture Decision

## Problem

Sprint Connect needs to design the full system architecture for its greenfield Order Management System (OMS). The system handles 70,000 order lines per day across multiple channels (Gateway, Marketplace, Kiosk, POSTerminal, BulkImport) and integrates with four external systems (WMS, TMS, POS, STS and etc..).

The architecture must support:
- A complex Order aggregate state machine
- DDD patterns: aggregate root, entities (Package, OrderLine, DeliverySlot), value objects, domain events
- CQRS: write model via Order aggregate, read model via projections and Redis cache
- Outbox pattern for reliable event delivery to external systems without a message broker
- ACL adapters per external integration (WmsAdapter, TmsAdapter, PosAdapter, StsAdapter)

## Context

- Technology: C# .NET 10, ASP.NET Core Web API, MySql
- Scale: ~70K order lines/day (~50K orders/day), peak ~3–5 orders/second
- Deployment: Kubernetes — 2 replicas for oms-api (stateless), 1 replica for oms-outbox-worker (single-writer)
- No message queue needed at this scale — Outbox polling worker calls ACL adapters directly via HTTP
- DB layout: 1 PostgreSQL instance, 4 schemas (orders, payment, returns, config), separate audit DB
- Security: JWT per channel, HMAC per integration, Vault for secrets

## Decision Already Made

The chosen architecture is a **Modular Monolith** with DDD + CQRS + Outbox + ACL. This was selected over microservices because:
1. Small team — microservices overhead not justified
2. Order lifecycle requires atomic transactions across Order/Payment/Returns
3. 70K order lines/day is well within single-instance PostgreSQL capacity
4. Clean module boundaries now enable future service extraction without rewriting integration contracts

## Constraints

- Must not use a message queue — Outbox pattern provides sufficient reliability at this scale
- All cross-module access is by ID only — no cross-schema joins
- Outbox worker must be single-writer to prevent duplicate event publishing
- State machine enforced in domain code, not DB config tables

## Feature is required

- OMS can handle multiple Business Unit like:
    - Manage the owner's workflow for each business unit, such as: an SSP can process pickconfirm at an external system and confirm product packing within our system etc.
- Dynamic Flow for outbox like: Merketplace Lazada must to call the Api A and Api B after pick confirm. but Tiktok maybe call the Api C.
- OMS Must to Support is minimum
    - Create Order
    - Cancel Order
    - Partial Pick
    - Reture Order
    - Rescheduler
    - เป็น case สั่งเนื่อกับไก่ แต่ลูกค้าเอาแค่ไก่ไม่เอาเนื้อ เพราะเนื้อไม่สด
    
    - Run Wave
    - Decline
    - RTS