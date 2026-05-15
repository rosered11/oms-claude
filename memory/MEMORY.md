# Memory Index

- [Project Architecture](project_architecture.md) — Tech stack, scale, deployment, key architectural constraints and decisions for the OMS
- [Project Channels & Dynamic Routing](project_channels_routing.md) — Allowed channel types, dynamic outbox routing per marketplace and gateway, multi-BU workflow requirements, BU data isolation rules (CMG/CFR isolation, JWT-enforced filtering, 403 on cross-BU mutations), POD routing differences (invoice at Delivered, STS invoice/credit note to TMS+GW not WMS); configuration-driven dispatch via `config.outbox_routing_rules` (CRUD endpoints in `docs/oms-api-blueprint.md` under Group: Configuration Management)
- [PK Type Convention](feedback_pk_type.md) — Always use `bigint` for PKs and FKs in ER diagrams and schema; never uuid
- [POS Integration Architecture](pod_flow_pos_webhook_decision.md) — POS does NOT call OMS; OMS calls POS outbound for recalculations; all `/webhooks/pos/*` endpoints removed; POD terminal state is `Delivered`; STS ABB/Tax Invoice triggers after `Delivered` and routes to TMS + GW only
