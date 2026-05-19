---
name: Payment Flow Routing — PRE_PAID vs PAY_ON_DELIVERY
type: decision
---

The `orders.payment_flow` column (VARCHAR 50) is the single routing discriminator for all STS invoice and credit note events. It has two values:

- `"PRE_PAID"` — invoice triggers after `PickConfirmed`; forwarded to WMS + Gateway
- `"PAY_ON_DELIVERY"` — invoice triggers after `Delivered`; forwarded to TMS + Gateway

This routing is 100% table-driven via `config.outbox_routing_rules`. The outbox worker evaluates `(channel_type, business_unit, trigger_event)` plus `payment_method` at dispatch time. No conditional if/else logic exists in application code for payment routing.

**Why:** Adding a new payment flow or changing which system receives an invoice requires only a row change in `config.outbox_routing_rules`, not a code deployment.

**When to apply:** Any time you are implementing or reviewing code that dispatches STS invoice or credit note events. If you see an `if paymentMethod == "Prepaid"` branch in non-configuration code, that is an ACL violation — move it to a routing rule.

**Key columns involved:**
- `orders.payment_flow` — drives routing
- `orders.payment_method` — dimension evaluated at dispatch (Prepaid / POD / COD)
- `config.outbox_routing_rules.trigger_event` — event names: `ABBTaxInvoiceSentToWMS`, `ABBTaxInvoiceSentToTMS`, `ABBTaxInvoiceSentToGateway`, `CreditNoteSentToWMS`, `CreditNoteSentToTMS`, `CreditNoteSentToGateway`
