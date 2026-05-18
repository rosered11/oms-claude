---
name: POS Integration Architecture — OMS Calls POS; POS Never Webhooks OMS
type: adr
date: 2026-05-14
source: request/sequence-diagram-pod.md + team clarification 2026-05-14
---

# POS Integration: OMS → POS (outbound only); No `/webhooks/pos/*` Endpoints

## Core Rule

**POS does not call OMS.** POS exposes an API that OMS calls (outbound). There are no
inbound `/webhooks/pos/*` endpoints in the OMS API. All `/webhooks/pos/*` entries have
been removed from the API blueprint and from all E2E tests.

## Recalculation Flow (Revised)

OMS calls the POS recalculation API directly when WMS requests a recalculation:

```
WMS → SC:  POST /webhooks/wms/recalculation-requested
SC  → POS: [outbound] GET/POST POS recalculation API  (OMS is the client)
SC:        receives POS response, applies adjusted amount, clears posRecalcPending
SC returns 202 with posRecalcPending = false
```

There is no callback from POS to OMS. The recalculation is resolved synchronously
within the OMS webhook handler.

## E2E Test Impact

All six E2E tests (UC1–UC6) have been updated:
- `POST /webhooks/pos/pos-recalc-completed` calls removed from Step 4
- After `recalculation-requested`, the assertion is `posRecalcPending: false`
- `POST /webhooks/pos/invoiced` removed from Prepaid tests (UC1/UC2/UC3)
- `POST /webhooks/pos/payment-confirmed` removed from Prepaid tests (UC1/UC2/UC3)

## Terminal State for All Flows

With POS webhooks removed:
- **Prepaid** terminal state in OMS: `Delivered` (Invoiced/Paid transitions require a
  non-POS mechanism — pending design)
- **POD** terminal state in OMS: `Delivered` (unchanged)

---

# (Previous) POD Flow: Terminal State is `Delivered` — POS Webhooks Are NOT Called

## Context

The API blueprint §2.3 (docs/oms-api-blueprint.md) documents a state machine that
continues past `Delivered` through `Invoiced` (via `POST /webhooks/pos/invoiced`) and
`Paid` (via `POST /webhooks/pos/payment-confirmed`). The STS webhooks section of the
blueprint also documents the POD ABB/Tax Invoice trigger point and routing targets
correctly. A customer-confirmed sequence diagram was provided in
`request/sequence-diagram-pod.md` to clarify the POD end-to-end flow.

## Problem

Does the POD flow call `POST /webhooks/pos/invoiced` and
`POST /webhooks/pos/payment-confirmed` to advance the order to `Invoiced` and `Paid`?
Or does the POD order lifecycle terminate at `Delivered`?

## Decision

The **sequence diagram is the customer-confirmed source of truth** for the POD flow.
The sequence diagram ends at `Delivered` with no subsequent POS invoice or payment
webhook steps. The POS invoice/payment webhook path applies to non-POD channels
(e.g. Prepaid, Kiosk, in-store collection flows) — not to POD.

## Confirmed POD Terminal State and Post-Delivery Steps

### 1. POD terminal order status is `Delivered`

The POD order lifecycle ends at status `Delivered`. There is no transition to
`Invoiced` or `Paid` for POD orders. The sequence:

```
TMS → SC: Delivered [inbound webhook /tms/package-delivered]
SC → Gateway: Delivered
Order status → Delivered   ← terminal state for POD
```

### 2. STS ABB/Tax Invoice is triggered after `Delivered` (not after `PickConfirmed`)

For POD orders, STS sends the ABB/Tax Invoice webhook **after the order reaches
`Delivered`** status. This contrasts with Prepaid, where the STS invoice arrives
after `PickConfirmed` and before TMS dispatch.

The sequence diagram note reads:
> "Webhook ABB/Tax Invoice [link ABB/Tax Invoice] trigger after Pick Confirm in case POD"

The phrase "Pick Confirm" here refers to the broader POD delivery confirmation
(package delivered), not the WMS PickConfirmed event. The routing table in the
API blueprint confirms the trigger point: "After `Delivered`" for POD.

### 3. ABB/Tax Invoice routing for POD: TMS + Gateway (NOT WMS + Gateway)

When STS sends the ABB/Tax Invoice for a POD order, OMS forwards it to:

- TMS (Transport Management System)
- Gateway (CFW Gateway)

It is NOT forwarded to WMS. This differs from Prepaid, which routes to WMS + Gateway.

Domain events dispatched for POD ABB/Tax Invoice:
- `ABBTaxInvoiceSentToTMS` → TMS
- `ABBTaxInvoiceSentToGateway` → Gateway

### 4. Credit Note routing for POD (optional step): TMS + Gateway

If a credit note exists (e.g. price adjustment after partial pick), STS sends a
separate Credit Note webhook. For POD orders this is also forwarded to TMS + Gateway only.

Domain events dispatched for POD Credit Note:
- `CreditNoteSentToTMS` → TMS
- `CreditNoteSentToGateway` → Gateway

### 5. `POST /webhooks/pos/invoiced` and `POST /webhooks/pos/payment-confirmed` are NOT called in the POD flow

These two endpoints:
- `POST /webhooks/pos/invoiced`   → transitions order to `Invoiced` (UC12)
- `POST /webhooks/pos/payment-confirmed` → transitions order to `Paid` (UC13)

**are absent from the POD sequence diagram** and must NOT be invoked for POD orders.
Their use cases (UC12, UC13) apply to non-POD channels where POS performs physical
payment collection (e.g. Click & Collect, Kiosk, in-store).

## Complete POD Post-Delivery Sequence

```
TMS → SC:  Delivered [/webhooks/tms/package-delivered]
SC  → Gateway:  Delivered notification (outbox)
            Order status = Delivered  ← terminal

STS → SC:  POST /webhooks/sts/abb-tax-invoice   (after Delivered)
SC  → TMS: Send ABB/Tax Invoice link (outbox: ABBTaxInvoiceSentToTMS)
SC  → Gateway:  Send ABB/Tax Invoice link (outbox: ABBTaxInvoiceSentToGateway)

[Optional — only if credit note exists]
STS → SC:  POST /webhooks/sts/credit-note
SC  → TMS: Send Credit Note link (outbox: CreditNoteSentToTMS)
SC  → Gateway:  Send Credit Note link (outbox: CreditNoteSentToGateway)
```

## Contrast: What the API Blueprint §2.3 Shows vs. Reality

| Aspect | API Blueprint §2.3 state machine | Confirmed POD reality |
|---|---|---|
| Terminal status for POD | Implies progression to `Invoiced` → `Paid` | `Delivered` |
| POS invoiced webhook used? | Listed as UC12 (general) | NOT called for POD |
| POS payment-confirmed used? | Listed as UC13 (general) | NOT called for POD |
| STS trigger for ABB invoice | After `Delivered` (correct in STS section) | After `Delivered` |
| ABB invoice routing | TMS + Gateway (correct in STS section) | TMS + Gateway |

The STS webhooks section of the blueprint is internally consistent with the sequence
diagram. The ambiguity is in the general state machine description which does not
clearly gate `Invoiced`/`Paid` transitions to non-POD channels.

## Implementation Constraint

Any handler for `POST /webhooks/pos/invoiced` and `POST /webhooks/pos/payment-confirmed`
MUST validate that the order's `is_prepaid` flag (or channel type) is not POD before
processing. Receiving these webhooks for a POD order is an error condition — the handler
should return 422 Unprocessable Entity.

## Watch Out For

- Blueprint language that implies a universal `Delivered → Invoiced → Paid` path:
  this path is channel-specific, not universal.
- The STS sequence diagram note says "trigger after Pick Confirm in case POD" —
  "Pick Confirm" in that context means delivery confirmation (Delivered), not the
  WMS PickConfirmed webhook. Do not conflate these two events.
- STS Credit Note is optional; do not make it a required step in POD handler logic.
