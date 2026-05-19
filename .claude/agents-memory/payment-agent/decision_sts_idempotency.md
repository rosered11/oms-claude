---
name: STS Webhook Idempotency — DB UNIQUE Constraints, Not Application Checks
type: decision
---

All STS webhook idempotency is enforced via database-level UNIQUE constraints, not application-layer "already exists?" queries.

**For credit notes:**
- `payment.credit_notes.idempotency_key` has a UNIQUE constraint
- Value is set from the incoming `X-Idempotency-Key` header on `POST /webhooks/sts/credit-note-received`
- Duplicate INSERT fails the constraint → OMS returns 409 conflict

**For ABB/Tax invoices:**
- `payment.invoices.source_sts_ref` stores the STS-assigned reference
- `payment.invoices.invoice_number` is a UNIQUE key
- Duplicate submission: OMS checks whether a row already exists for the `invoiceNumber` before inserting, returns 409 conflict

**For WMS/TMS webhooks generally:**
- `orders.order_webhook_logs.idempotency_key` is UNIQUE
- Covers all wave-started, pick-confirmed, delivered, etc. events

**Idempotency key format:** UUID — provided by the calling system in `X-Idempotency-Key` header.

**Why:** Application-layer pre-checks ("SELECT then INSERT") are vulnerable to race conditions under concurrent retries. UNIQUE constraints at the DB level provide race-safe deduplication.

**When to apply:** When implementing any handler for STS/WMS/TMS inbound webhooks. Always rely on the constraint, not a prior SELECT.

**Known bug fixed:** The `credit-note-received` webhook previously used field name `creditAmount` in the request body. This was corrected to `amount`. If you see `creditAmount = 0` bugs in old code, the fix is to read `amount` instead.
