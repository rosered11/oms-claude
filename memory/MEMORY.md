# Memory Index

- [Project Architecture](project_architecture.md) — Tech stack, scale, deployment, key architectural constraints and decisions for the OMS
- [Project Channels & Dynamic Routing](project_channels_routing.md) — Allowed channel types, dynamic outbox routing per marketplace and gateway, multi-BU workflow requirements, BU data isolation rules (CMG/CFR isolation, JWT-enforced filtering, 403 on cross-BU mutations)
- [PK Type Convention](feedback_pk_type.md) — Always use `bigint` for PKs and FKs in ER diagrams and schema; never uuid
