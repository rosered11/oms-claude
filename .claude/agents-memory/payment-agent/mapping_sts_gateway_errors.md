---
name: STS / Gateway Error Code to Domain Payment Status Mapping
type: mapping
---

When STS sends an invoice or credit note webhook, the `is_success` field in the outbox payload (Integration 3) is computed from `payment.invoices.status`:

| `payment.invoices.status` | `documents[].is_success` (outbox payload to TMS/WMS) | `documents[].url` |
|---|---|---|
| `"Issued"` | `true` | `invoices.invoice_link` |
| Any other value | `false` | `""` (empty string) |

When `payment.order_payments.status` is mapped for the Gateway delivery payload (Integration 2):

| `payment.order_payments.status` | `payments[].payment_status` (outbox payload to Gateway) |
|---|---|
| `"Captured"` | `"PAID"` |
| Any other value | `"UNPAID"` |

When `payment.order_payments.payment_method` is mapped for the Gateway delivery payload:

| `order_payments.payment_method` | `payments[].payment_method` | `payments[].tendor` |
|---|---|---|
| `CreditCard` | `CREDIT_CARD` | `WCRD` |
| `QRCode` | `QR_CODE` | `QRPP` |
| `PayOnDelivery` | `POD` | `WCOD` |
| Any other | `POD` (fallback) | `WCOD` (fallback) |

**When to apply:** When implementing Gateway, TMS, or WMS outbox payload builders for any payment-related event.
