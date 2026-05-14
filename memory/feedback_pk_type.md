---
name: feedback_pk_type
description: "PK type convention for OMS ER diagrams and schema — always use bigint, never uuid"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 1bb21d0c-0cc7-46b1-86f4-651a10827f28
---

Always use `bigint` as the primary key type in ER diagrams and any new schema definitions. Never use `uuid` for PKs (or FKs that reference them).

**Why:** The project migrated all PKs from UUID to bigint. Using UUID for new PKs would reintroduce an inconsistency the team deliberately eliminated.

**How to apply:** Any time a new entity, table, or ER diagram block is created, declare the PK as `bigint` and all FK columns referencing it as `bigint` as well.
