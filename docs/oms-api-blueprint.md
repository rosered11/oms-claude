FORMAT: 1A
HOST: https://api.sprintconnect.io/v1

# Sprint Connect OMS — API Blueprint

**Version:** 2.0
**Format:** REST / JSON

---

## Group Orders

### List Orders [GET /orders]

List orders (paginated). Used by the Kanban Board. (UC17)

+ Parameters
    + status (optional, string) - Filter by order status
    + store (optional, string) - Filter by store
    + type (optional, string) - Fulfillment type filter
    + channel (optional, string) - Channel type filter (e.g. `Marketplace`, `Gateway`, `Web`)
    + page (optional, number, `1`) - Page number
    + limit (optional, number, `50`) - Items per page (max 200)

+ Response 200 (application/json)
    + Body

            {
              "items": [
                {
                  "id": "ORD-001",
                  "orderNumber": "ORD-001",
                  "status": "PickStarted",
                  "fulfillmentType": "Delivery",
                  "paymentMethod": "Prepaid",
                  "store": "Central DC",
                  "storeId": "store-central-dc",
                  "lineCount": 3,
                  "totalAmount": 23.80,
                  "currency": "THB",
                  "customer": { "name": "Alice Johnson", "phone": "0812345678" },
                  "deliverySlot": {
                    "scheduledStart": "2024-01-15T18:00:00Z",
                    "scheduledEnd": "2024-01-15T20:00:00Z"
                  },
                  "createdAt": "2024-01-15T14:00:00Z",
                  "updatedAt": "2024-01-15T15:31:00Z"
                }
              ],
              "total": 42,
              "page": 1,
              "limit": 50
            }

---

### Create Order [POST /orders]

Create a new outbound order. (UC1)

Idempotent on `sourceOrderId` — duplicate calls with the same value return the existing order.

**`paymentFlow`** field: string — controls the invoice trigger and routing. Allowed values: `"PRE_PAID"` | `"PAY_ON_DELIVERY"`. Stored as `VARCHAR(50)` in `orders.payment_flow`. Extensible for future flow types.

**`paymentMethod`** field: string — the payment instrument. Stored in `payment.order_payments.payment_method`. Allowed values: `"CreditCard"`, `"QRCode"`, `"BankTransfer"`, `"StoreCredit"`, `"PayOnDelivery"`. Used for outbound field mapping to external systems (not for routing).

**Outbox events dispatched on order creation:**

| Event | Target | Condition |
|---|---|---|
| `SaleOrderSentToWMS` | WMS | All orders |
| `SaleOrderSentToTMS` | TMS | All orders — dispatched at order creation for transport scheduling |

+ Request (application/json)
    + Body

            {
              "sourceOrderId": "EXT-001",
              "channelType": "App",
              "businessUnit": "TOPS",
              "storeId": "store-central-dc",
              "fulfillmentType": "Delivery",
              "paymentFlow": "PRE_PAID",
              "paymentMethod": "CreditCard",
              "customer": {
                "name": "Alice Johnson",
                "phone": "0812345678",
                "email": "alice@example.com",
                "externalCustomerId": "CRM-ALICE-001"
              },
              "deliveryAddress": {
                "addressType": "Delivery",
                "firstName": "Alice",
                "lastName": "Johnson",
                "mobilePhone": "0812345678",
                "email": "alice@example.com",
                "address1": "123 Main St",
                "subdistrict": "Silom",
                "district": "Bang Rak",
                "province": "Bangkok",
                "postalCode": "10500"
              },
              "deliverySlot": {
                "scheduledStart": "2024-01-15T14:00:00Z",
                "scheduledEnd": "2024-01-15T16:00:00Z",
                "bookedVia": "TMS",
                "bookingRef": "TMS-BK-001"
              },
              "lines": [
                {
                  "sku": "APPLE-1KG",
                  "productName": "Apple (1 kg bag)",
                  "barcode": "8851234567890",
                  "requestedQty": 4,
                  "unitPrice": 1.20,
                  "unitOfMeasure": "Each"
                }
              ]
            }

+ Response 201 (application/json)
    + Body

            {
              "id": "ORD-001",
              "orderNumber": "ORD-001",
              "status": "Pending",
              "createdAt": "2024-01-15T14:00:00Z"
            }

+ Response 409 (application/json)
    + Body

            { "error": "conflict", "detail": "Order with sourceOrderId EXT-001 already exists as ORD-001." }

---

### Get Order [GET /orders/{id}]

Get full order detail. (UC16)

+ Parameters
    + id (required, string) - Order ID

+ Response 200 (application/json)
    + Body

            {
              "id": "ORD-001",
              "orderNumber": "ORD-001",
              "status": "PickStarted",
              "fulfillmentType": "Delivery",
              "paymentMethod": "Prepaid",
              "substitutionFlag": false,
              "store": "Central DC",
              "storeId": "store-central-dc",
              "customer": { "name": "Alice Johnson", "phone": "0812345678", "email": "alice@example.com" },
              "deliveryAddress": {
                "address1": "123 Main St",
                "subdistrict": "Silom",
                "district": "Bang Rak",
                "province": "Bangkok",
                "postalCode": "10500"
              },
              "deliverySlot": {
                "slotId": "slot-001",
                "scheduledStart": "2024-01-15T18:00:00Z",
                "scheduledEnd": "2024-01-15T20:00:00Z"
              },
              "lines": [
                {
                  "orderLineId": "line-001",
                  "sku": "APPLE-1KG",
                  "productName": "Apple (1 kg bag)",
                  "barcode": "8851234567890",
                  "unitOfMeasure": "Each",
                  "requestedQty": 4,
                  "pickedQty": 4,
                  "unitPrice": 1.20,
                  "recalculatedUnitPrice": 1.08,
                  "currency": "THB",
                  "status": "Active"
                }
              ],
              "packages": [
                {
                  "packageId": "pkg-001",
                  "trackingId": "TRK-2024-001",
                  "vehicleType": "Van",
                  "weight": 2.5,
                  "status": "OutForDelivery",
                  "lineIds": ["line-001"]
                }
              ],
              "totalAmount": 23.80,
              "currency": "THB",
              "createdAt": "2024-01-15T14:00:00Z",
              "updatedAt": "2024-01-15T15:31:00Z"
            }

---

### List Order Lines [GET /orders/{id}/lines]

List order lines with picked quantities and recalculated prices. (UC16)

+ Parameters
    + id (required, string) - Order ID

+ Response 200 (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "lines": [
                {
                  "orderLineId": "line-001",
                  "sku": "APPLE-1KG",
                  "productName": "Apple (1 kg bag)",
                  "barcode": "8851234567890",
                  "unitOfMeasure": "Each",
                  "requestedQty": 4,
                  "pickedQty": 4,
                  "unitPrice": 1.20,
                  "recalculatedUnitPrice": 1.08,
                  "currency": "THB",
                  "status": "Active"
                }
              ]
            }

---

### List Order Packages [GET /orders/{id}/packages]

List packages with tracking IDs and carrier details. (UC18)

+ Parameters
    + id (required, string) - Order ID

+ Response 200 (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "packages": [
                {
                  "packageId": "pkg-001",
                  "trackingId": "TRK-2024-001",
                  "vehicleType": "Van",
                  "weight": 2.5,
                  "status": "OutForDelivery",
                  "lineIds": ["line-001"]
                }
              ]
            }

---

### List Order Webhooks [GET /orders/{id}/webhooks]

List all webhook events received for an order.

+ Parameters
    + id (required, string) - Order ID

+ Response 200 (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "webhooks": [
                {
                  "webhookLogId": "whl-001",
                  "sourceSystem": "WMS",
                  "eventType": "PickStarted",
                  "detail": "Picker picker-01 started at 2024-01-15T15:00:00Z",
                  "receivedAt": "2024-01-15T15:00:00Z"
                }
              ]
            }

---

### Get Order Credit Note [GET /orders/{id}/credit-note]

Get the credit note associated with an order. Returns the credit note issued by STS for this order — for example, after a substitution where the replacement item is cheaper than the original (UC11), or after a partial pick adjustment.

Returns `404` if no credit note exists for the order.

| Field | Type | Description |
|---|---|---|
| `creditNoteId` | string | OMS internal credit note ID |
| `creditNoteNumber` | string | Fiscal credit note number from STS |
| `invoiceId` | string | The invoice being partially reversed |
| `amount` | number | Credit amount in baht (e.g. `44.00`) |
| `currency` | string | Currency code — `THB` |
| `reason` | string | Why the credit note was issued — e.g. `PriceAdjustment`, `CustomerRejection` |
| `status` | string | `Issued`, `Applied`, or `Cancelled` |
| `creditNoteLink` | string | URL to the Credit Note PDF hosted by STS |
| `sourceStsRef` | string | STS reference ID for reconciliation |
| `issuedAt` | timestamp | When STS issued the credit note (ISO 8601 UTC) |

+ Parameters
    + id (required, string) - Order ID

+ Response 200 (application/json)
    + Body

            {
              "creditNoteId": "CN-001",
              "creditNoteNumber": "CN-UC11-1716000000000",
              "invoiceId": "inv-001",
              "amount": 44.00,
              "currency": "THB",
              "reason": "PriceAdjustment",
              "status": "Issued",
              "creditNoteLink": "https://sts.example.com/cn/UC11.pdf",
              "sourceStsRef": "STS-CN-REF-001",
              "issuedAt": "2024-01-15T16:05:00Z"
            }

+ Response 404 (application/json)
    + Body

            { "error": "not_found", "detail": "No credit note exists for order ORD-001." }

---

### List Order Substitutions [GET /orders/{id}/substitutions]

List substitutions offered by WMS. (UC5)

+ Parameters
    + id (required, string) - Order ID

+ Response 200 (application/json)
    + Body

            {
              "orderId": "ORD-003",
              "substitutions": [
                {
                  "substitutionId": "sub-001",
                  "orderLineId": "line-003",
                  "originalSku": "MILK-1L",
                  "originalProductName": "Whole Milk (1L)",
                  "substituteSku": "MILK-2L",
                  "substituteProductName": "Whole Milk (2L)",
                  "substituteUnitPrice": 0.55,
                  "substitutedAmount": 1,
                  "customerApproved": null,
                  "approvedAt": null,
                  "createdAt": "2024-01-15T15:10:00Z"
                }
              ]
            }

---

### Approve Substitution [POST /orders/{id}/substitutions/{subId}/approve]

Customer approves a proposed substitution. (UC5)

+ Parameters
    + id (required, string) - Order ID
    + subId (required, string) - Substitution ID

+ Response 200 (application/json)
    + Body

            { "substitutionId": "sub-001", "customerApproved": true, "approvedAt": "2024-01-15T15:15:00Z" }

---

### Reject Substitution [POST /orders/{id}/substitutions/{subId}/reject]

Customer rejects a substitution; original line is voided. (UC5)

+ Parameters
    + id (required, string) - Order ID
    + subId (required, string) - Substitution ID

+ Response 200 (application/json)
    + Body

            { "substitutionId": "sub-001", "customerApproved": false, "approvedAt": "2024-01-15T15:16:00Z" }

---

### Get Order Timeline [GET /orders/{id}/timeline]

Chronological event history combining domain transitions, inbound webhooks, and outbox events. (UC16)

**Event types:**

| `type` | Source table |
|---|---|
| `domain` | `orders.order_status_history` |
| `webhook` | `orders.order_webhook_logs` |
| `outbox` | `orders.order_outbox` |
| `bridge` | Derived marker — synthesized between PO put-away and first order event |

+ Parameters
    + id (required, string) - Order ID

+ Response 200 (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "order": {
                "orderNumber": "ORD-001",
                "status": "Delivered",
                "fulfillmentType": "Delivery",
                "store": "Central DC"
              },
              "events": [
                {
                  "id": 1,
                  "occurredAt": "2024-01-15T14:00:00Z",
                  "time": "14:00",
                  "phase": "outbound",
                  "type": "domain",
                  "system": "OMS",
                  "event": "Pending",
                  "detail": "Order created. 4 lines, ฿480.",
                  "outStatus": null
                },
                {
                  "id": 2,
                  "occurredAt": "2024-01-15T15:00:00Z",
                  "time": "15:00",
                  "phase": "outbound",
                  "type": "webhook",
                  "system": "WMS",
                  "event": "PickStarted",
                  "detail": "Picker picker-01 started.",
                  "outStatus": null
                },
                {
                  "id": 3,
                  "occurredAt": "2024-01-15T15:00:00Z",
                  "time": "15:00",
                  "phase": "outbound",
                  "type": "outbox",
                  "system": "TMS",
                  "event": "PickStartedEvent",
                  "detail": "Dispatched to TMS for driver scheduling.",
                  "outStatus": "Published"
                }
              ],
              "summary": {
                "totalEvents": 12,
                "inboundPhaseEvents": 3,
                "outboundPhaseEvents": 9,
                "orderToDeliveredMinutes": 320,
                "totalEndToEndMinutes": 380
              }
            }

---

### Hold Order [PATCH /orders/{id}/hold]

Place order on hold. Saves `pre_hold_status`. (UC6)

+ Parameters
    + id (required, string) - Order ID

+ Request (application/json)
    + Body

            { "holdReason": "ManualReview", "heldBy": "ops-agent-01" }

+ Response 200 (application/json)
    + Body

            { "id": "ORD-001", "newStatus": "OnHold", "preHoldStatus": "PickStarted" }

---

### Release Order Hold [PATCH /orders/{id}/release-hold]

Release hold; restores `pre_hold_status`. (UC6)

+ Parameters
    + id (required, string) - Order ID

+ Request (application/json)
    + Body

            { "releasedBy": "ops-agent-01" }

+ Response 200 (application/json)
    + Body

            { "id": "ORD-001", "newStatus": "PickStarted" }

---

### Cancel Order [PATCH /orders/{id}/cancel]

Cancel order. Allowed from `Pending` or `OnHold` only. (UC9)

**Outbox events dispatched on cancellation (all three are appended atomically):**

| Event | Target | Purpose |
|---|---|---|
| `OrderCancelledSentToWMS` | WMS | Reverse stock reservation |
| `OrderCancelledSentToTMS` | TMS | Cancel delivery booking |
| `OrderCancelledSentToGateway` | Gateway | Notify customer of cancellation |

All three events appear on `GET /orders/{id}/timeline` with `type: "outbox"` and the respective `system` values (`"WMS"`, `"TMS"`, `"Gateway"`).

+ Parameters
    + id (required, string) - Order ID

+ Request (application/json)
    + Body

            { "reason": "CustomerRequest", "cancelledBy": "ops-agent-01" }

+ Response 200 (application/json)
    + Body

            { "id": "ORD-005", "newStatus": "Cancelled" }

+ Response 409 (application/json)
    + Body

            { "error": "invalid_transition", "detail": "Order ORD-005 is in status Delivered. Cancellation is not allowed from this state." }

---

### Trigger POS Recalculation [POST /orders/{id}/recalculate]

Manually trigger a POS recalculation. OMS calls POS API outbound synchronously and returns the adjusted amount. (UC15)

+ Parameters
    + id (required, string) - Order ID

+ Response 202 (application/json)
    + Body

            { "orderId": "ORD-009", "adjustedAmount": 198.00, "recalcTriggeredAt": "2024-01-15T15:30:00Z" }

---

### Record Partial Pick [PATCH /orders/{id}/partial-pick]

Record a partial pick: one or more order lines were picked in lesser quantity than ordered. OMS calls POS outbound synchronously to recalculate. (UC-PARTPICK)

Not allowed after `PickConfirmed`.

**Error 409:** `pos_recalc_already_pending` — POS recalculation already in progress

**Error 409:** `invalid_transition` — Order not in a state that allows partial pick

**Error 422:** `zero_pick_not_allowed` — All lines reduced to zero; use Cancel Order instead

+ Parameters
    + id (required, string) - Order ID

+ Request (application/json)
    + Body

            {
              "lines": [
                {
                  "orderLineId": "LINE-001",
                  "pickedQuantity": 1,
                  "orderedQuantity": 2,
                  "reason": "OutOfStock"
                }
              ],
              "idempotencyKey": "uuid-here"
            }

+ Response 200 (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "status": "PickStarted",
              "partialLines": [
                {
                  "orderLineId": "LINE-001",
                  "pickedQuantity": 1,
                  "orderedQuantity": 2,
                  "shortfallQuantity": 1,
                  "reason": "OutOfStock"
                }
              ]
            }

+ Response 409 (application/json)
    + Body

            { "error": "pos_recalc_already_pending", "detail": "POS recalculation already in progress for ORD-001." }

+ Response 422 (application/json)
    + Body

            { "error": "zero_pick_not_allowed", "detail": "All lines reduced to zero quantity. Use Cancel Order instead." }

---

### Get Delivery Slot [GET /orders/{id}/delivery-slot]

Get current delivery slot. (UC19)

+ Parameters
    + id (required, string) - Order ID

+ Response 200 (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "slotId": "slot-001",
              "scheduledStart": "2024-01-15T18:00:00Z",
              "scheduledEnd": "2024-01-15T20:00:00Z",
              "storeId": "store-central-dc"
            }

---

### Reschedule Delivery Slot [PATCH /orders/{id}/delivery-slot]

Reschedule delivery window. Not allowed once order is `OutForDelivery`, `Delivered`, or later. (UC19, UC-RESCHEDULE)

**Outbox event dispatched:** `DeliverySlotRescheduledEvent` → TMS

+ Parameters
    + id (required, string) - Order ID

+ Request (application/json)
    + Body

            {
              "scheduledStart": "2024-01-15T20:00:00Z",
              "scheduledEnd": "2024-01-15T22:00:00Z",
              "bookedVia": "TMS",
              "bookingRef": "TMS-BK-002",
              "reason": "CustomerRequest"
            }

+ Response 200 (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "deliverySlot": {
                "scheduledStart": "2024-01-15T20:00:00Z",
                "scheduledEnd": "2024-01-15T22:00:00Z"
              }
            }

+ Response 409 (application/json)
    + Body

            { "error": "slot_change_not_allowed", "detail": "Order ORD-001 is already OutForDelivery. Slot cannot be changed." }

---

### Send Prepaid Invoice [POST /orders/{id}/invoice/prepaid]

Send pre-delivery ABB/Tax Invoice to WMS. Prepaid orders only — called after PickConfirmed and before TMS dispatch. (UC28)

+ Parameters
    + id (required, string) - Order ID

+ Response 202 (application/json)
    + Body

            { "orderId": "ORD-001", "invoiceNumber": "INV-PRE-001", "invoicedAt": "2024-01-15T09:46:00Z" }

---

## Group Returns

### Create Return [POST /returns]

Initiate a return for a delivered or paid order. (UC14)

#### Partial Item Return

When a customer rejects specific items at delivery (e.g. ordered beef and chicken, but chicken was not fresh):

- Call `POST /returns` with `returnType: "PartialItem"` and list only the rejected line items in `items[]`
- OMS triggers partial refund calculation via POS
- Only allowed after `Delivered` status
- Each `returnLineItem` must reference the original `orderLineId`

**Example request for partial return:**

```json
{
  "orderId": "ORD-001",
  "returnType": "PartialItem",
  "returnReason": "ItemNotFresh",
  "items": [
    { "orderLineId": "line-002", "sku": "CHICKEN-1KG", "quantity": 1, "itemReason": "ItemNotFresh" }
  ],
  "requestedBy": "alice@example.com"
}
```

+ Request (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "returnReason": "WrongItem",
              "items": [
                { "orderLineId": "line-001", "sku": "APPLE-1KG", "quantity": 2, "itemReason": "WrongItem" }
              ],
              "requestedBy": "alice@example.com"
            }

+ Response 201 (application/json)
    + Body

            {
              "id": "RET-001",
              "returnOrderNumber": "RET-001",
              "orderId": "ORD-001",
              "status": "ReturnRequested",
              "createdAt": "2024-01-15T20:00:00Z"
            }

+ Response 422 (application/json)
    + Body

            { "error": "unprocessable", "detail": "Order ORD-005 is in status Cancelled. Returns are only allowed from Delivered." }

---

### List Returns [GET /returns]

List returns.

+ Parameters
    + orderId (optional, string) - Filter by order ID
    + status (optional, string) - Filter by return status
    + page (optional, number, `1`) - Page number
    + limit (optional, number, `50`) - Items per page

+ Response 200 (application/json)
    + Body

            {
              "items": [
                {
                  "id": "RET-001",
                  "returnOrderNumber": "RET-001",
                  "orderId": "ORD-001",
                  "status": "PutAway",
                  "returnReason": "WrongItem",
                  "requestedAt": "2024-01-15T20:00:00Z",
                  "refundedAt": "2024-01-16T11:35:00Z",
                  "createdAt": "2024-01-15T20:00:00Z",
                  "updatedAt": "2024-01-16T11:35:00Z"
                }
              ],
              "total": 5,
              "page": 1,
              "limit": 50
            }

---

### Get Return [GET /returns/{id}]

Get full return detail. (UC14)

+ Parameters
    + id (required, string) - Return ID

+ Response 200 (application/json)
    + Body

            {
              "id": "RET-001",
              "returnOrderNumber": "RET-001",
              "orderId": "ORD-001",
              "invoiceId": "inv-001",
              "creditNoteId": "CN-RET-001",
              "status": "PutAway",
              "goodsReceiveNo": "GRN-RET-2024-001",
              "returnReason": "WrongItem",
              "requestedAt": "2024-01-15T20:00:00Z",
              "pickupScheduledAt": "2024-01-16T09:00:00Z",
              "pickedUpAt": "2024-01-16T09:45:00Z",
              "receivedAt": "2024-01-16T11:00:00Z",
              "inspectedAt": "2024-01-16T11:20:00Z",
              "putAwayAt": "2024-01-16T11:30:00Z",
              "refundedAt": "2024-01-16T11:35:00Z",
              "createdAt": "2024-01-15T20:00:00Z",
              "updatedAt": "2024-01-16T11:35:00Z"
            }

---

### List Return Items [GET /returns/{id}/items]

List items in a return, including condition and put-away location. (UC14)

+ Parameters
    + id (required, string) - Return ID

+ Response 200 (application/json)
    + Body

            {
              "returnId": "RET-001",
              "items": [
                {
                  "returnItemId": "ri-001",
                  "orderLineId": "line-001",
                  "sku": "APPLE-1KG",
                  "productName": "Apple (1 kg bag)",
                  "barcode": "8851234567890",
                  "quantity": 2,
                  "unitOfMeasure": "Each",
                  "unitPrice": 1.20,
                  "currency": "THB",
                  "itemReason": "WrongItem",
                  "condition": "Resellable",
                  "putAwayStatus": "PutAway",
                  "assignedSloc": "B-05",
                  "inspectedAt": "2024-01-16T11:20:00Z",
                  "putAwayAt": "2024-01-16T11:30:00Z"
                }
              ]
            }

---

### Cancel Return [PATCH /returns/{id}/cancel]

Cancel return (allowed from `ReturnRequested` or `PickupScheduled` only). (UC14)

+ Parameters
    + id (required, string) - Return ID

+ Request (application/json)
    + Body

            { "reason": "CustomerChangedMind", "cancelledBy": "ops-agent-01" }

+ Response 200 (application/json)
    + Body

            { "id": "RET-001", "newStatus": "Cancelled" }

---

### Get Return Refund [GET /returns/{id}/refund]

Get refund and credit note for a completed return. (UC14)

+ Parameters
    + id (required, string) - Return ID

+ Response 200 (application/json)
    + Body

            {
              "returnId": "RET-001",
              "refund": {
                "refundId": "ref-001",
                "refundAmount": 2.40,
                "currency": "THB",
                "refundMethod": "CreditCard",
                "status": "Processed",
                "referenceNo": "REF-TXN-001",
                "processedAt": "2024-01-16T11:35:00Z"
              },
              "creditNote": {
                "creditNoteId": "CN-RET-001",
                "creditNoteNumber": "CN-RET-001",
                "invoiceId": "inv-001",
                "amount": 2.40,
                "currency": "THB",
                "reason": "Return",
                "status": "Issued"
              }
            }

---

## Group Inbound

### List Purchase Orders [GET /inbound/purchase-orders]

List Purchase Orders. (UC21)

+ Parameters
    + status (optional, string) - Filter by PO status
    + store (optional, string) - Filter by store
    + page (optional, number, `1`) - Page number
    + limit (optional, number, `50`) - Items per page

+ Response 200 (application/json)
    + Body

            {
              "items": [
                {
                  "id": "PO-001",
                  "poNumber": "PO-001",
                  "supplier": "Fresh Foods Ltd",
                  "supplierId": "sup-fresh-foods",
                  "lines": 3,
                  "status": "Closed",
                  "store": "Central DC",
                  "value": 450.00,
                  "goodsReceiveNo": "GRN-2024-001",
                  "createdAt": "2024-01-15T08:00:00Z",
                  "updatedAt": "2024-01-15T10:15:00Z"
                }
              ],
              "total": 10,
              "page": 1,
              "limit": 50
            }

---

### Create Purchase Order [POST /inbound/purchase-orders]

Create a Purchase Order. Triggers `PurchaseOrderCreatedEvent` → WMS. (UC21)

+ Request (application/json)
    + Body

            {
              "poNumber": "PO-005",
              "supplierId": "sup-fresh-foods",
              "storeId": "store-central-dc",
              "lines": [
                { "sku": "APPLE-1KG", "orderedQty": 20, "unitCost": 0.45, "currency": "THB" }
              ]
            }

+ Response 201 (application/json)
    + Body

            { "id": "PO-005", "poNumber": "PO-005", "status": "Created", "createdAt": "2024-01-15T08:00:00Z" }

---

### Get Purchase Order [GET /inbound/purchase-orders/{id}]

Get PO detail including all lines with received quantities and conditions. (UC21)

+ Parameters
    + id (required, string) - Purchase Order ID

+ Response 200 (application/json)
    + Body

            {
              "id": "PO-001",
              "poNumber": "PO-001",
              "supplier": "Fresh Foods Ltd",
              "store": "Central DC",
              "storeId": "store-central-dc",
              "status": "Closed",
              "value": 450.00,
              "goodsReceiveNo": "GRN-2024-001",
              "lines": [
                {
                  "poLineId": "pol-001",
                  "sku": "APPLE-1KG",
                  "productName": "Apple (1 kg bag)",
                  "orderedQty": 10,
                  "receivedQty": 10,
                  "unitCost": 0.45,
                  "currency": "THB",
                  "condition": "Resellable",
                  "sloc": "A-12",
                  "receivedAt": "2024-01-15T09:28:00Z",
                  "putAwayAt": "2024-01-15T10:15:00Z"
                }
              ],
              "createdAt": "2024-01-15T08:00:00Z",
              "updatedAt": "2024-01-15T10:15:00Z"
            }

---

### List Goods Receipts for PO [GET /inbound/purchase-orders/{id}/goods-receipts]

List goods receipt records for a PO. (UC21)

+ Parameters
    + id (required, string) - Purchase Order ID

+ Response 200 (application/json)
    + Body

            {
              "purchaseOrderId": "PO-001",
              "goodsReceipts": [
                {
                  "goodsReceiveNo": "GRN-2024-001",
                  "status": "PutAway",
                  "receivedAt": "2024-01-15T09:28:00Z",
                  "putAwayAt": "2024-01-15T10:15:00Z",
                  "lines": [
                    { "sku": "APPLE-1KG", "receivedQty": 10, "condition": "Resellable", "sloc": "A-12" }
                  ]
                }
              ]
            }

---

### List Transfer Orders [GET /inbound/transfer-orders]

List Transfer Orders. (UC22)

+ Parameters
    + status (optional, string) - Filter by TO status
    + sourceStore (optional, string) - Filter by source store
    + destStore (optional, string) - Filter by destination store
    + page (optional, number, `1`) - Page number
    + limit (optional, number, `50`) - Items per page

+ Response 200 (application/json)
    + Body

            {
              "items": [
                {
                  "id": "TR-001",
                  "transferNumber": "TR-001",
                  "source": "Central DC",
                  "sourceStoreId": "store-central-dc",
                  "dest": "Store A",
                  "destStoreId": "store-a",
                  "lines": 1,
                  "status": "Completed",
                  "tracking": "TRK-TR-001",
                  "createdAt": "2024-01-15T10:00:00Z",
                  "updatedAt": "2024-01-15T14:30:00Z"
                }
              ],
              "total": 4,
              "page": 1,
              "limit": 50
            }

---

### Create Transfer Order [POST /inbound/transfer-orders]

Create a Transfer Order. Triggers `TransferOrderCreatedEvent` → source WMS. (UC22)

+ Request (application/json)
    + Body

            {
              "sourceStoreId": "store-central-dc",
              "destStoreId": "store-b",
              "lines": [ { "sku": "APPLE-1KG", "requestedQty": 6 } ]
            }

+ Response 201 (application/json)
    + Body

            { "id": "TR-005", "transferNumber": "TR-005", "status": "Created", "createdAt": "2024-01-15T10:00:00Z" }

---

### Get Transfer Order [GET /inbound/transfer-orders/{id}]

Get Transfer Order detail including lines and quantities. (UC22)

+ Parameters
    + id (required, string) - Transfer Order ID

+ Response 200 (application/json)
    + Body

            {
              "id": "TR-001",
              "transferNumber": "TR-001",
              "source": "Central DC",
              "sourceStoreId": "store-central-dc",
              "dest": "Store A",
              "destStoreId": "store-a",
              "status": "Completed",
              "lines": [
                {
                  "toLineId": "tol-001",
                  "sku": "APPLE-1KG",
                  "productName": "Apple (1 kg bag)",
                  "requestedQty": 4,
                  "transferredQty": 4,
                  "confirmedAt": "2024-01-15T11:00:00Z"
                }
              ],
              "createdAt": "2024-01-15T10:00:00Z",
              "updatedAt": "2024-01-15T14:30:00Z"
            }

---

### List Transfer Order Confirmations [GET /inbound/transfer-orders/{id}/confirmations]

List pick and receipt confirmations for a Transfer Order. (UC22)

+ Parameters
    + id (required, string) - Transfer Order ID

+ Response 200 (application/json)
    + Body

            {
              "transferOrderId": "TR-001",
              "confirmations": [
                { "type": "PickConfirmed", "confirmedAt": "2024-01-15T11:00:00Z", "confirmedBy": "WMS", "tracking": "TRK-TR-001" },
                { "type": "TransferReceived", "confirmedAt": "2024-01-15T14:30:00Z", "confirmedBy": "WMS", "tracking": "TRK-TR-001" }
              ]
            }

---

## Group Stock

### Get SKU Stock Ledger [GET /stock/{sku}/ledger]

Per-SKU stock movement ledger across locations. OMS does not own inventory counts — this reflects events OMS recorded. (UC24)

+ Parameters
    + sku (required, string) - SKU identifier
    + storeId (optional, string) - Filter by store
    + from (optional, string) - ISO 8601 start date
    + to (optional, string) - ISO 8601 end date

+ Response 200 (application/json)
    + Body

            {
              "sku": "APPLE-1KG",
              "skuName": "Apple (1 kg bag)",
              "unitPrice": 1.20,
              "currency": "THB",
              "locations": [
                {
                  "storeId": "store-central-dc",
                  "storeName": "Central DC",
                  "balance": 0,
                  "events": [
                    { "id": 1, "time": "10:15", "occurredAt": "2024-01-15T10:15:00Z", "dir": "in", "ref": "PO-001", "refType": "PurchaseOrder", "event": "PurchaseOrderPutAwayConfirmed", "qtyChange": 10, "balance": 10, "detail": "Fresh Foods Ltd — 10 bags shelved at Sloc A-12." },
                    { "id": 2, "time": "11:00", "occurredAt": "2024-01-15T11:00:00Z", "dir": "out", "ref": "TR-001", "refType": "TransferOrder", "event": "TransferPickConfirmed", "qtyChange": -4, "balance": 6, "detail": "4 bags picked for transfer → Store A" },
                    { "id": 3, "time": "15:31", "occurredAt": "2024-01-15T15:31:00Z", "dir": "out", "ref": "ORD-A", "refType": "Order", "event": "PickConfirmed", "qtyChange": -6, "balance": 0, "detail": "6 bags picked for delivery" }
                  ]
                }
              ]
            }

---

## Group Webhooks (Inbound)

All webhook endpoints return `202 Accepted`. Duplicate `X-Idempotency-Key` values are ignored.

**Required headers on all webhooks:**

| Header | Description |
|---|---|
| `X-Source-System` | `WMS`, `TMS`, or `POS` |
| `X-Idempotency-Key` | UUID |
| `X-Webhook-Signature` | HMAC-SHA256 of body using shared secret |

---

### WMS Pick Started [POST /webhooks/wms/pick-started]

WMS picker begins collecting items. → `PickStarted`. (UC3)

**Outbox events dispatched:**
- `PickStartedSentToTMS` → TMS (all payment methods — allows TMS to prepare delivery logistics in advance)

+ Request (application/json)
    + Body

            { "orderId": "ORD-001", "pickerId": "picker-01", "startedAt": "2024-01-15T15:00:00Z" }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "orderId": "ORD-001", "newStatus": "PickStarted" }

---

### WMS Wave Started [POST /webhooks/wms/wave-started]

WMS notifies OMS that wave picking has started. (UC-WAVE)

Valid only when order is in `PickStarted` status.

**Outbox event dispatched:** `WaveStartedSentToGateway` → Gateway

+ Request (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "waveId": "WAVE-001",
              "startedAt": "2024-01-15T15:35:00Z"
            }

+ Response 202

---

### WMS Pick Confirmed [POST /webhooks/wms/pick-confirmed]

WMS reports actual picked quantities per line. Triggers POS recalculation if any quantity differs or substitution exists. → `PickConfirmed`. (UC4)

+ Request (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "lines": [ { "orderLineId": "line-001", "sku": "APPLE-1KG", "pickedQty": 5, "substituted": false } ],
              "pickedAt": "2024-01-15T15:31:00Z"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "orderId": "ORD-001", "newStatus": "PickConfirmed" }

---

### WMS Packed [POST /webhooks/wms/packed]

WMS confirms order packed into packages. → `Packed`. (UC4)

+ Request (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "packages": [ { "trackingId": "TRK-2024-001", "vehicleType": "Van", "weight": 2.5, "lineIds": ["line-001"] } ],
              "packedAt": "2024-01-15T17:30:00Z"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "orderId": "ORD-001", "newStatus": "Packed", "packagesCreated": 1 }

---

### WMS Substitution Offered [POST /webhooks/wms/substitution-offered]

WMS offers alternative SKU for unfulfillable line. Sets `substitution_flag = true`. (UC5)

+ Request (application/json)
    + Body

            {
              "orderId": "ORD-003",
              "orderLineId": "line-003",
              "substituteSku": "MILK-2L",
              "substituteProductName": "Whole Milk (2L)",
              "substituteUnitPrice": 0.55,
              "substitutedAmount": 1,
              "offeredAt": "2024-01-15T15:10:00Z"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "substitutionId": "sub-001", "orderId": "ORD-003", "customerNotified": true }

---

### WMS Put Away Confirmed [POST /webhooks/wms/put-away-confirmed]

WMS confirms returned items are shelved. Atomically: transitions the **return record** to `PutAway`, transitions the **linked order** from `Delivered` to `Returned`, and initiates the refund calculation. (UC12, UC14)

**Side effects (all atomic in the same DB transaction):**

| Effect | Detail |
|---|---|
| Return record status | `Requested` → `PutAway` |
| **Order status** | `Delivered` → **`Returned`** |
| Refund record | Created with `status: Pending` |
| Credit note | Created or linked if applicable |

The order status transition to `Returned` was added to fix a bug where the order remained in `Delivered` after the return was put away. Calling `GET /orders/{orderId}` after this webhook now returns `status: "Returned"`.

+ Request (application/json)
    + Body

            {
              "returnId": "RET-001",
              "items": [
                { "sku": "APPLE-1KG", "condition": "Resellable", "sloc": "B-05", "quantity": 2, "performedBy": "wms-picker-07" }
              ],
              "putAwayAt": "2024-01-15T11:00:00Z"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "returnId": "RET-001", "newReturnStatus": "PutAway", "refundInitiated": true, "creditNoteId": "CN-RET-001" }

---

### WMS Goods Receipt Confirmed [POST /webhooks/wms/goods-receipt-confirmed]

WMS confirms goods physically received at dock against a PO. (UC21)

+ Request (application/json)
    + Body

            {
              "purchaseOrderId": "PO-001",
              "goodsReceiveNo": "GRN-2024-001",
              "lines": [ { "sku": "APPLE-1KG", "receivedQty": 10 } ],
              "receivedAt": "2024-01-15T09:28:00Z"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "purchaseOrderId": "PO-001", "newStatus": "FullyReceived" }

---

### WMS Purchase Order Put Away Confirmed [POST /webhooks/wms/purchase-order-put-away-confirmed]

WMS confirms inbound goods shelved. Closes PO. Signals stock available. (UC21)

+ Request (application/json)
    + Body

            {
              "purchaseOrderId": "PO-001",
              "items": [ { "sku": "APPLE-1KG", "condition": "Resellable", "sloc": "A-12", "qty": 10 } ],
              "putAwayAt": "2024-01-15T10:14:00Z"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "purchaseOrderId": "PO-001", "newStatus": "Closed" }

---

### WMS Transfer Pick Confirmed [POST /webhooks/wms/transfer-pick-confirmed]

WMS at source store confirms items picked and packed for transfer. Triggers TMS dispatch. (UC22)

+ Request (application/json)
    + Body

            {
              "transferOrderId": "TR-001",
              "lines": [ { "sku": "APPLE-1KG", "transferredQty": 4 } ],
              "confirmedAt": "2024-01-15T11:00:00Z"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "transferOrderId": "TR-001", "newStatus": "PickConfirmed" }

---

### WMS Transfer Received [POST /webhooks/wms/transfer-received]

WMS at destination confirms stock arrived and put away. Completes Transfer Order. (UC22)

+ Request (application/json)
    + Body

            { "transferOrderId": "TR-001", "receivedAt": "2024-01-15T14:30:00Z" }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "transferOrderId": "TR-001", "newStatus": "Completed" }

---

### WMS Damaged Goods Received [POST /webhooks/wms/damaged-goods-received]

WMS checks in a damaged package returned by TMS driver. Order → `OnHold (PackageDamaged)`. (UC23)

+ Request (application/json)
    + Body

            {
              "orderId": "ORD-006",
              "trackingId": "TRK-2024-006",
              "receivedAt": "2024-01-15T12:00:00Z",
              "items": [
                { "sku": "APPLE-1KG", "condition": "Repairable", "sloc": null, "quantity": 3 }
              ]
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "orderId": "ORD-006", "damagedReceiptId": "DMG-001", "newOrderStatus": "OnHold", "holdReason": "PackageDamaged" }

---

### WMS Damaged Goods Put Away [POST /webhooks/wms/damaged-goods-put-away]

WMS confirms damaged items inspected, condition assigned, shelved/disposed. (UC23)

+ Request (application/json)
    + Body

            {
              "damagedReceiptId": "DMG-001",
              "items": [ { "sku": "APPLE-1KG", "condition": "Repairable", "sloc": "DMG-01", "quantity": 12 } ],
              "putAwayAt": "2024-01-15T12:00:00Z",
              "updatedBy": "wms-inspector-02"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "damagedReceiptId": "DMG-001", "newStatus": "PutAway" }

---

### TMS Slot Rescheduled [POST /webhooks/tms/slot-rescheduled]

TMS notifies OMS that the delivery slot has been rescheduled by the customer. Updates the delivery slot and notifies WMS. Not allowed once the order is `OutForDelivery` or later. (UC8)

**Outbox event dispatched:** `DeliverySlotRescheduledEvent` → WMS

+ Request (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "newScheduledStart": "2024-01-15T20:00:00Z",
              "newScheduledEnd": "2024-01-15T21:00:00Z",
              "bookingRef": "TMS-RESCHEDULE-001",
              "reason": "CustomerRequest",
              "rescheduledAt": "2024-01-15T14:00:00Z"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "orderId": "ORD-001", "deliverySlot": { "scheduledStart": "2024-01-15T20:00:00Z", "scheduledEnd": "2024-01-15T21:00:00Z" } }

+ Response 409 (application/json)
    + Body

            { "error": "slot_change_not_allowed", "detail": "Order ORD-001 is already OutForDelivery. Slot cannot be changed." }

---

### TMS Package Dispatched [POST /webhooks/tms/package-dispatched]

TMS driver collected the package. Transitions order to `OutForDelivery`. (UC7)

**Outbox event dispatched:** `OutForDeliverySentToGateway` → Gateway

+ Request (application/json)
    + Body

            { "trackingId": "TRK-2024-001", "dispatchedAt": "2024-01-15T17:47:00Z" }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "orderId": "ORD-001", "newOrderStatus": "OutForDelivery", "newPackageStatus": "OutForDelivery" }

---

### TMS Package Delivered [POST /webhooks/tms/package-delivered]

TMS confirms delivery to customer. Terminal state for home delivery. For POD orders, triggers the STS ABB/Tax Invoice flow via Gateway. → `Delivered`. (UC8)

**Outbox events dispatched:**
- `DeliveredSentToGateway` → Gateway (all payment methods)

+ Request (application/json)
    + Body

            {
              "trackingId": "TRK-2024-001",
              "deliveredAt": "2024-01-15T19:22:00Z",
              "recipientName": "Alice Johnson",
              "proofOfDelivery": "https://tms.example.com/pod/TRK-2024-001.jpg"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "orderId": "ORD-001", "newStatus": "Delivered", "invoiceTriggered": true }

---

### TMS Package Damage Reported [POST /webhooks/tms/package-damage-reported]

TMS driver reports damage before/during delivery. Order → `OnHold`. Driver instructed to return goods to warehouse. (UC20)

+ Request (application/json)
    + Body

            {
              "trackingId": "TRK-2024-006",
              "reason": "PackageDamaged",
              "driverNote": "Box crushed during transport",
              "reportedAt": "2024-01-15T10:45:00Z"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "orderId": "ORD-006", "newOrderStatus": "OnHold", "holdReason": "PackageDamaged", "preHoldStatus": "OutForDelivery" }

---

### TMS Recalculation Requested [POST /webhooks/tms/recalculation-requested]

TMS driver requests a POS recalculation at the customer's door (POD only). Used when the actual delivered weight differs from the ordered quantity (e.g. weight-based items). OMS calls POS outbound and returns the adjusted amount so the driver knows the correct amount to collect. Only valid when order is `OutForDelivery`.

**Error 404:** `tracking_not_found`

**Error 422:** Invalid transition — order is not `OutForDelivery`

+ Request (application/json)
    + Body

            {
              "trackingId": "TRK-2024-005",
              "reason": "ActualWeightDiffers",
              "actualWeight": 0.84123,
              "requestedAt": "2024-01-15T14:30:00Z"
            }

+ Response 202 (application/json)
    + Body

            { "accepted": true, "orderId": "ORD-005", "adjustedAmount": 106.84 }

+ Response 404 (application/json)
    + Body

            { "error": "tracking_not_found", "detail": "Tracking ID TRK-2024-005 not found." }

+ Response 422 (application/json)
    + Body

            { "error": "invalid_transition", "detail": "Order is not in OutForDelivery status." }

---

## Group STS Webhooks

Inbound callbacks from the Settlement & Tax System (STS). STS generates official ABB/Tax Invoice and Credit Note documents and notifies OMS with download links.

**How STS is triggered (external to OMS):**

OMS dispatches outbox events to Gateway at key status transitions. Gateway handles payment and tax processing outside OMS. Once Gateway's processing is complete, STS sends the ABB/Tax Invoice (and optionally a Credit Note) back to OMS.

| Flow | OMS outbox event that triggers Gateway | Gateway processing | STS sends to OMS |
|---|---|---|---|
| **Prepaid** | `PickConfirmedSentToGateway` (after `PickConfirmed`) | Gateway handles payment settlement externally | ABB/Tax Invoice or Credit Note |
| **POD** | `DeliveredSentToGateway` (after `Delivered`) | Gateway handles COD/payment collection externally | ABB/Tax Invoice or Credit Note |

OMS routes the received STS documents to downstream systems based on `orders.payment_flow`:

| Flow | Trigger point | Invoice forwarded to | Credit Note forwarded to |
|---|---|---|---|
| **Pre-paid** (`payment_flow = "PRE_PAID"`) | After `PickConfirmed`, before TMS dispatch | WMS + Gateway | WMS + Gateway |
| **POD** — Pay On Delivery (`payment_flow = "PAY_ON_DELIVERY"`) | After `Delivered` | TMS + Gateway | TMS + Gateway |

**Shared STS webhook headers:**

| Header | Description |
|---|---|
| `X-Source-System` | Always `STS` |
| `X-Idempotency-Key` | UUID — duplicate requests with same key are ignored |
| `X-Webhook-Signature` | HMAC-SHA256 of request body using shared secret |

---

### STS ABB Tax Invoice [POST /webhooks/sts/abb-tax-invoice]

STS sends the ABB/Tax Invoice document link. Timing and forwarding targets differ by payment type.

+ Request (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "invoiceNumber": "ABB-2024-001",
              "invoiceLink": "https://sts.example.com/invoices/ABB-2024-001.pdf",
              "amount": 23.80,
              "currency": "THB",
              "issuedAt": "2024-01-15T16:00:00Z"
            }

+ Response 202 (application/json)
    + Body

            {
              "accepted": true,
              "orderId": "ORD-001",
              "invoiceNumber": "ABB-2024-001",
              "invoiceId": "inv-001",
              "forwardedTo": ["WMS", "Gateway"]
            }

+ Response 409 (application/json)
    + Body

            { "error": "conflict", "detail": "ABB/Tax Invoice ABB-2024-001 already received for ORD-001." }

---

### STS Credit Note [POST /webhooks/sts/credit-note]

STS sends a Credit Note document link as a separate webhook when a credit note exists for the order. Pre-paid: forwards to WMS and Gateway. POD: forwards to TMS and Gateway.

+ Request (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "creditNoteNumber": "CN-2024-001",
              "creditNoteLink": "https://sts.example.com/credit-notes/CN-2024-001.pdf",
              "amount": 200,
              "currency": "THB",
              "issuedAt": "2024-01-15T16:05:00Z"
            }

+ Response 202 (application/json)
    + Body

            {
              "accepted": true,
              "orderId": "ORD-001",
              "creditNoteNumber": "CN-2024-001",
              "forwardedTo": ["WMS"]
            }

+ Response 409 (application/json)
    + Body

            { "error": "conflict", "detail": "Credit Note CN-2024-001 already received for ORD-001." }

---

### STS ABB Tax Invoice Received [POST /webhooks/sts/abb-tax-invoice-received]

STS sends the official ABB/Tax Invoice to OMS. Timing and forwarding targets differ by payment method: Prepaid invoices arrive after `PickConfirmed`; POD invoices arrive after `Delivered`.

Note: `invoiceLink` is required for POD (the link is forwarded to TMS and Gateway). For Prepaid, only `invoiceAmount` and `invoiceNumber` are forwarded to WMS.

**Routing by payment method:**
- `paymentMethod = 'Prepaid'`: dispatches `ABBTaxInvoiceSentToWMS` → WMS and `ABBTaxInvoiceSentToGateway` → Gateway
- `paymentMethod = 'POD'`: dispatches `ABBTaxInvoiceSentToTMS` → TMS and `ABBTaxInvoiceSentToGateway` → Gateway

+ Request (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "invoiceNumber": "INV-STS-001",
              "invoiceAmount": 2380.00,
              "currency": "THB",
              "invoiceLink": "https://sts.example.com/invoices/INV-STS-001.pdf",
              "issuedAt": "2024-01-15T16:00:00Z"
            }

+ Response 202

---

### STS Credit Note Received [POST /webhooks/sts/credit-note-received]

STS issues a credit note to OMS. Optional — only dispatched when a credit note exists for the order (e.g. price adjustment after substitution or partial pick). Forwarding target depends on payment method.

**Note:** The request field is `amount` (not `creditAmount` — that was a previous naming that caused a 0-amount bug and has been corrected).

**Routing by payment method:**
- `paymentMethod = 'Prepaid'`: dispatches `CreditNoteSentToWMS` → WMS and `CreditNoteSentToGateway` → Gateway
- `paymentMethod = 'POD'`: dispatches `CreditNoteSentToTMS` → TMS and `CreditNoteSentToGateway` → Gateway

+ Request (application/json)
    + Body

            {
              "orderId": "ORD-001",
              "creditNoteNumber": "CN-001",
              "amount": 44.00,
              "currency": "THB",
              "creditNoteLink": "https://sts.example.com/credit-notes/CN-001.pdf",
              "issuedAt": "2024-01-15T16:05:00Z"
            }

+ Response 202

---

## Group Configuration Management

### Outbox Routing Rules

Endpoints for managing `config.outbox_routing_rules` — the table that drives dynamic outbox dispatch. The outbox worker matches `(channel_type, business_unit, trigger_event)` to resolve which target systems receive an event. Exact matches take precedence over wildcard `*` entries; all matching rules are dispatched, ordered by `execution_order`. If no rule matches, no outbox event is dispatched (safe opt-out via config).

**OutboxRoutingRule response shape:**

```json
{
  "rule_id": 1,
  "channel_type": "Marketplace",
  "business_unit": "TikTok",
  "trigger_event": "PickConfirmedEvent",
  "target_system": "Marketplace",
  "endpoint_key": "tiktok.pick-confirm",
  "execution_order": 2,
  "is_active": true
}
```

| Field | Type | Description |
|---|---|---|
| `rule_id` | bigint | Auto-generated primary key |
| `channel_type` | string | Channel this rule applies to; use `"*"` to match all channels |
| `business_unit` | string | Business unit this rule applies to; use `"*"` to match all BUs |
| `trigger_event` | string | Domain event name that triggers dispatch |
| `target_system` | string | Downstream system: `WMS`, `TMS`, `Marketplace`, `Gateway`, etc. |
| `endpoint_key` | string | ACL adapter key resolved to a real URL (e.g. `tiktok.pick-confirm`) |
| `execution_order` | int | Ascending sort order when multiple rules match the same trigger |
| `is_active` | bool | `false` = soft-deleted / opted out; worker skips inactive rules |

---

### List Outbox Routing Rules [GET /config/outbox-routing-rules]

Return all outbox routing rules.

+ Response 200 (application/json)
    + Body

            { "data": [ { "rule_id": 1, "channel_type": "Marketplace", "business_unit": "TikTok", "trigger_event": "PickConfirmedEvent", "target_system": "Marketplace", "endpoint_key": "tiktok.pick-confirm", "execution_order": 2, "is_active": true } ] }

---

### Get Outbox Routing Rule [GET /config/outbox-routing-rules/{ruleId}]

Return a single outbox routing rule by ID.

+ Parameters
    + ruleId (required, number) - Rule ID

+ Response 200 (application/json)
    + Body

            { "data": { "rule_id": 1, "channel_type": "Marketplace", "business_unit": "TikTok", "trigger_event": "PickConfirmedEvent", "target_system": "Marketplace", "endpoint_key": "tiktok.pick-confirm", "execution_order": 2, "is_active": true } }

+ Response 404 (application/json)
    + Body

            { "error_code": "not_found", "message": "Rule 999 not found.", "trace_id": "abc-123" }

---

### Create Outbox Routing Rule [POST /config/outbox-routing-rules]

Create a new outbox routing rule.

| Field | Type | Required | Description |
|---|---|---|---|
| `channel_type` | string | Yes | Channel type or `"*"` for all channels |
| `business_unit` | string | Yes | Business unit or `"*"` for all BUs |
| `trigger_event` | string | Yes | Domain event name |
| `target_system` | string | Yes | Downstream system identifier |
| `endpoint_key` | string | Yes | ACL adapter endpoint key |
| `execution_order` | int | Yes | Dispatch order when multiple rules match |
| `is_active` | bool | Yes | Set `true` to activate immediately |

+ Request (application/json)
    + Body

            {
              "channel_type": "Marketplace",
              "business_unit": "TikTok",
              "trigger_event": "PickConfirmedEvent",
              "target_system": "Marketplace",
              "endpoint_key": "tiktok.pick-confirm",
              "execution_order": 2,
              "is_active": true
            }

+ Response 201 (application/json)
    + Body

            { "data": { "rule_id": 1, "channel_type": "Marketplace", "business_unit": "TikTok", "trigger_event": "PickConfirmedEvent", "target_system": "Marketplace", "endpoint_key": "tiktok.pick-confirm", "execution_order": 2, "is_active": true } }

---

### Replace Outbox Routing Rule [PUT /config/outbox-routing-rules/{ruleId}]

Replace an existing outbox routing rule. All fields are replaced.

+ Parameters
    + ruleId (required, number) - Rule ID

+ Request (application/json)
    + Body

            {
              "channel_type": "Marketplace",
              "business_unit": "TikTok",
              "trigger_event": "PickConfirmedEvent",
              "target_system": "Marketplace",
              "endpoint_key": "tiktok.pick-confirm",
              "execution_order": 2,
              "is_active": true
            }

+ Response 200 (application/json)
    + Body

            { "data": { "rule_id": 1, "channel_type": "Marketplace", "business_unit": "TikTok", "trigger_event": "PickConfirmedEvent", "target_system": "Marketplace", "endpoint_key": "tiktok.pick-confirm", "execution_order": 2, "is_active": true } }

+ Response 404 (application/json)
    + Body

            { "error_code": "not_found", "message": "Rule 999 not found.", "trace_id": "abc-123" }

---

### Deactivate Outbox Routing Rule [DELETE /config/outbox-routing-rules/{ruleId}]

Soft-delete a routing rule: sets `is_active = false`. The worker will stop dispatching events for this rule. The record is retained for audit.

+ Parameters
    + ruleId (required, number) - Rule ID

+ Response 200 (application/json)
    + Body

            { "data": { "message": "Rule deactivated" } }

+ Response 404 (application/json)
    + Body

            { "error_code": "not_found", "message": "Rule 999 not found.", "trace_id": "abc-123" }

---

## Error Envelope

All error responses share this shape:

```json
{ "error": "<error_code>", "detail": "<human-readable message>" }
```

| Code | HTTP Status | Meaning |
|---|---|---|
| `not_found` | 404 | Resource does not exist |
| `conflict` | 409 | Duplicate `sourceOrderId` |
| `invalid_transition` | 409 | State machine guard rejected the transition |
| `unprocessable` | 422 | Business rule violation |
| `invalid_parameter` | 400 | Bad query parameter |
