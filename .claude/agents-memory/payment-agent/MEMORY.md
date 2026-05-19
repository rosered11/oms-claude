# Payment Agent Memory Index

- [Payment Flow Routing Decision](decision_payment_flow_routing.md) — PRE_PAID vs PAY_ON_DELIVERY drives invoice timing and outbox targets; routing is table-driven via config.outbox_routing_rules, never conditional code
- [STS Invoice and Credit Note Idempotency](decision_sts_idempotency.md) — Idempotency for STS webhooks uses DB-level UNIQUE constraints, not application-layer pre-checks
- [STS Gateway Error Mappings](mapping_sts_gateway_errors.md) — How STS/gateway response states map to domain payment statuses
- [Return Refund Flow Decisions](decision_return_refund_flow.md) — Refund calculation from return_items.unit_price × quantity; put-away is the atomic trigger; no re-authorisation
