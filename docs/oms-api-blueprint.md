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

**Query parameters:** `status`, `store`, `type` (fulfillment type), `page` (default 1), `limit` (max 200, default 50)

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
  "customer": {
    "name": "Alice Johnson",
    "phone": "0812345678",
    "email": "alice@example.com"
  },
  "deliveryAddress": {
    "address1": "123 Main St",
    "subdistrict": "Silom",
    "district": "Bang Rak",
    "province": "Bangkok",
    "postalCode": "10500"
  },
  "deliverySlot": {
    "scheduledStart": "2024-01-15T14:00:00Z",
    "scheduledEnd": "2024-01-15T16:00:00Z"
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
  "posRecalcPending": false,
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

**Response 409:**
```json
{ "error": "invalid_transition", "detail": "Order ORD-005 is in status Delivered. Cancellation is not allowed from this state." }
```

---

### POST /orders/{id}/recalculate

Manually trigger a POS recalculation. Sets `posRecalcPending = true`. (UC15)

**Response 202:**
```json
{ "orderId": "ORD-009", "posRecalcPending": true, "recalcTriggeredAt": "2024-01-15T15:30:00Z" }
```

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

Reschedule delivery window. Not allowed once order is `OutForDelivery` or later. (UC19)

**Request:** `{ "scheduledStart": "2024-01-15T20:00:00Z", "scheduledEnd": "2024-01-15T22:00:00Z" }`

**Response 200:** Updated slot object.

**Response 409:**
```json
{ "error": "invalid_transition", "detail": "Order ORD-001 is already OutForDelivery. Slot cannot be changed." }
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

WMS offers alternative SKU for unfulfillable line. Sets `substitution_flag = true`, `pos_recalc_pending = true`. (UC5)

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

**Response 202:** `{ "accepted": true, "substitutionId": "sub-001", "orderId": "ORD-003", "customerNotified": true, "posRecalcPending": true }`

---

### POST /webhooks/wms/put-away-confirmed

WMS confirms returned items are shelved. Triggers atomic refund calculation and credit note. (UC14)

**Request:**
```json
{
  "returnId": "RET-001",
  "items": [ { "sku": "APPLE-1KG", "condition": "Resellable", "sloc": "B-05" } ],
  "putAwayAt": "2024-01-15T11:00:00Z"
}
```

**Response 202:** `{ "accepted": true, "returnId": "RET-001", "newReturnStatus": "PutAway", "refundInitiated": true, "creditNoteId": "CN-RET-001" }`

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

**Request:** `{ "orderId": "ORD-006", "trackingId": "TRK-2024-006", "receivedAt": "..." }`

**Response 202:** `{ "accepted": true, "orderId": "ORD-006", "damagedReceiptId": "DMG-001", "newOrderStatus": "OnHold", "holdReason": "PackageDamaged" }`

---

### POST /webhooks/wms/damaged-goods-put-away

WMS confirms damaged items inspected, condition assigned, shelved/disposed. (UC23)

**Request:**
```json
{
  "damagedReceiptId": "DMG-001",
  "items": [ { "sku": "APPLE-1KG", "condition": "Repairable", "sloc": "DMG-01", "qty": 12 } ],
  "putAwayAt": "2024-01-15T12:00:00Z"
}
```

**Response 202:** `{ "accepted": true, "damagedReceiptId": "DMG-001", "newStatus": "PutAway" }`

---

### POST /webhooks/tms/package-dispatched

TMS driver collected the package. → `OutForDelivery`. (UC7)

**Request:** `{ "trackingId": "TRK-2024-001", "dispatchedAt": "2024-01-15T17:47:00Z" }`

**Response 202:** `{ "accepted": true, "orderId": "ORD-001", "newOrderStatus": "OutForDelivery", "newPackageStatus": "OutForDelivery" }`

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

### POST /webhooks/pos/pos-collection-ready

POS confirms Click & Collect order packed and ready at store. → `ReadyForCollection`. (UC10)

**Request:** `{ "orderId": "ORD-002", "storeId": "store-a", "notifiedAt": "..." }`

**Response 202:** `{ "accepted": true, "orderId": "ORD-002", "newStatus": "ReadyForCollection" }`

---

### POST /webhooks/pos/collected

POS confirms customer collected order. Triggers invoice generation. → `Collected`. (UC11)

**Request:** `{ "orderId": "ORD-002", "collectedAt": "2024-01-15T15:00:00Z", "collectedBy": "Alice Johnson" }`

**Response 202:** `{ "accepted": true, "orderId": "ORD-002", "newStatus": "Collected", "invoiceTriggered": true }`

---

### POST /webhooks/pos/invoiced

POS confirms fiscal invoice issued. → `Invoiced`. (UC12)

**Request:**
```json
{ "orderId": "ORD-001", "invoiceNumber": "INV-2024-001", "totalAmount": 2380, "currency": "THB", "invoicedAt": "2024-01-15T19:25:00Z" }
```

**Response 202:** `{ "accepted": true, "orderId": "ORD-001", "newStatus": "Invoiced", "invoiceId": "inv-001" }`

---

### POST /webhooks/pos/payment-confirmed

POS confirms payment received. → `Paid`. (UC13)

**Request:**
```json
{ "orderId": "ORD-001", "invoiceNumber": "INV-2024-001", "paymentMethod": "CreditCard", "paidAmount": 2380, "currency": "THB", "paidAt": "2024-01-15T19:31:00Z" }
```

**Response 202:** `{ "accepted": true, "orderId": "ORD-001", "newStatus": "Paid" }`

---

### POST /webhooks/pos/recalculation-result

POS returns adjusted total after promotions applied to picked quantities. Clears `posRecalcPending`. (UC15)

**Request:**
```json
{
  "orderId": "ORD-001",
  "originalAmount": 2450,
  "adjustedAmount": 2380,
  "currency": "THB",
  "promotionsApplied": [
    { "promoCode": "FRESH10", "discountAmount": 70, "description": "10% fresh produce discount" }
  ],
  "recalculatedAt": "2024-01-15T15:36:00Z"
}
```

**Response 202:** `{ "accepted": true, "orderId": "ORD-001", "finalAmount": 2380, "posRecalcPendingCleared": true }`

---

### POST /webhooks/pos/pos-recalc-completed

POS confirms full recalculation cycle closed. All amounts finalised. Unblocks packing workflow. (UC15)

**Request:**
```json
{ "orderId": "ORD-001", "finalAmount": 2380, "currency": "THB", "completedAt": "2024-01-15T15:37:00Z" }
```

**Response 202:** `{ "accepted": true, "orderId": "ORD-001" }`

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
