# Sprint Connect OMS — API Blueprint

**Version:** 2.0  
**Format:** REST / JSON  
**Host:** `https://api.sprintconnect.io/v1`  
**Auth:** Bearer JWT on all endpoints (except `POST /auth/token`)

---

## Authentication

### POST /auth/token

Obtain a Bearer JWT for API access.

**Request:**
```json
{
  "clientId": "oms-client-01",
  "clientSecret": "s3cr3t",
  "scope": "orders:read orders:write"
}
```

**Response 200:**
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiJ9...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

---

## Group: Orders

### GET /orders

List orders (paginated). Used by the Kanban Board. (UC17)

**Query parameters:** `status`, `store`, `type` (fulfillment type), `channel` (channel type — e.g. `Marketplace`, `Gateway`, `Web`), `page` (default 1), `limit` (max 200, default 50)

**Response 200:**
```json
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
      "totalAmount": 2380,
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
```

---

### POST /orders

Create a new outbound order. (UC1)

Idempotent on `sourceOrderId` — duplicate calls with the same value return the existing order.

**Request:**
```json
{
  "sourceOrderId": "EXT-001",
  "channelType": "App",
  "businessUnit": "TOPS",
  "storeId": "store-central-dc",
  "fulfillmentType": "Delivery",
  "paymentMethod": "Prepaid",
  "isPrepaid": true,
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
      "unitPrice": 120,
      "unitOfMeasure": "Each"
    }
  ]
}
```

**Response 201:**
```json
{
  "id": "ORD-001",
  "orderNumber": "ORD-001",
  "status": "Pending",
  "createdAt": "2024-01-15T14:00:00Z"
}
```

**`paymentMethod`** field values:
- `"Prepaid"` — slot pre-booked; ABB/Tax Invoice issued after PickConfirmed; forwarded to WMS and Gateway
- `"POD"` — Pay on Delivery; invoice issued after Delivered; ABB/Tax Invoice forwarded to TMS + Gateway

**Outbox events dispatched on order creation:**

| Event | Target | Condition |
|---|---|---|
| `SaleOrderSentToWMS` | WMS | All orders |
| `SaleOrderSentToTMS` | TMS | All orders — dispatched at order creation for transport scheduling |

**Response 409** (duplicate `sourceOrderId`):
```json
{ "error": "conflict", "detail": "Order with sourceOrderId EXT-001 already exists as ORD-001." }
```

---

### GET /orders/{id}

Get full order detail. (UC16)

**Response 200:**
```json
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
      "unitPrice": 120,
      "recalculatedUnitPrice": 108,
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
  "totalAmount": 2380,
  "currency": "THB",
  "createdAt": "2024-01-15T14:00:00Z",
  "updatedAt": "2024-01-15T15:31:00Z"
}
```

---

### GET /orders/{id}/lines

List order lines with picked quantities and recalculated prices. (UC16)

**Response 200:** `{ "orderId": "ORD-001", "lines": [ ... ] }` — same shape as `lines[]` in GET /orders/{id}.

---

### GET /orders/{id}/packages

List packages with tracking IDs and carrier details. (UC18)

**Response 200:** `{ "orderId": "ORD-001", "packages": [ ... ] }` — same shape as `packages[]` in GET /orders/{id}.

---

### GET /orders/{id}/webhooks

List all webhook events received for an order.

**Response 200:**
```json
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
```

---

### GET /orders/{id}/credit-note

Get the credit note associated with an order. Returns the credit note issued by STS for this order — for example, after a substitution where the replacement item is cheaper than the original (UC11), or after a partial pick adjustment.

Returns `404` if no credit note exists for the order.

**Response 200:**
```json
{
  "creditNoteId": "CN-001",
  "creditNoteNumber": "CN-UC11-1716000000000",
  "invoiceId": "inv-001",
  "amount": 4400,
  "currency": "THB",
  "reason": "PriceAdjustment",
  "status": "Issued",
  "creditNoteLink": "https://sts.example.com/cn/UC11.pdf",
  "sourceStsRef": "STS-CN-REF-001",
  "issuedAt": "2024-01-15T16:05:00Z"
}
```

**Response 404:**
```json
{ "error": "not_found", "detail": "No credit note exists for order ORD-001." }
```

| Field | Type | Description |
|---|---|---|
| `creditNoteId` | string | OMS internal credit note ID |
| `creditNoteNumber` | string | Fiscal credit note number from STS |
| `invoiceId` | string | The invoice being partially reversed |
| `amount` | number | Credit amount in satang (smallest THB unit) |
| `currency` | string | Currency code — `THB` |
| `reason` | string | Why the credit note was issued — e.g. `PriceAdjustment`, `CustomerRejection` |
| `status` | string | `Issued`, `Applied`, or `Cancelled` |
| `creditNoteLink` | string | URL to the Credit Note PDF hosted by STS |
| `sourceStsRef` | string | STS reference ID for reconciliation |
| `issuedAt` | timestamp | When STS issued the credit note (ISO 8601 UTC) |

---

### GET /orders/{id}/substitutions

List substitutions offered by WMS. (UC5)

**Response 200:**
```json
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
      "substituteUnitPrice": 55,
      "substitutedAmount": 1,
      "customerApproved": null,
      "approvedAt": null,
      "createdAt": "2024-01-15T15:10:00Z"
    }
  ]
}
```

---

### POST /orders/{id}/substitutions/{subId}/approve

Customer approves a proposed substitution. (UC5)

**Response 200:** `{ "substitutionId": "sub-001", "customerApproved": true, "approvedAt": "..." }`

---

### POST /orders/{id}/substitutions/{subId}/reject

Customer rejects a substitution; original line is voided. (UC5)

**Response 200:** `{ "substitutionId": "sub-001", "customerApproved": false, "approvedAt": "..." }`

---

### GET /orders/{id}/timeline

Chronological event history combining domain transitions, inbound webhooks, and outbox events. (UC16)

**Response 200:**
```json
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
```

**Event types:**

| `type` | Source table |
|---|---|
| `domain` | `orders.order_status_history` |
| `webhook` | `orders.order_webhook_logs` |
| `outbox` | `orders.order_outbox` |
| `bridge` | Derived marker — synthesized between PO put-away and first order event |

---

### PATCH /orders/{id}/hold

Place order on hold. Saves `pre_hold_status`. (UC6)

**Request:** `{ "holdReason": "ManualReview", "heldBy": "ops-agent-01" }`

**Response 200:** `{ "id": "ORD-001", "newStatus": "OnHold", "preHoldStatus": "PickStarted" }`

---

### PATCH /orders/{id}/release-hold

Release hold; restores `pre_hold_status`. (UC6)

**Request:** `{ "releasedBy": "ops-agent-01" }`

**Response 200:** `{ "id": "ORD-001", "newStatus": "PickStarted" }`

---

### PATCH /orders/{id}/cancel

Cancel order. Allowed from `Pending`, `BookingConfirmed`, `OnHold` only. (UC9)

**Request:** `{ "reason": "CustomerRequest", "cancelledBy": "ops-agent-01" }`

**Response 200:** `{ "id": "ORD-005", "newStatus": "Cancelled" }`

**Outbox events dispatched on cancellation (all three are appended atomically):**

| Event | Target | Purpose |
|---|---|---|
| `OrderCancelledSentToWMS` | WMS | Reverse stock reservation |
| `OrderCancelledSentToTMS` | TMS | Cancel delivery booking |
| `OrderCancelledSentToGateway` | Gateway | Notify customer of cancellation |

All three events appear on `GET /orders/{id}/timeline` with `type: "outbox"` and the respective `system` values (`"WMS"`, `"TMS"`, `"Gateway"`).

**Response 409:**
```json
{ "error": "invalid_transition", "detail": "Order ORD-005 is in status Delivered. Cancellation is not allowed from this state." }
```

---

### POST /orders/{id}/recalculate

Manually trigger a POS recalculation. OMS calls POS API outbound synchronously and returns the adjusted amount. (UC15)

**Response 202:**
```json
{ "orderId": "ORD-009", "adjustedAmount": 19800, "recalcTriggeredAt": "2024-01-15T15:30:00Z" }
```

---

### PATCH /orders/{id}/partial-pick

Record a partial pick: one or more order lines were picked in lesser quantity than ordered. OMS calls POS outbound synchronously to recalculate. (UC-PARTPICK)

Not allowed after `PickConfirmed`.

**Request:**
```json
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
```

**Response 200:**
```json
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
```

**Error 409:** `pos_recalc_already_pending` — POS recalculation already in progress

**Error 409:** `invalid_transition` — Order not in a state that allows partial pick

**Error 422:** `zero_pick_not_allowed` — All lines reduced to zero; use Cancel Order instead

---

### GET /orders/{id}/delivery-slot

Get current delivery slot. (UC19)

**Response 200:**
```json
{
  "orderId": "ORD-001",
  "slotId": "slot-001",
  "scheduledStart": "2024-01-15T18:00:00Z",
  "scheduledEnd": "2024-01-15T20:00:00Z",
  "storeId": "store-central-dc"
}
```

---

### PATCH /orders/{id}/delivery-slot

Reschedule delivery window. Not allowed once order is `OutForDelivery`, `Delivered`, or later. (UC19, UC-RESCHEDULE)

**Request:**
```json
{
  "scheduledStart": "2024-01-15T20:00:00Z",
  "scheduledEnd": "2024-01-15T22:00:00Z",
  "bookedVia": "TMS",
  "bookingRef": "TMS-BK-002",
  "reason": "CustomerRequest"
}
```

**Response 200:**
```json
{
  "orderId": "ORD-001",
  "deliverySlot": {
    "scheduledStart": "2024-01-15T20:00:00Z",
    "scheduledEnd": "2024-01-15T22:00:00Z"
  }
}
```

**Outbox event dispatched:** `DeliverySlotRescheduledEvent` → TMS

**Response 409:**
```json
{ "error": "slot_change_not_allowed", "detail": "Order ORD-001 is already OutForDelivery. Slot cannot be changed." }
```

---

### POST /orders/{id}/invoice/prepaid

Send pre-delivery ABB/Tax Invoice to WMS. Prepaid orders only — called after PickConfirmed and before TMS dispatch. (UC28)

**Response 202:**
```json
{ "orderId": "ORD-001", "invoiceNumber": "INV-PRE-001", "invoicedAt": "2024-01-15T09:46:00Z" }
```

---

## Group: Returns

### POST /returns

Initiate a return for a delivered or paid order. (UC14)

**Request:**
```json
{
  "orderId": "ORD-001",
  "returnReason": "WrongItem",
  "items": [
    { "orderLineId": "line-001", "sku": "APPLE-1KG", "quantity": 2, "itemReason": "WrongItem" }
  ],
  "requestedBy": "alice@example.com"
}
```

**Response 201:**
```json
{
  "id": "RET-001",
  "returnOrderNumber": "RET-001",
  "orderId": "ORD-001",
  "status": "ReturnRequested",
  "createdAt": "2024-01-15T20:00:00Z"
}
```

**Response 422:**
```json
{ "error": "unprocessable", "detail": "Order ORD-005 is in status Cancelled. Returns are only allowed from Delivered, Invoiced, or Paid." }
```

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

---

### GET /returns

List returns. Query params: `orderId`, `status`, `page`, `limit`.

**Response 200:** Paginated `items[]` array with `id`, `returnOrderNumber`, `orderId`, `status`, `returnReason`, `requestedAt`, `refundedAt`, `createdAt`, `updatedAt`.

---

### GET /returns/{id}

Get full return detail. (UC14)

**Response 200:**
```json
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
```

---

### GET /returns/{id}/items

List items in a return, including condition and put-away location. (UC14)

**Response 200:**
```json
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
      "unitPrice": 120,
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
```

---

### PATCH /returns/{id}/cancel

Cancel return (allowed from `ReturnRequested` or `PickupScheduled` only). (UC14)

**Request:** `{ "reason": "CustomerChangedMind", "cancelledBy": "ops-agent-01" }`

**Response 200:** `{ "id": "RET-001", "newStatus": "Cancelled" }`

---

### GET /returns/{id}/refund

Get refund and credit note for a completed return. (UC14)

**Response 200:**
```json
{
  "returnId": "RET-001",
  "refund": {
    "refundId": "ref-001",
    "refundAmount": 240,
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
    "amount": 240,
    "currency": "THB",
    "reason": "Return",
    "status": "Issued"
  }
}
```

---

## Group: Inbound

### GET /inbound/purchase-orders

List Purchase Orders. Query params: `status`, `store`, `page`, `limit`. (UC21)

**Response 200:** Paginated `items[]` with `id`, `poNumber`, `supplier`, `supplierId`, `lines`, `status`, `store`, `value`, `goodsReceiveNo`, `createdAt`, `updatedAt`.

---

### POST /inbound/purchase-orders

Create a Purchase Order. Triggers `PurchaseOrderCreatedEvent` → WMS. (UC21)

**Request:**
```json
{
  "poNumber": "PO-005",
  "supplierId": "sup-fresh-foods",
  "storeId": "store-central-dc",
  "lines": [
    { "sku": "APPLE-1KG", "orderedQty": 20, "unitCost": 45, "currency": "THB" }
  ]
}
```

**Response 201:** `{ "id": "PO-005", "poNumber": "PO-005", "status": "Created", "createdAt": "..." }`

---

### GET /inbound/purchase-orders/{id}

Get PO detail including all lines with received quantities and conditions. (UC21)

**Response 200:**
```json
{
  "id": "PO-001",
  "poNumber": "PO-001",
  "supplier": "Fresh Foods Ltd",
  "store": "Central DC",
  "storeId": "store-central-dc",
  "status": "Closed",
  "value": 45000,
  "goodsReceiveNo": "GRN-2024-001",
  "lines": [
    {
      "poLineId": "pol-001",
      "sku": "APPLE-1KG",
      "productName": "Apple (1 kg bag)",
      "orderedQty": 10,
      "receivedQty": 10,
      "unitCost": 45,
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
```

---

### GET /inbound/purchase-orders/{id}/goods-receipts

List goods receipt records for a PO. (UC21)

**Response 200:** `{ "purchaseOrderId": "PO-001", "goodsReceipts": [ { "goodsReceiveNo", "status", "receivedAt", "putAwayAt", "lines": [...] } ] }`

---

### GET /inbound/transfer-orders

List Transfer Orders. Query params: `status`, `sourceStore`, `destStore`, `page`, `limit`. (UC22)

**Response 200:** Paginated `items[]` with `id`, `transferNumber`, `source`, `sourceStoreId`, `dest`, `destStoreId`, `lines`, `status`, `tracking`, `createdAt`, `updatedAt`.

---

### POST /inbound/transfer-orders

Create a Transfer Order. Triggers `TransferOrderCreatedEvent` → source WMS. (UC22)

**Request:**
```json
{
  "sourceStoreId": "store-central-dc",
  "destStoreId": "store-b",
  "lines": [ { "sku": "APPLE-1KG", "requestedQty": 6 } ]
}
```

**Response 201:** `{ "id": "TR-005", "transferNumber": "TR-005", "status": "Created", "createdAt": "..." }`

---

### GET /inbound/transfer-orders/{id}

Get Transfer Order detail including lines and quantities. (UC22)

**Response 200:** Full TO object with `lines[]` containing `toLineId`, `sku`, `productName`, `requestedQty`, `transferredQty`, `confirmedAt`.

---

### GET /inbound/transfer-orders/{id}/confirmations

List pick and receipt confirmations for a Transfer Order. (UC22)

**Response 200:**
```json
{
  "transferOrderId": "TR-001",
  "confirmations": [
    { "type": "PickConfirmed", "confirmedAt": "2024-01-15T11:00:00Z", "confirmedBy": "WMS", "tracking": "TRK-TR-001" },
    { "type": "TransferReceived", "confirmedAt": "2024-01-15T14:30:00Z", "confirmedBy": "WMS", "tracking": "TRK-TR-001" }
  ]
}
```

---

## Group: Stock

### GET /stock/{sku}/ledger

Per-SKU stock movement ledger across locations. OMS does not own inventory counts — this reflects events OMS recorded. (UC24)

**Query params:** `storeId` (optional filter), `from` (ISO 8601 date), `to` (ISO 8601 date)

**Response 200:**
```json
{
  "sku": "APPLE-1KG",
  "skuName": "Apple (1 kg bag)",
  "unitPrice": 120,
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
```

---

## Group: Webhooks (Inbound)

All webhook endpoints return `202 Accepted`. Duplicate `X-Idempotency-Key` values are ignored.

**Required headers on all webhooks:**

| Header | Description |
|---|---|
| `X-Source-System` | `WMS`, `TMS`, or `POS` |
| `X-Idempotency-Key` | UUID |
| `X-Webhook-Signature` | HMAC-SHA256 of body using shared secret |

---

### POST /webhooks/wms/booking-confirmed

WMS confirms stock reserved. → `BookingConfirmed`. (UC2)

**Request:** `{ "orderId": "ORD-001", "wmsBookingRef": "WMS-BK-001", "confirmedAt": "..." }`

**Response 202:** `{ "accepted": true, "orderId": "ORD-001", "newStatus": "BookingConfirmed" }`

---

### POST /webhooks/wms/pick-started

WMS picker begins collecting items. → `PickStarted`. (UC3)

**Request:** `{ "orderId": "ORD-001", "pickerId": "picker-01", "startedAt": "..." }`

**Response 202:** `{ "accepted": true, "orderId": "ORD-001", "newStatus": "PickStarted" }`

**Outbox events dispatched:**
- `PickStartedSentToTMS` → TMS (all payment methods — allows TMS to prepare delivery logistics in advance)

---

### POST /webhooks/wms/wave-started

WMS notifies OMS that wave picking has started. (UC-WAVE)

Valid only when order is in `PickStarted` status.

**Headers:** `X-Idempotency-Key: <uuid>`

**Request:**
```json
{
  "orderId": "ORD-001",
  "waveId": "WAVE-001",
  "startedAt": "2024-01-15T15:35:00Z"
}
```

**Response 202 Accepted**

**Outbox event dispatched:** `WaveStartedSentToGateway` → Gateway

---

### POST /webhooks/wms/pick-confirmed

WMS reports actual picked quantities per line. Triggers POS recalculation if any quantity differs or substitution exists. → `PickConfirmed`. (UC4)

**Request:**
```json
{
  "orderId": "ORD-001",
  "lines": [ { "orderLineId": "line-001", "sku": "APPLE-1KG", "pickedQty": 5, "substituted": false } ],
  "pickedAt": "2024-01-15T15:31:00Z"
}
```

**Response 202:** `{ "accepted": true, "orderId": "ORD-001", "newStatus": "PickConfirmed" }`

---

### POST /webhooks/wms/packed

WMS confirms order packed into packages. → `Packed`. (UC4)

**Request:**
```json
{
  "orderId": "ORD-001",
  "packages": [ { "trackingId": "TRK-2024-001", "vehicleType": "Van", "weight": 2.5, "lineIds": ["line-001"] } ],
  "packedAt": "2024-01-15T17:30:00Z"
}
```

**Response 202:** `{ "accepted": true, "orderId": "ORD-001", "newStatus": "Packed", "packagesCreated": 1 }`

---

### POST /webhooks/wms/substitution-offered

WMS offers alternative SKU for unfulfillable line. Sets `substitution_flag = true`. (UC5)

**Request:**
```json
{
  "orderId": "ORD-003",
  "orderLineId": "line-003",
  "substituteSku": "MILK-2L",
  "substituteProductName": "Whole Milk (2L)",
  "substituteUnitPrice": 55,
  "substitutedAmount": 1,
  "offeredAt": "2024-01-15T15:10:00Z"
}
```

**Response 202:** `{ "accepted": true, "substitutionId": "sub-001", "orderId": "ORD-003", "customerNotified": true }`

---

### POST /webhooks/wms/put-away-confirmed

WMS confirms returned items are shelved. Atomically: transitions the **return record** to `PutAway`, transitions the **linked order** from `Delivered` to `Returned`, and initiates the refund calculation. (UC12, UC14)

**Request:**
```json
{
  "returnId": "RET-001",
  "items": [
    { "sku": "APPLE-1KG", "condition": "Resellable", "sloc": "B-05", "quantity": 2, "performedBy": "wms-picker-07" }
  ],
  "putAwayAt": "2024-01-15T11:00:00Z"
}
```

**Response 202:** `{ "accepted": true, "returnId": "RET-001", "newReturnStatus": "PutAway", "refundInitiated": true, "creditNoteId": "CN-RET-001" }`

**Side effects (all atomic in the same DB transaction):**

| Effect | Detail |
|---|---|
| Return record status | `Requested` → `PutAway` |
| **Order status** | `Delivered` → **`Returned`** |
| Refund record | Created with `status: Pending` |
| Credit note | Created or linked if applicable |

The order status transition to `Returned` was added to fix a bug where the order remained in `Delivered` after the return was put away. Calling `GET /orders/{orderId}` after this webhook now returns `status: "Returned"`.

---

### POST /webhooks/wms/goods-receipt-confirmed

WMS confirms goods physically received at dock against a PO. (UC21)

**Request:**
```json
{
  "purchaseOrderId": "PO-001",
  "goodsReceiveNo": "GRN-2024-001",
  "lines": [ { "sku": "APPLE-1KG", "receivedQty": 10 } ],
  "receivedAt": "2024-01-15T09:28:00Z"
}
```

**Response 202:** `{ "accepted": true, "purchaseOrderId": "PO-001", "newStatus": "FullyReceived" }`

---

### POST /webhooks/wms/purchase-order-put-away-confirmed

WMS confirms inbound goods shelved. Closes PO. Signals stock available. (UC21)

**Request:**
```json
{
  "purchaseOrderId": "PO-001",
  "items": [ { "sku": "APPLE-1KG", "condition": "Resellable", "sloc": "A-12", "qty": 10 } ],
  "putAwayAt": "2024-01-15T10:14:00Z"
}
```

**Response 202:** `{ "accepted": true, "purchaseOrderId": "PO-001", "newStatus": "Closed" }`

---

### POST /webhooks/wms/transfer-pick-confirmed

WMS at source store confirms items picked and packed for transfer. Triggers TMS dispatch. (UC22)

**Request:**
```json
{
  "transferOrderId": "TR-001",
  "lines": [ { "sku": "APPLE-1KG", "transferredQty": 4 } ],
  "confirmedAt": "2024-01-15T11:00:00Z"
}
```

**Response 202:** `{ "accepted": true, "transferOrderId": "TR-001", "newStatus": "PickConfirmed" }`

---

### POST /webhooks/wms/transfer-received

WMS at destination confirms stock arrived and put away. Completes Transfer Order. (UC22)

**Request:** `{ "transferOrderId": "TR-001", "receivedAt": "2024-01-15T14:30:00Z" }`

**Response 202:** `{ "accepted": true, "transferOrderId": "TR-001", "newStatus": "Completed" }`

---

### POST /webhooks/wms/damaged-goods-received

WMS checks in a damaged package returned by TMS driver. Order → `OnHold (PackageDamaged)`. (UC23)

**Request:**
```json
{
  "orderId": "ORD-006",
  "trackingId": "TRK-2024-006",
  "receivedAt": "2024-01-15T12:00:00Z",
  "items": [
    { "sku": "APPLE-1KG", "condition": "Repairable", "sloc": null, "quantity": 3 }
  ]
}
```

**Response 202:** `{ "accepted": true, "orderId": "ORD-006", "damagedReceiptId": "DMG-001", "newOrderStatus": "OnHold", "holdReason": "PackageDamaged" }`

---

### POST /webhooks/wms/damaged-goods-put-away

WMS confirms damaged items inspected, condition assigned, shelved/disposed. (UC23)

**Request:**
```json
{
  "damagedReceiptId": "DMG-001",
  "items": [ { "sku": "APPLE-1KG", "condition": "Repairable", "sloc": "DMG-01", "quantity": 12 } ],
  "putAwayAt": "2024-01-15T12:00:00Z",
  "updatedBy": "wms-inspector-02"
}
```

**Response 202:** `{ "accepted": true, "damagedReceiptId": "DMG-001", "newStatus": "PutAway" }`

---

### POST /webhooks/tms/slot-rescheduled

TMS notifies OMS that the delivery slot has been rescheduled by the customer. Updates the delivery slot and notifies WMS. Not allowed once the order is `OutForDelivery` or later. (UC8)

**Request:**
```json
{
  "orderId": "ORD-001",
  "newScheduledStart": "2024-01-15T20:00:00Z",
  "newScheduledEnd": "2024-01-15T21:00:00Z",
  "bookingRef": "TMS-RESCHEDULE-001",
  "reason": "CustomerRequest",
  "rescheduledAt": "2024-01-15T14:00:00Z"
}
```

**Response 202:** `{ "accepted": true, "orderId": "ORD-001", "deliverySlot": { "scheduledStart": "2024-01-15T20:00:00Z", "scheduledEnd": "2024-01-15T21:00:00Z" } }`

**Outbox event dispatched:** `DeliverySlotRescheduledEvent` → WMS

**Response 409:**
```json
{ "error": "slot_change_not_allowed", "detail": "Order ORD-001 is already OutForDelivery. Slot cannot be changed." }
```

---

### POST /webhooks/tms/package-dispatched

TMS driver collected the package. Transitions order to `OutForDelivery`. (UC7)

**Request:** `{ "trackingId": "TRK-2024-001", "dispatchedAt": "2024-01-15T17:47:00Z" }`

**Response 202:** `{ "accepted": true, "orderId": "ORD-001", "newOrderStatus": "OutForDelivery", "newPackageStatus": "OutForDelivery" }`

**Outbox event dispatched:** `OutForDeliverySentToGateway` → Gateway

---

### POST /webhooks/tms/package-delivered

TMS confirms delivery to customer. Triggers invoice generation. → `Delivered`. (UC8)

**Request:**
```json
{
  "trackingId": "TRK-2024-001",
  "deliveredAt": "2024-01-15T19:22:00Z",
  "recipientName": "Alice Johnson",
  "proofOfDelivery": "https://tms.example.com/pod/TRK-2024-001.jpg"
}
```

**Response 202:** `{ "accepted": true, "orderId": "ORD-001", "newStatus": "Delivered", "invoiceTriggered": true }`

**Outbox events dispatched:**
- `DeliveredSentToGateway` → Gateway (all payment methods)

---

### POST /webhooks/tms/package-damage-reported

TMS driver reports damage before/during delivery. Order → `OnHold`. Driver instructed to return goods to warehouse. (UC20)

**Request:**
```json
{
  "trackingId": "TRK-2024-006",
  "reason": "PackageDamaged",
  "driverNote": "Box crushed during transport",
  "reportedAt": "2024-01-15T10:45:00Z"
}
```

**Response 202:** `{ "accepted": true, "orderId": "ORD-006", "newOrderStatus": "OnHold", "holdReason": "PackageDamaged", "preHoldStatus": "OutForDelivery" }`

---

### POST /webhooks/tms/recalculation-requested

TMS driver requests a POS recalculation at the customer's door (POD only). Used when the actual delivered weight differs from the ordered quantity (e.g. weight-based items). OMS calls POS outbound and returns the adjusted amount so the driver knows the correct amount to collect. Only valid when order is `OutForDelivery`.

**Request:**
```json
{
  "trackingId": "TRK-2024-005",
  "reason": "ActualWeightDiffers",
  "actualWeight": 0.84123,
  "requestedAt": "2024-01-15T14:30:00Z"
}
```

**Response 202:** `{ "accepted": true, "orderId": "ORD-005", "adjustedAmount": 10684 }`

**Error 404:** `tracking_not_found`
**Error 422:** Invalid transition — order is not `OutForDelivery`

---

## Group: STS Webhooks

Inbound callbacks from the Settlement & Tax System (STS). STS generates official ABB/Tax Invoice and Credit Note documents and notifies OMS with download links.

**How STS is triggered (external to OMS):**

OMS dispatches outbox events to Gateway at key status transitions. Gateway handles payment and tax processing outside OMS. Once Gateway's processing is complete, STS sends the ABB/Tax Invoice (and optionally a Credit Note) back to OMS.

| Flow | OMS outbox event that triggers Gateway | Gateway processing | STS sends to OMS |
|---|---|---|---|
| **Prepaid** | `PickConfirmedSentToGateway` (after `PickConfirmed`) | Gateway handles payment settlement externally | ABB/Tax Invoice or Credit Note |
| **POD** | `DeliveredSentToGateway` (after `Delivered`) | Gateway handles COD/payment collection externally | ABB/Tax Invoice or Credit Note |

OMS routes the received STS documents to downstream systems based on `orders.is_prepaid`:

| Flow | Trigger point | Invoice forwarded to | Credit Note forwarded to |
|---|---|---|---|
| **Pre-paid** (`is_prepaid = true`) | After `PickConfirmed`, before TMS dispatch | WMS + Gateway | WMS + Gateway |
| **POD** — Pay On Delivery (`is_prepaid = false`) | After `Delivered` | TMS + Gateway | TMS + Gateway |

**Shared STS webhook headers:**

| Header | Description |
|---|---|
| `X-Source-System` | Always `STS` |
| `X-Idempotency-Key` | UUID — duplicate requests with same key are ignored |
| `X-Webhook-Signature` | HMAC-SHA256 of request body using shared secret |

---

### POST /webhooks/sts/abb-tax-invoice

STS sends the ABB/Tax Invoice document link. Timing and forwarding targets differ by payment type.

**Request:**
```json
{
  "orderId": "ORD-001",
  "invoiceNumber": "ABB-2024-001",
  "invoiceLink": "https://sts.example.com/invoices/ABB-2024-001.pdf",
  "amount": 2380,
  "currency": "THB",
  "issuedAt": "2024-01-15T16:00:00Z"
}
```

**Response 202:**
```json
{
  "accepted": true,
  "orderId": "ORD-001",
  "invoiceNumber": "ABB-2024-001",
  "invoiceId": "inv-001",
  "forwardedTo": ["WMS", "Gateway"]
}
```

**Response 409:** `{ "error": "conflict", "detail": "ABB/Tax Invoice ABB-2024-001 already received for ORD-001." }`

---

### POST /webhooks/sts/credit-note

STS sends a Credit Note document link as a separate webhook when a credit note exists for the order. Pre-paid: forwards to WMS and Gateway. POD: forwards to TMS and Gateway.

**Request:**
```json
{
  "orderId": "ORD-001",
  "creditNoteNumber": "CN-2024-001",
  "creditNoteLink": "https://sts.example.com/credit-notes/CN-2024-001.pdf",
  "amount": 200,
  "currency": "THB",
  "issuedAt": "2024-01-15T16:05:00Z"
}
```

**Response 202:**
```json
{
  "accepted": true,
  "orderId": "ORD-001",
  "creditNoteNumber": "CN-2024-001",
  "forwardedTo": ["WMS"]
}
```

**Response 409:** `{ "error": "conflict", "detail": "Credit Note CN-2024-001 already received for ORD-001." }`

---

### POST /webhooks/sts/abb-tax-invoice-received

STS sends the official ABB/Tax Invoice to OMS. Timing and forwarding targets differ by payment method: Prepaid invoices arrive after `PickConfirmed`; POD invoices arrive after `Delivered`.

**Headers:** `X-Idempotency-Key: <uuid>`

**Request:**
```json
{
  "orderId": "ORD-001",
  "invoiceNumber": "INV-STS-001",
  "invoiceAmount": 238000,
  "currency": "THB",
  "invoiceLink": "https://sts.example.com/invoices/INV-STS-001.pdf",
  "issuedAt": "2024-01-15T16:00:00Z"
}
```

Note: `invoiceLink` is required for POD (the link is forwarded to TMS and Gateway). For Prepaid, only `invoiceAmount` and `invoiceNumber` are forwarded to WMS.

**Response 202 Accepted**

**Routing by payment method:**
- `paymentMethod = 'Prepaid'`: dispatches `ABBInvoiceSentToWMS` → WMS and `ABBInvoiceSentToGateway` → Gateway
- `paymentMethod = 'POD'`: dispatches `ABBTaxInvoiceSentToTMS` → TMS and `ABBTaxInvoiceSentToGateway` → Gateway

---

### POST /webhooks/sts/credit-note-received

STS issues a credit note to OMS. Optional — only dispatched when a credit note exists for the order (e.g. price adjustment after substitution or partial pick). Forwarding target depends on payment method.

**Headers:** `X-Idempotency-Key: <uuid>`

**Request:**
```json
{
  "orderId": "ORD-001",
  "creditNoteNumber": "CN-001",
  "amount": 4400,
  "currency": "THB",
  "creditNoteLink": "https://sts.example.com/credit-notes/CN-001.pdf",
  "issuedAt": "2024-01-15T16:05:00Z"
}
```

**Note:** The request field is `amount` (not `creditAmount` — that was a previous naming that caused a 0-amount bug and has been corrected).

**Response 202 Accepted**

**Routing by payment method:**
- `paymentMethod = 'Prepaid'`: dispatches `CreditNoteSentToWMS` → WMS and `CreditNoteSentToGateway` → Gateway
- `paymentMethod = 'POD'`: dispatches `CreditNoteSentToTMS` → TMS and `CreditNoteSentToGateway` → Gateway

---

## Group: Branches

### GET /branches/nearby

Returns branches near a given location. Used as the first step in the POD customer journey — customer selects a branch before requesting a delivery time slot.

**Query parameters:** `lat` (required), `lng` (required), `radius` (km, default 10), `limit` (default 20)

**Response 200:**
```json
{
  "branches": [
    {
      "branchId": "store-central-dc",
      "name": "Central DC",
      "address": "123 Main St, Bangkok",
      "lat": 13.7563,
      "lng": 100.5018,
      "distanceKm": 2.3,
      "availableSlots": true
    }
  ]
}
```

**Error 400:** `missing_coordinates` — lat or lng not provided

---

## Group: Configuration Management

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

### GET /config/outbox-routing-rules

Return all outbox routing rules.

**Auth:** Bearer JWT

**Response 200:**
```json
{ "data": [ { "rule_id": 1, "channel_type": "Marketplace", "business_unit": "TikTok", "trigger_event": "PickConfirmedEvent", "target_system": "Marketplace", "endpoint_key": "tiktok.pick-confirm", "execution_order": 2, "is_active": true } ] }
```

---

### GET /config/outbox-routing-rules/{ruleId}

Return a single outbox routing rule by ID.

**Auth:** Bearer JWT

**Response 200:** `{ "data": OutboxRoutingRule }`

**Response 404:** `{ "error_code": "not_found", "message": "Rule <ruleId> not found.", "trace_id": "..." }`

---

### POST /config/outbox-routing-rules

Create a new outbox routing rule.

**Auth:** Bearer JWT

**Request:**

| Field | Type | Required | Description |
|---|---|---|---|
| `channel_type` | string | Yes | Channel type or `"*"` for all channels |
| `business_unit` | string | Yes | Business unit or `"*"` for all BUs |
| `trigger_event` | string | Yes | Domain event name |
| `target_system` | string | Yes | Downstream system identifier |
| `endpoint_key` | string | Yes | ACL adapter endpoint key |
| `execution_order` | int | Yes | Dispatch order when multiple rules match |
| `is_active` | bool | Yes | Set `true` to activate immediately |

**Response 201:** `{ "data": OutboxRoutingRule }`

---

### PUT /config/outbox-routing-rules/{ruleId}

Replace an existing outbox routing rule. All fields are replaced.

**Auth:** Bearer JWT

**Request:** Same fields as `POST /config/outbox-routing-rules`.

**Response 200:** `{ "data": OutboxRoutingRule }`

**Response 404:** `{ "error_code": "not_found", "message": "Rule <ruleId> not found.", "trace_id": "..." }`

---

### DELETE /config/outbox-routing-rules/{ruleId}

Soft-delete a routing rule: sets `is_active = false`. The worker will stop dispatching events for this rule. The record is retained for audit.

**Auth:** Bearer JWT

**Response 200:** `{ "data": { "message": "Rule deactivated" } }`

**Response 404:** `{ "error_code": "not_found", "message": "Rule <ruleId> not found.", "trace_id": "..." }`

---

## Error Envelope

All error responses share this shape:

```json
{ "error": "<error_code>", "detail": "<human-readable message>" }
```

| Code | HTTP Status | Meaning |
|---|---|---|
| `unauthorized` | 401 | Token missing or expired |
| `not_found` | 404 | Resource does not exist |
| `conflict` | 409 | Duplicate `sourceOrderId` |
| `invalid_transition` | 409 | State machine guard rejected the transition |
| `unprocessable` | 422 | Business rule violation |
| `invalid_parameter` | 400 | Bad query parameter |
