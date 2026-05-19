---
name: Return Refund Flow — Calculation, Atomicity, No Re-authorisation
type: decision
---

**Refund amount calculation:**

`returns.return_refunds.refund_amount = SUM(return_items.unit_price × return_items.quantity)`

`returns.return_items.unit_price` is copied from `orders.order_lines.original_unit_price` at return creation time. This is intentional — the refund is based on what the customer was actually charged, not any current catalogue price.

**Atomic trigger:**

`POST /webhooks/wms/put-away-confirmed` is the single atomic trigger that writes ALL of the following in one DB transaction:
1. `returns.returns.status = "PutAway"`
2. `returns.return_items` — condition, assigned_sloc, put_away_status updated
3. `returns.return_put_away_logs` INSERT (audit row per item)
4. `orders.orders.status = "Returned"` (order transitions from Delivered → Returned)
5. `returns.return_refunds` INSERT (status = Pending)

If any write fails, the whole transaction rolls back. The order stays Delivered and the return stays in its pre-putaway state.

**No re-authorisation:**

Refunds always operate against the original payment record. They are reverse transactions on the original authorisation/capture. Never issue a new payment authorisation as part of a refund flow.

**Partial return rule:**

A partial return creates one `returns.return_items` row per returned line. Lines not returned are absent from the return items. `returns.returns.invoice_id` still points to the full original invoice — the credit note covers only the partial amount.

**Return state machine:**
Requested → PickupScheduled → PickedUp → Received → Inspected → PutAway → Refunded

The order state transitions from Delivered → Returned only at `PutAway` (not at `Requested`).

**When to apply:** Any time you implement or review put-away-confirmed handler, return creation, or refund dispatch logic.
