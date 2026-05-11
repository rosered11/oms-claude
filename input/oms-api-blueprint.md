FORMAT: 1A
HOST: https://api.sprintconnect.io/v1

# Sprint Connect OMS API

REST API for the Sprint Connect Order Management System.
All timestamps are ISO 8601 UTC. All monetary values are integers in the smallest currency unit (satang / stang).
Authentication uses Bearer JWT on every endpoint unless marked public.

---

# Group Authentication

## Token [/auth/token]

### Request Access Token [POST]

Exchange service credentials for a short-lived JWT.

+ Request (application/json)

    + Headers

            X-Service-Name: oms-dashboard

    + Body

            {
              "client_id": "dashboard-client",
              "client_secret": "••••••••"
            }

+ Response 200 (application/json)

        {
          "access_token": "eyJhbGciOiJSUzI1NiJ9...",
          "token_type": "Bearer",
          "expires_in": 3600
        }

+ Response 401 (application/json)

        {
          "error": "invalid_client",
          "error_description": "Client credentials are invalid."
        }

---

# Group Orders

Outbound customer orders — goods leaving the warehouse.

## Order Collection [/orders{?status,store,type,page,limit}]

### List Orders [GET]

Returns a paginated list of outbound orders. Use `status` (multi-value, comma-separated) to filter the Kanban Board columns.

+ Parameters

    + status (optional, string, `Pending,PickStarted`) ... Comma-separated order statuses. Allowed values: `Pending`, `BookingConfirmed`, `PickStarted`, `PickConfirmed`, `ReadyForCollection`, `Packed`, `OutForDelivery`, `Delivering`, `Delivered`, `Collected`, `Invoiced`, `Paid`, `OnHold`, `Cancelled`, `Returned`.
    + store (optional, string, `Central DC`) ... Filter by store or DC name.
    + type (optional, string, `Delivery`) ... Fulfillment type. Allowed: `Delivery`, `Express`, `ClickAndCollect`.
    + page (optional, number, `1`) ... 1-based page number. Default `1`.
    + limit (optional, number, `50`) ... Items per page. Max `200`. Default `50`.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "items": [
            {
              "id": "ORD-001",
              "orderNumber": "ORD-001",
              "customer": "Alice Johnson",
              "items": 5,
              "type": "Delivery",
              "status": "Paid",
              "store": "Central DC",
              "amount": 2380,
              "holdReason": null,
              "orderDate": "2024-01-15",
              "channelType": "Web",
              "businessUnit": "SC01",
              "paymentMethod": "CreditCard",
              "substitutionFlag": false,
              "posRecalcPending": false,
              "deliverySlot": {
                "scheduledStart": "2024-01-15T18:00:00Z",
                "scheduledEnd": "2024-01-15T20:00:00Z"
              },
              "createdAt": "2024-01-15T14:02:00Z",
              "updatedAt": "2024-01-15T19:31:00Z"
            },
            {
              "id": "ORD-006",
              "orderNumber": "ORD-006",
              "customer": "Frank Lee",
              "items": 12,
              "type": "Delivery",
              "status": "OnHold",
              "store": "Central DC",
              "amount": 5600,
              "holdReason": "PackageDamaged",
              "orderDate": "2024-01-15",
              "channelType": "App",
              "businessUnit": "SC01",
              "paymentMethod": "PromptPay",
              "substitutionFlag": false,
              "posRecalcPending": false,
              "deliverySlot": {
                "scheduledStart": "2024-01-15T13:00:00Z",
                "scheduledEnd": "2024-01-15T15:00:00Z"
              },
              "createdAt": "2024-01-15T09:10:00Z",
              "updatedAt": "2024-01-15T10:45:00Z"
            }
          ],
          "total": 14,
          "page": 1,
          "limit": 50
        }

+ Response 400 (application/json)

        {
          "error": "invalid_parameter",
          "detail": "Unknown status value 'PENDING'. Use PascalCase enum values."
        }

+ Response 401 (application/json)

        {
          "error": "unauthorized",
          "detail": "Bearer token missing or expired."
        }

### Create Order [POST]

Creates a new outbound customer order. OMS auto-routes fulfillment based on `fulfillmentType` and `businessUnit` rollout configuration. Returns `201` with the new order ID and `Pending` status. (UC1)

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

    + Body

            {
              "sourceOrderId": "EXT-12345",
              "channelType": "Web",
              "businessUnit": "SC01",
              "storeId": "store-central-dc",
              "fulfillmentType": "Delivery",
              "paymentMethod": "CreditCard",
              "customer": {
                "name": "Alice Johnson",
                "phone": "+66812345678",
                "email": "alice@example.com"
              },
              "deliveryAddress": {
                "address1": "99/1 Sukhumvit Rd",
                "subdistrict": "Khlong Toei",
                "district": "Khlong Toei",
                "province": "Bangkok",
                "postalCode": "10110"
              },
              "deliverySlot": {
                "scheduledStart": "2024-01-15T18:00:00Z",
                "scheduledEnd": "2024-01-15T20:00:00Z"
              },
              "lines": [
                {
                  "sku": "APPLE-1KG",
                  "productName": "Apple (1 kg bag)",
                  "barcode": "8851234567890",
                  "requestedQty": 5,
                  "unitPrice": 120,
                  "unitOfMeasure": "Each"
                }
              ]
            }

+ Response 201 (application/json)

        {
          "id": "ORD-015",
          "orderNumber": "ORD-015",
          "status": "Pending",
          "createdAt": "2024-01-15T14:02:00Z"
        }

+ Response 422 (application/json)

        {
          "error": "unprocessable",
          "detail": "sourceOrderId EXT-12345 already exists."
        }

---

## Order [/orders/{id}]

### Get Order [GET]

Returns full detail for a single order, including all lines and packages. (UC16)

+ Parameters

    + id (required, string, `ORD-001`) ... Order ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "id": "ORD-001",
          "orderNumber": "ORD-001",
          "customer": {
            "name": "Alice Johnson",
            "phone": "+66812345678",
            "email": "alice@example.com"
          },
          "type": "Delivery",
          "channelType": "Web",
          "businessUnit": "SC01",
          "status": "Paid",
          "store": "Central DC",
          "amount": 2380,
          "originalAmount": 2450,
          "orderDate": "2024-01-15",
          "paymentMethod": "CreditCard",
          "substitutionFlag": false,
          "posRecalcPending": false,
          "preHoldStatus": null,
          "holdReason": null,
          "deliveryAddress": {
            "address1": "99/1 Sukhumvit Rd",
            "subdistrict": "Khlong Toei",
            "district": "Khlong Toei",
            "province": "Bangkok",
            "postalCode": "10110"
          },
          "deliverySlot": {
            "scheduledStart": "2024-01-15T18:00:00Z",
            "scheduledEnd": "2024-01-15T20:00:00Z"
          },
          "lines": [
            {
              "orderLineId": "line-001",
              "sku": "APPLE-1KG",
              "productName": "Apple (1 kg bag)",
              "barcode": "8851234567890",
              "status": "Delivered",
              "isSubstitute": false,
              "requestedQty": 5,
              "pickedQty": 5,
              "unitPrice": 120,
              "totalPrice": 600,
              "unitOfMeasure": "Each"
            }
          ],
          "packages": [
            {
              "packageId": "pkg-001",
              "trackingId": "TRK-2024-001",
              "vehicleType": "Van",
              "weight": 2.5,
              "status": "Delivered",
              "lineIds": ["line-001"]
            }
          ],
          "createdAt": "2024-01-15T14:02:00Z",
          "updatedAt": "2024-01-15T19:31:00Z"
        }

+ Response 404 (application/json)

        {
          "error": "not_found",
          "detail": "Order ORD-999 does not exist."
        }

---

## Order Lines [/orders/{id}/lines]

### List Order Lines [GET]

Returns all order lines with requested and picked quantities. Useful for pick-quantity reconciliation. (UC1, UC16)

+ Parameters

    + id (required, string, `ORD-001`) ... Order ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "orderId": "ORD-001",
          "lines": [
            {
              "orderLineId": "line-001",
              "sku": "APPLE-1KG",
              "productName": "Apple (1 kg bag)",
              "barcode": "8851234567890",
              "status": "Delivered",
              "isSubstitute": false,
              "requestedQty": 5,
              "pickedQty": 5,
              "unitPrice": 120,
              "totalPrice": 600,
              "unitOfMeasure": "Each"
            }
          ]
        }

+ Response 404 (application/json)

        {
          "error": "not_found",
          "detail": "Order ORD-999 does not exist."
        }

---

## Order Packages [/orders/{id}/packages]

### List Packages [GET]

Returns all packages for an order with tracking IDs, vehicle type, weight, and constituent line IDs. (UC18)

+ Parameters

    + id (required, string, `ORD-001`) ... Order ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "orderId": "ORD-001",
          "packages": [
            {
              "packageId": "pkg-001",
              "trackingId": "TRK-2024-001",
              "vehicleType": "Van",
              "weight": 2.5,
              "status": "Delivered",
              "lineIds": ["line-001"]
            }
          ]
        }

---

## Order Webhooks [/orders/{id}/webhooks]

### List Webhook Events [GET]

Returns all inbound webhook log entries recorded for this order from WMS, TMS, and POS. (UC1)

+ Parameters

    + id (required, string, `ORD-001`) ... Order ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "orderId": "ORD-001",
          "webhooks": [
            {
              "webhookLogId": "whl-001",
              "sourceSystem": "WMS",
              "eventType": "BookingConfirmed",
              "detail": "WMS booking ref WMS-BK-001 confirmed.",
              "receivedAt": "2024-01-15T14:05:00Z"
            },
            {
              "webhookLogId": "whl-003",
              "sourceSystem": "WMS",
              "eventType": "PickConfirmed",
              "detail": "5 lines picked. No substitutions.",
              "receivedAt": "2024-01-15T15:31:00Z"
            },
            {
              "webhookLogId": "whl-007",
              "sourceSystem": "TMS",
              "eventType": "PackageDelivered",
              "detail": "Delivered to Alice Johnson. POD: https://tms.example.com/pod/TRK-2024-001.jpg",
              "receivedAt": "2024-01-15T19:22:00Z"
            }
          ]
        }

---

## Order Substitutions [/orders/{id}/substitutions]

### List Substitutions [GET]

Returns all substitution proposals for the order. Each entry includes the original line, proposed substitute, and customer approval status. (UC5)

+ Parameters

    + id (required, string, `ORD-003`) ... Order ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

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

---

## Approve Substitution [/orders/{id}/substitutions/{subId}/approve]

### Approve Substitution [POST]

Customer approves a proposed product substitution. OMS sets `customerApproved = true` and triggers POS recalculation if needed. (UC5)

+ Parameters

    + id (required, string, `ORD-003`) ... Order ID.
    + subId (required, string, `sub-001`) ... Substitution ID.

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "substitutionId": "sub-001",
          "orderId": "ORD-003",
          "customerApproved": true,
          "approvedAt": "2024-01-15T15:45:00Z"
        }

+ Response 409 (application/json)

        {
          "error": "conflict",
          "detail": "Substitution sub-001 has already been actioned."
        }

---

## Reject Substitution [/orders/{id}/substitutions/{subId}/reject]

### Reject Substitution [POST]

Customer rejects a proposed substitution. The original line is voided and POS recalculates without it. (UC5)

+ Parameters

    + id (required, string, `ORD-003`) ... Order ID.
    + subId (required, string, `sub-001`) ... Substitution ID.

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "substitutionId": "sub-001",
          "orderId": "ORD-003",
          "customerApproved": false,
          "lineVoided": true,
          "posRecalcTriggered": true
        }

---

## Order Timeline [/orders/{id}/timeline]

### Get Order Timeline [GET]

Returns the full event log for a single order in chronological order.
Each event has a `type`: `domain` (OMS state change), `webhook` (received from WMS/TMS/POS), `outbox` (dispatched to external system), or `bridge` (WMS stock marker). (UC16)

+ Parameters

    + id (required, string, `ORD-001`) ... Order ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "order": {
            "id": "ORD-001",
            "customer": "Alice Johnson",
            "store": "Central DC",
            "items": 5,
            "status": "Paid",
            "type": "Delivery",
            "amount": 2380,
            "linkedPoId": "PO-001"
          },
          "events": [
            {
              "id": 1,
              "time": "08:00",
              "occurredAt": "2024-01-15T08:00:00Z",
              "phase": "inbound",
              "type": "domain",
              "system": "OMS",
              "event": "PurchaseOrderCreated",
              "detail": "PO-001 — Fresh Foods Ltd · 15 lines · ฿45,000. Status → Created",
              "outStatus": null
            },
            {
              "id": 6,
              "time": "10:15",
              "occurredAt": "2024-01-15T10:15:00Z",
              "phase": "bridge",
              "type": "bridge",
              "system": "WMS",
              "event": "Stock Available in WMS",
              "detail": "WMS incremented available inventory for all received SKUs. Picker can now fulfill orders.",
              "outStatus": null
            },
            {
              "id": 7,
              "time": "14:02",
              "occurredAt": "2024-01-15T14:02:00Z",
              "phase": "outbound",
              "type": "domain",
              "system": "OMS",
              "event": "OrderCreated",
              "detail": "ORD-001 — Alice Johnson · Delivery · 5 items · ฿2,450. Status → Pending",
              "outStatus": null
            },
            {
              "id": 27,
              "time": "19:31",
              "occurredAt": "2024-01-15T19:31:00Z",
              "phase": "outbound",
              "type": "domain",
              "system": "OMS",
              "event": "OrderPaid",
              "detail": "Order fully closed. Total time from order → payment: 5h 29m",
              "outStatus": null
            }
          ],
          "summary": {
            "totalEvents": 27,
            "inboundPhaseEvents": 5,
            "bridgeEvents": 1,
            "outboundPhaseEvents": 21,
            "inboundToStockAvailableMinutes": 75,
            "orderToDeliveredMinutes": 329,
            "totalEndToEndMinutes": 691
          }
        }

+ Response 404 (application/json)

        {
          "error": "not_found",
          "detail": "Order ORD-999 does not exist."
        }

---

## Hold Order [/orders/{id}/hold]

### Place Order on Hold [PATCH]

Places an active order on hold. The current status is preserved as `preHoldStatus` and the order transitions to `OnHold`. Allowed from any non-terminal, non-closed status. (UC6)

+ Parameters

    + id (required, string, `ORD-004`) ... Order ID.

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

    + Body

            {
              "holdReason": "PackageDamaged",
              "heldBy": "ops-agent-01"
            }

+ Response 200 (application/json)

        {
          "id": "ORD-004",
          "newStatus": "OnHold",
          "preHoldStatus": "OutForDelivery",
          "holdReason": "PackageDamaged"
        }

+ Response 409 (application/json)

        {
          "error": "invalid_transition",
          "detail": "Order ORD-004 is already OnHold."
        }

---

## Release Hold [/orders/{id}/release-hold]

### Release Order from Hold [PATCH]

Restores the order to its `preHoldStatus`. Clears `holdReason` and `preHoldStatus`. (UC6)

+ Parameters

    + id (required, string, `ORD-004`) ... Order ID.

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

    + Body

            {
              "releasedBy": "ops-agent-01"
            }

+ Response 200 (application/json)

        {
          "id": "ORD-004",
          "newStatus": "OutForDelivery",
          "preHoldStatus": null,
          "holdReason": null
        }

+ Response 409 (application/json)

        {
          "error": "invalid_transition",
          "detail": "Order ORD-004 is not currently OnHold."
        }

---

## Cancel Order [/orders/{id}/cancel]

### Cancel Order [PATCH]

Cancels an order. Allowed from `Pending`, `BookingConfirmed`, or `OnHold` statuses only. Triggers `OrderCancelledEvent` via outbox; WMS reverses any stock reservation. (UC9)

+ Parameters

    + id (required, string, `ORD-005`) ... Order ID.

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

    + Body

            {
              "reason": "CustomerRequest",
              "cancelledBy": "ops-agent-01"
            }

+ Response 200 (application/json)

        {
          "id": "ORD-005",
          "newStatus": "Cancelled"
        }

+ Response 409 (application/json)

        {
          "error": "invalid_transition",
          "detail": "Order ORD-005 is in status Delivered. Cancellation is not allowed from this state."
        }

---

## Recalculate Order [/orders/{id}/recalculate]

### Trigger POS Recalculation [POST]

Manually triggers a POS recalculation for the order. Recalculation is normally triggered automatically when WMS reports a substitution or quantity shortfall. Sets `posRecalcPending = true` and publishes `RecalcRequestedEvent` via outbox. (UC15)

+ Parameters

    + id (required, string, `ORD-009`) ... Order ID.

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 202 (application/json)

        {
          "orderId": "ORD-009",
          "posRecalcPending": true,
          "recalcTriggeredAt": "2024-01-15T15:30:00Z"
        }

---

## Delivery Slot [/orders/{id}/delivery-slot]

### Get Delivery Slot [GET]

Returns the current delivery slot for an order. (UC19)

+ Parameters

    + id (required, string, `ORD-001`) ... Order ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "orderId": "ORD-001",
          "slotId": "slot-001",
          "scheduledStart": "2024-01-15T18:00:00Z",
          "scheduledEnd": "2024-01-15T20:00:00Z",
          "storeId": "store-central-dc"
        }

+ Response 404 (application/json)

        {
          "error": "not_found",
          "detail": "Order ORD-001 has no delivery slot assigned."
        }

### Update Delivery Slot [PATCH]

Reschedules the delivery window. Allowed only when order has not yet reached `OutForDelivery`. (UC19)

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

    + Body

            {
              "scheduledStart": "2024-01-15T20:00:00Z",
              "scheduledEnd": "2024-01-15T22:00:00Z"
            }

+ Response 200 (application/json)

        {
          "orderId": "ORD-001",
          "slotId": "slot-001",
          "scheduledStart": "2024-01-15T20:00:00Z",
          "scheduledEnd": "2024-01-15T22:00:00Z"
        }

+ Response 409 (application/json)

        {
          "error": "invalid_transition",
          "detail": "Order ORD-001 is already OutForDelivery. Slot cannot be changed."
        }

---

# Group Returns

Customer return requests — goods flowing back from customer to warehouse.

## Returns Collection [/returns{?orderId,status,page,limit}]

### Create Return [POST]

Initiates a return for a delivered or paid order. OMS creates a `Return` record with status `ReturnRequested` and dispatches a pickup request via outbox. (UC14)

+ Parameters

    + orderId (optional, string, `ORD-001`) ... Filter by order ID.
    + status (optional, string, `ReturnRequested`) ... Filter by return status.
    + page (optional, number, `1`) ... Default `1`.
    + limit (optional, number, `50`) ... Max `200`. Default `50`.

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

    + Body

            {
              "orderId": "ORD-001",
              "returnReason": "WrongItem",
              "items": [
                {
                  "orderLineId": "line-001",
                  "sku": "APPLE-1KG",
                  "quantity": 2,
                  "itemReason": "WrongItem"
                }
              ],
              "requestedBy": "alice@example.com"
            }

+ Response 201 (application/json)

        {
          "id": "RET-001",
          "returnOrderNumber": "RET-001",
          "orderId": "ORD-001",
          "status": "ReturnRequested",
          "createdAt": "2024-01-15T20:00:00Z"
        }

+ Response 422 (application/json)

        {
          "error": "unprocessable",
          "detail": "Order ORD-005 is in status Cancelled. Returns are only allowed from Delivered, Invoiced, or Paid."
        }

### List Returns [GET]

Returns a paginated list of return records.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "items": [
            {
              "id": "RET-001",
              "returnOrderNumber": "RET-001",
              "orderId": "ORD-001",
              "status": "PutAway",
              "returnReason": "WrongItem",
              "requestedAt": "2024-01-15T20:00:00Z",
              "refundedAt": "2024-01-15T21:30:00Z",
              "createdAt": "2024-01-15T20:00:00Z",
              "updatedAt": "2024-01-15T21:30:00Z"
            }
          ],
          "total": 1,
          "page": 1,
          "limit": 50
        }

---

## Return [/returns/{id}]

### Get Return [GET]

Returns full detail for a single return record. (UC14)

+ Parameters

    + id (required, string, `RET-001`) ... Return ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

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

+ Response 404 (application/json)

        {
          "error": "not_found",
          "detail": "Return RET-999 does not exist."
        }

---

## Return Items [/returns/{id}/items]

### List Return Items [GET]

Returns all items in a return, including condition assigned at inspection and put-away storage location. (UC14)

+ Parameters

    + id (required, string, `RET-001`) ... Return ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

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

---

## Cancel Return [/returns/{id}/cancel]

### Cancel Return [PATCH]

Cancels a return that has not yet been received at the warehouse. Allowed from `ReturnRequested` or `PickupScheduled` statuses. (UC14)

+ Parameters

    + id (required, string, `RET-001`) ... Return ID.

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

    + Body

            {
              "reason": "CustomerChangedMind",
              "cancelledBy": "ops-agent-01"
            }

+ Response 200 (application/json)

        {
          "id": "RET-001",
          "newStatus": "Cancelled"
        }

+ Response 409 (application/json)

        {
          "error": "invalid_transition",
          "detail": "Return RET-001 is in status PutAway. It cannot be cancelled after goods receipt."
        }

---

## Return Refund [/returns/{id}/refund]

### Get Refund [GET]

Returns the refund record and associated credit note for a completed return. (UC14)

+ Parameters

    + id (required, string, `RET-001`) ... Return ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

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

+ Response 404 (application/json)

        {
          "error": "not_found",
          "detail": "Return RET-001 has no refund record. Refund is created after put-away completes."
        }

---

# Group Inbound

Goods arriving at the warehouse: Purchase Orders (from suppliers) and Transfer Orders (between stores / DCs).

## Purchase Order Collection [/inbound/purchase-orders{?status,store,page,limit}]

### List Purchase Orders [GET]

Returns all Purchase Orders. Used by the Kanban Board Inbound tab (PO swimlane). (UC21)

+ Parameters

    + status (optional, string, `Created,PartiallyReceived`) ... Comma-separated PO statuses. Allowed: `Created`, `PartiallyReceived`, `FullyReceived`, `Closed`.
    + store (optional, string, `Central DC`) ... Receiving store or DC.
    + page (optional, number, `1`) ... Default `1`.
    + limit (optional, number, `50`) ... Max `200`. Default `50`.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "items": [
            {
              "id": "PO-001",
              "poNumber": "PO-001",
              "supplier": "Fresh Foods Ltd",
              "supplierId": "sup-fresh-foods",
              "lines": 15,
              "status": "Closed",
              "store": "Central DC",
              "value": 45000,
              "goodsReceiveNo": "GRN-2024-001",
              "createdAt": "2024-01-15T08:00:00Z",
              "updatedAt": "2024-01-15T10:15:00Z"
            },
            {
              "id": "PO-002",
              "poNumber": "PO-002",
              "supplier": "Beverages Corp",
              "supplierId": "sup-beverages",
              "lines": 8,
              "status": "Created",
              "store": "Store A",
              "value": 12000,
              "goodsReceiveNo": null,
              "createdAt": "2024-01-15T11:00:00Z",
              "updatedAt": "2024-01-15T11:00:00Z"
            }
          ],
          "total": 4,
          "page": 1,
          "limit": 50
        }

+ Response 401 (application/json)

        {
          "error": "unauthorized",
          "detail": "Bearer token missing or expired."
        }

### Create Purchase Order [POST]

Creates a new Purchase Order in OMS to track expected inbound goods from a supplier. Triggers `PurchaseOrderCreatedEvent` via outbox to notify WMS of expected receipt. (UC21)

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

    + Body

            {
              "poNumber": "PO-005",
              "supplierId": "sup-fresh-foods",
              "storeId": "store-central-dc",
              "lines": [
                {
                  "sku": "APPLE-1KG",
                  "orderedQty": 20,
                  "unitCost": 45,
                  "currency": "THB"
                }
              ]
            }

+ Response 201 (application/json)

        {
          "id": "PO-005",
          "poNumber": "PO-005",
          "status": "Created",
          "createdAt": "2024-01-15T08:00:00Z"
        }

+ Response 422 (application/json)

        {
          "error": "unprocessable",
          "detail": "poNumber PO-005 already exists."
        }

---

## Purchase Order [/inbound/purchase-orders/{id}]

### Get Purchase Order [GET]

Returns full detail for a single PO including all order lines with received quantities and conditions. (UC21)

+ Parameters

    + id (required, string, `PO-001`) ... Purchase Order ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "id": "PO-001",
          "poNumber": "PO-001",
          "supplier": "Fresh Foods Ltd",
          "supplierId": "sup-fresh-foods",
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

+ Response 404 (application/json)

        {
          "error": "not_found",
          "detail": "Purchase Order PO-999 does not exist."
        }

---

## PO Goods Receipts [/inbound/purchase-orders/{id}/goods-receipts]

### List Goods Receipts [GET]

Returns all goods receipt records associated with a Purchase Order. (UC21)

+ Parameters

    + id (required, string, `PO-001`) ... Purchase Order ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "purchaseOrderId": "PO-001",
          "goodsReceipts": [
            {
              "goodsReceiveNo": "GRN-2024-001",
              "status": "PutAway",
              "receivedAt": "2024-01-15T09:28:00Z",
              "putAwayAt": "2024-01-15T10:15:00Z",
              "lines": [
                {
                  "sku": "APPLE-1KG",
                  "receivedQty": 10,
                  "condition": "Resellable",
                  "sloc": "A-12"
                }
              ]
            }
          ]
        }

---

## Transfer Order Collection [/inbound/transfer-orders{?status,sourceStore,destStore,page,limit}]

### List Transfer Orders [GET]

Returns all Transfer Orders. Used by the Kanban Board Inbound tab (Transfer swimlane). (UC22)

+ Parameters

    + status (optional, string, `InTransit,Received`) ... Comma-separated TO statuses. Allowed: `Created`, `PickConfirmed`, `InTransit`, `Received`, `Completed`.
    + sourceStore (optional, string, `Central DC`) ... Source store or DC.
    + destStore (optional, string, `Store A`) ... Destination store.
    + page (optional, number, `1`) ... Default `1`.
    + limit (optional, number, `50`) ... Max `200`. Default `50`.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "items": [
            {
              "id": "TR-001",
              "transferNumber": "TR-001",
              "source": "Central DC",
              "sourceStoreId": "store-central-dc",
              "dest": "Store B",
              "destStoreId": "store-b",
              "lines": 6,
              "status": "InTransit",
              "tracking": "TRK-TR-001",
              "createdAt": "2024-01-15T10:00:00Z",
              "updatedAt": "2024-01-15T11:00:00Z"
            },
            {
              "id": "TR-002",
              "transferNumber": "TR-002",
              "source": "Store A",
              "sourceStoreId": "store-a",
              "dest": "Store C",
              "destStoreId": "store-c",
              "lines": 3,
              "status": "Created",
              "tracking": null,
              "createdAt": "2024-01-15T13:00:00Z",
              "updatedAt": "2024-01-15T13:00:00Z"
            }
          ],
          "total": 4,
          "page": 1,
          "limit": 50
        }

+ Response 401 (application/json)

        {
          "error": "unauthorized",
          "detail": "Bearer token missing or expired."
        }

### Create Transfer Order [POST]

Creates a new Transfer Order between stores or DCs. Triggers `TransferOrderCreatedEvent` via outbox to notify source store WMS to begin picking. (UC22)

+ Request (application/json)

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

    + Body

            {
              "sourceStoreId": "store-central-dc",
              "destStoreId": "store-b",
              "lines": [
                {
                  "sku": "APPLE-1KG",
                  "requestedQty": 6
                }
              ]
            }

+ Response 201 (application/json)

        {
          "id": "TR-005",
          "transferNumber": "TR-005",
          "status": "Created",
          "createdAt": "2024-01-15T10:00:00Z"
        }

---

## Transfer Order [/inbound/transfer-orders/{id}]

### Get Transfer Order [GET]

Returns full detail for a single Transfer Order including line items and quantities. (UC22)

+ Parameters

    + id (required, string, `TR-001`) ... Transfer Order ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "id": "TR-001",
          "transferNumber": "TR-001",
          "source": "Central DC",
          "sourceStoreId": "store-central-dc",
          "dest": "Store B",
          "destStoreId": "store-b",
          "status": "InTransit",
          "tracking": "TRK-TR-001",
          "lines": [
            {
              "toLineId": "tol-001",
              "sku": "APPLE-1KG",
              "productName": "Apple (1 kg bag)",
              "requestedQty": 6,
              "transferredQty": 6,
              "confirmedAt": "2024-01-15T11:00:00Z"
            }
          ],
          "createdAt": "2024-01-15T10:00:00Z",
          "updatedAt": "2024-01-15T11:00:00Z"
        }

+ Response 404 (application/json)

        {
          "error": "not_found",
          "detail": "Transfer Order TR-999 does not exist."
        }

---

## Transfer Order Confirmations [/inbound/transfer-orders/{id}/confirmations]

### List Transfer Confirmations [GET]

Returns confirmation records for a Transfer Order: pick confirmation at source and receipt confirmation at destination. (UC22)

+ Parameters

    + id (required, string, `TR-001`) ... Transfer Order ID.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

        {
          "transferOrderId": "TR-001",
          "confirmations": [
            {
              "type": "PickConfirmed",
              "confirmedAt": "2024-01-15T11:00:00Z",
              "confirmedBy": "WMS",
              "tracking": "TRK-TR-001"
            },
            {
              "type": "TransferReceived",
              "confirmedAt": "2024-01-15T14:30:00Z",
              "confirmedBy": "WMS",
              "tracking": "TRK-TR-001"
            }
          ]
        }

---

# Group Stock

WMS stock movement ledger — read-only view of inbound/outbound events per SKU.
OMS does not own inventory counts. This endpoint reflects events OMS recorded, not WMS stock levels.

## Stock Ledger [/stock/{sku}/ledger{?storeId,from,to}]

### Get Stock Ledger [GET]

Returns per-location stock movement events for a SKU.
Each location has its own `events` list and current `balance`.
Used by the Stock Flow view (Case 1 = single location, Case 2 = multi-location after transfer).

+ Parameters

    + sku (required, string, `APPLE-1KG`) ... Product SKU.
    + storeId (optional, string, `store-central-dc`) ... Filter to a single location. Omit to get all locations.
    + from (optional, string, `2024-01-15`) ... ISO 8601 date. Filters events `occurredAt >= from`.
    + to (optional, string, `2024-01-15`) ... ISO 8601 date. Filters events `occurredAt <= to`.

+ Request

    + Headers

            Authorization: Bearer eyJhbGciOiJSUzI1NiJ9...

+ Response 200 (application/json)

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
                {
                  "id": 1,
                  "time": "10:15",
                  "occurredAt": "2024-01-15T10:15:00Z",
                  "dir": "in",
                  "ref": "PO-001",
                  "refType": "PurchaseOrder",
                  "event": "PurchaseOrderPutAwayConfirmed",
                  "qtyChange": 10,
                  "balance": 10,
                  "detail": "Fresh Foods Ltd — 10 bags shelved at Sloc A-12. WMS stock now available."
                },
                {
                  "id": 2,
                  "time": "11:00",
                  "occurredAt": "2024-01-15T11:00:00Z",
                  "dir": "out",
                  "ref": "TR-001",
                  "refType": "TransferOrder",
                  "event": "TransferPickConfirmed",
                  "qtyChange": -4,
                  "balance": 6,
                  "detail": "4 bags picked & packed for transfer → Store A (TRK-TR-001)"
                },
                {
                  "id": 3,
                  "time": "15:31",
                  "occurredAt": "2024-01-15T15:31:00Z",
                  "dir": "out",
                  "ref": "ORD-A",
                  "refType": "Order",
                  "event": "PickConfirmed",
                  "qtyChange": -6,
                  "balance": 0,
                  "detail": "Alice Johnson picks 6 bags for delivery"
                }
              ]
            },
            {
              "storeId": "store-a",
              "storeName": "Store A",
              "balance": 1,
              "events": [
                {
                  "id": 1,
                  "time": "14:30",
                  "occurredAt": "2024-01-15T14:30:00Z",
                  "dir": "in",
                  "ref": "TR-001",
                  "refType": "TransferOrder",
                  "event": "TransferReceived",
                  "qtyChange": 4,
                  "balance": 4,
                  "detail": "4 bags received from Central DC via TRK-TR-001"
                },
                {
                  "id": 2,
                  "time": "16:00",
                  "occurredAt": "2024-01-15T16:00:00Z",
                  "dir": "out",
                  "ref": "ORD-C",
                  "refType": "Order",
                  "event": "PickConfirmed",
                  "qtyChange": -3,
                  "balance": 1,
                  "detail": "Charlie Wong picks 3 bags for delivery"
                }
              ]
            }
          ]
        }

+ Response 404 (application/json)

        {
          "error": "not_found",
          "detail": "SKU UNKNOWN-SKU has no stock ledger entries."
        }

+ Response 400 (application/json)

        {
          "error": "invalid_parameter",
          "detail": "'from' must be a valid ISO 8601 date (YYYY-MM-DD)."
        }

---

# Group Webhooks

Inbound callbacks from external systems (WMS, TMS, POS).
All webhook endpoints return `202 Accepted` immediately.
Processing is synchronous inside the handler; the response does NOT wait for outbox dispatch.
Each handler stages an `OrderWebhookLog` entry atomically with the domain state change.

**Shared webhook headers:**

| Header | Description |
|---|---|
| `X-Source-System` | Sending system: `WMS`, `TMS`, or `POS` |
| `X-Idempotency-Key` | UUID — duplicate requests with same key are ignored |
| `X-Webhook-Signature` | HMAC-SHA256 of request body using shared secret |

---

## WMS: Booking Confirmed [/webhooks/wms/booking-confirmed]

### Booking Confirmed [POST]

WMS confirms it can fulfil the order and has reserved stock. Transitions order to `BookingConfirmed`. (UC2)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: a0b1c2d3-e4f5-6789-abcd-ef0123456789
            X-Webhook-Signature: sha256=aaa111...

    + Body

            {
              "orderId": "ORD-001",
              "wmsBookingRef": "WMS-BK-001",
              "confirmedAt": "2024-01-15T14:05:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-001",
          "newStatus": "BookingConfirmed"
        }

+ Response 409 (application/json)

        {
          "error": "invalid_transition",
          "detail": "Order ORD-001 is in status BookingConfirmed. Cannot re-confirm booking."
        }

---

## WMS: Pick Started [/webhooks/wms/pick-started]

### Pick Started [POST]

WMS picker has begun collecting items from shelves. Transitions order to `PickStarted`. (UC3)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: b1c2d3e4-f5a6-7890-bcde-f01234567890
            X-Webhook-Signature: sha256=bbb222...

    + Body

            {
              "orderId": "ORD-001",
              "pickerId": "picker-01",
              "startedAt": "2024-01-15T15:00:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-001",
          "newStatus": "PickStarted"
        }

---

## WMS: Pick Confirmed [/webhooks/wms/pick-confirmed]

### Pick Confirmed [POST]

WMS reports actual picked quantities per line. Triggers POS recalculation if any line quantity differs or if a substitution was recorded. Transitions order to `PickConfirmed`. (UC4)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: a1b2c3d4-e5f6-7890-abcd-ef1234567890
            X-Webhook-Signature: sha256=abc123...

    + Body

            {
              "orderId": "ORD-001",
              "lines": [
                {
                  "orderLineId": "line-001",
                  "sku": "APPLE-1KG",
                  "pickedQty": 5,
                  "substituted": false
                }
              ],
              "pickedAt": "2024-01-15T15:31:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-001",
          "newStatus": "PickConfirmed"
        }

+ Response 409 (application/json)

        {
          "error": "invalid_transition",
          "detail": "Order ORD-001 is in status Cancelled. PickConfirmed is not allowed from this state."
        }

---

## WMS: Packed [/webhooks/wms/packed]

### Packed [POST]

WMS confirms the order is packed into one or more packages, each with a tracking ID and carrier details. Transitions order to `Packed`. (UC4)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: c2d3e4f5-a6b7-8901-cdef-012345678901
            X-Webhook-Signature: sha256=ccc333...

    + Body

            {
              "orderId": "ORD-001",
              "packages": [
                {
                  "trackingId": "TRK-2024-001",
                  "vehicleType": "Van",
                  "weight": 2.5,
                  "lineIds": ["line-001"]
                }
              ],
              "packedAt": "2024-01-15T17:30:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-001",
          "newStatus": "Packed",
          "packagesCreated": 1
        }

---

## WMS: Substitution Offered [/webhooks/wms/substitution-offered]

### Substitution Offered [POST]

WMS reports a picker cannot fully fulfil a line and proposes an alternative SKU. OMS creates an `order_line_substitutions` record, sets `substitution_flag = true` and `pos_recalc_pending = true`, and notifies the customer. (UC5)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: d3e4f5a6-b7c8-9012-defa-123456789012
            X-Webhook-Signature: sha256=ddd444...

    + Body

            {
              "orderId": "ORD-003",
              "orderLineId": "line-003",
              "substituteSku": "MILK-2L",
              "substituteProductName": "Whole Milk (2L)",
              "substituteUnitPrice": 55,
              "substitutedAmount": 1,
              "offeredAt": "2024-01-15T15:10:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "substitutionId": "sub-001",
          "orderId": "ORD-003",
          "customerNotified": true,
          "posRecalcPending": true
        }

---

## WMS: Put Away Confirmed (Returns) [/webhooks/wms/put-away-confirmed]

### Put Away Confirmed [POST]

WMS confirms returned items are on shelf with condition assigned. Triggers atomic refund calculation and credit note issuance. (UC14)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: b2c3d4e5-f6a7-8901-bcde-f12345678901
            X-Webhook-Signature: sha256=def456...

    + Body

            {
              "returnId": "RET-001",
              "items": [
                {
                  "sku": "APPLE-1KG",
                  "condition": "Resellable",
                  "sloc": "B-05"
                }
              ],
              "putAwayAt": "2024-01-15T11:00:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "returnId": "RET-001",
          "newReturnStatus": "PutAway",
          "refundInitiated": true,
          "creditNoteId": "CN-RET-001"
        }

---

## WMS: Goods Receipt Confirmed [/webhooks/wms/goods-receipt-confirmed]

### Goods Receipt Confirmed [POST]

WMS confirms physical goods received against a Purchase Order. Updates received quantities per line. (UC21)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: c3d4e5f6-a7b8-9012-cdef-123456789012
            X-Webhook-Signature: sha256=ghi789...

    + Body

            {
              "purchaseOrderId": "PO-001",
              "goodsReceiveNo": "GRN-2024-001",
              "lines": [
                {
                  "sku": "APPLE-1KG",
                  "receivedQty": 10
                }
              ],
              "receivedAt": "2024-01-15T09:28:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "purchaseOrderId": "PO-001",
          "newStatus": "FullyReceived"
        }

---

## WMS: Purchase Order Put Away Confirmed [/webhooks/wms/purchase-order-put-away-confirmed]

### PO Put Away Confirmed [POST]

WMS confirms inbound goods are shelved. Closes the PO and signals stock availability to OMS. (UC21)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: d4e5f6a7-b8c9-0123-defa-234567890123
            X-Webhook-Signature: sha256=jkl012...

    + Body

            {
              "purchaseOrderId": "PO-001",
              "items": [
                {
                  "sku": "APPLE-1KG",
                  "condition": "Resellable",
                  "sloc": "A-12",
                  "qty": 10
                }
              ],
              "putAwayAt": "2024-01-15T10:14:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "purchaseOrderId": "PO-001",
          "newStatus": "Closed"
        }

---

## WMS: Transfer Pick Confirmed [/webhooks/wms/transfer-pick-confirmed]

### Transfer Pick Confirmed [POST]

WMS at source store confirms items are picked and packed for transfer. Triggers TMS dispatch registration. (UC22)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: e5f6a7b8-c9d0-1234-efab-345678901234
            X-Webhook-Signature: sha256=mno345...

    + Body

            {
              "transferOrderId": "TR-001",
              "lines": [
                {
                  "sku": "APPLE-1KG",
                  "transferredQty": 4
                }
              ],
              "confirmedAt": "2024-01-15T11:00:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "transferOrderId": "TR-001",
          "newStatus": "PickConfirmed"
        }

---

## WMS: Transfer Received [/webhooks/wms/transfer-received]

### Transfer Received [POST]

WMS at destination store confirms stock has arrived and been put away. Completes the Transfer Order. (UC22)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: f6a7b8c9-d0e1-2345-fabc-456789012345
            X-Webhook-Signature: sha256=pqr678...

    + Body

            {
              "transferOrderId": "TR-001",
              "receivedAt": "2024-01-15T14:30:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "transferOrderId": "TR-001",
          "newStatus": "Completed"
        }

---

## WMS: Damaged Goods Received [/webhooks/wms/damaged-goods-received]

### Damaged Goods Received [POST]

WMS confirms a damaged package has been checked in at the warehouse dock (returned by TMS driver). OMS links the receipt to the original order, places the order `OnHold` (saving `preHoldStatus`), and raises a `DamagedGoodsReceivedEvent`. (UC23)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: a8b9c0d1-e2f3-4567-abcd-678901234567
            X-Webhook-Signature: sha256=rrr999...

    + Body

            {
              "orderId": "ORD-006",
              "trackingId": "TRK-2024-006",
              "receivedAt": "2024-01-15T11:30:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-006",
          "damagedReceiptId": "DMG-001",
          "newOrderStatus": "OnHold",
          "holdReason": "PackageDamaged"
        }

---

## WMS: Damaged Goods Put Away [/webhooks/wms/damaged-goods-put-away]

### Damaged Goods Put Away [POST]

WMS confirms damaged items have been inspected, condition assigned, and placed on the appropriate shelf or disposal location. (UC23)

+ Request (application/json)

    + Headers

            X-Source-System: WMS
            X-Idempotency-Key: b9c0d1e2-f3a4-5678-bcde-789012345678
            X-Webhook-Signature: sha256=sss000...

    + Body

            {
              "damagedReceiptId": "DMG-001",
              "items": [
                {
                  "sku": "APPLE-1KG",
                  "condition": "Repairable",
                  "sloc": "DMG-01",
                  "qty": 12
                }
              ],
              "putAwayAt": "2024-01-15T12:00:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "damagedReceiptId": "DMG-001",
          "newStatus": "PutAway"
        }

---

## TMS: Package Dispatched [/webhooks/tms/package-dispatched]

### Package Dispatched [POST]

TMS driver has collected the package and is out for delivery. Updates order and package status. (UC7)

+ Request (application/json)

    + Headers

            X-Source-System: TMS
            X-Idempotency-Key: a7b8c9d0-e1f2-3456-abcd-567890123456
            X-Webhook-Signature: sha256=stu901...

    + Body

            {
              "trackingId": "TRK-2024-001",
              "dispatchedAt": "2024-01-15T17:47:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-001",
          "newOrderStatus": "OutForDelivery",
          "newPackageStatus": "OutForDelivery"
        }

---

## TMS: Package Delivered [/webhooks/tms/package-delivered]

### Package Delivered [POST]

TMS confirms delivery to customer. Triggers invoice generation and payment notification flow. (UC8)

+ Request (application/json)

    + Headers

            X-Source-System: TMS
            X-Idempotency-Key: b8c9d0e1-f2a3-4567-bcde-678901234567
            X-Webhook-Signature: sha256=vwx234...

    + Body

            {
              "trackingId": "TRK-2024-001",
              "deliveredAt": "2024-01-15T19:22:00Z",
              "recipientName": "Alice Johnson",
              "proofOfDelivery": "https://tms.example.com/pod/TRK-2024-001.jpg"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-001",
          "newStatus": "Delivered",
          "invoiceTriggered": true
        }

---

## TMS: Package Damage Reported [/webhooks/tms/package-damage-reported]

### Package Damage Reported [POST]

TMS driver reports a package is damaged before or during delivery attempt. OMS saves `preHoldStatus` and transitions the order to `OnHold` with `holdReason = PackageDamaged`. The driver is instructed to return the goods to the warehouse (UC23). (UC20)

+ Request (application/json)

    + Headers

            X-Source-System: TMS
            X-Idempotency-Key: c0d1e2f3-a4b5-6789-cdef-890123456789
            X-Webhook-Signature: sha256=ttt111...

    + Body

            {
              "trackingId": "TRK-2024-006",
              "reason": "PackageDamaged",
              "driverNote": "Box crushed during transport",
              "reportedAt": "2024-01-15T10:45:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-006",
          "newOrderStatus": "OnHold",
          "holdReason": "PackageDamaged",
          "preHoldStatus": "OutForDelivery"
        }

---

## POS: Collection Ready [/webhooks/pos/pos-collection-ready]

### Collection Ready [POST]

POS confirms a Click & Collect order is packed and ready for customer pickup at the store. Transitions order to `ReadyForCollection`. (UC10)

+ Request (application/json)

    + Headers

            X-Source-System: POS
            X-Idempotency-Key: d1e2f3a4-b5c6-7890-defa-901234567890
            X-Webhook-Signature: sha256=uuu222...

    + Body

            {
              "orderId": "ORD-002",
              "storeId": "store-a",
              "notifiedAt": "2024-01-15T14:30:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-002",
          "newStatus": "ReadyForCollection"
        }

---

## POS: Collected [/webhooks/pos/collected]

### Collected [POST]

POS confirms the customer has collected the Click & Collect order at the store. Transitions order to `Collected` and triggers invoice generation via outbox. (UC11)

+ Request (application/json)

    + Headers

            X-Source-System: POS
            X-Idempotency-Key: e2f3a4b5-c6d7-8901-efab-012345678901
            X-Webhook-Signature: sha256=vvv333...

    + Body

            {
              "orderId": "ORD-002",
              "collectedAt": "2024-01-15T15:00:00Z",
              "collectedBy": "Alice Johnson"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-002",
          "newStatus": "Collected",
          "invoiceTriggered": true
        }

---

## POS: Invoiced [/webhooks/pos/invoiced]

### Invoiced [POST]

POS confirms a fiscal invoice has been issued to the customer after delivery or collection. OMS stores the invoice reference and transitions to `Invoiced`. (UC12)

+ Request (application/json)

    + Headers

            X-Source-System: POS
            X-Idempotency-Key: f3a4b5c6-d7e8-9012-fabc-123456789012
            X-Webhook-Signature: sha256=www444...

    + Body

            {
              "orderId": "ORD-001",
              "invoiceNumber": "INV-2024-001",
              "totalAmount": 2380,
              "currency": "THB",
              "invoicedAt": "2024-01-15T19:25:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-001",
          "newStatus": "Invoiced",
          "invoiceId": "inv-001"
        }

---

## POS: Payment Confirmed [/webhooks/pos/payment-confirmed]

### Payment Confirmed [POST]

POS confirms the customer has paid. Transitions the order to the terminal `Paid` status. (UC13)

+ Request (application/json)

    + Headers

            X-Source-System: POS
            X-Idempotency-Key: a4b5c6d7-e8f9-0123-abcd-234567890123
            X-Webhook-Signature: sha256=xxx555...

    + Body

            {
              "orderId": "ORD-001",
              "invoiceNumber": "INV-2024-001",
              "paymentMethod": "CreditCard",
              "paidAmount": 2380,
              "currency": "THB",
              "paidAt": "2024-01-15T19:31:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-001",
          "newStatus": "Paid"
        }

---

## POS: Recalculation Result [/webhooks/pos/recalculation-result]

### Recalculation Result [POST]

POS returns the final adjusted total after applying promotions to actual picked quantities.
Clears `posRecalcPending = false` and updates `order_line_amounts` for the new recalc round. (UC15)

+ Request (application/json)

    + Headers

            X-Source-System: POS
            X-Idempotency-Key: c9d0e1f2-a3b4-5678-cdef-789012345678
            X-Webhook-Signature: sha256=yz0123...

    + Body

            {
              "orderId": "ORD-001",
              "originalAmount": 2450,
              "adjustedAmount": 2380,
              "currency": "THB",
              "promotionsApplied": [
                {
                  "promoCode": "FRESH10",
                  "discountAmount": 70,
                  "description": "10% fresh produce discount"
                }
              ],
              "recalculatedAt": "2024-01-15T15:36:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-001",
          "finalAmount": 2380,
          "posRecalcPendingCleared": true
        }

---

## POS: Recalculation Completed [/webhooks/pos/pos-recalc-completed]

### Recalculation Completed [POST]

POS confirms the full recalculation cycle is closed — all line amounts are finalised, promotions committed, and the order may proceed to packing.
Sent after `recalculation-result` once POS has persisted the fiscal record on its side. (UC15)

+ Request (application/json)

    + Headers

            X-Source-System: POS
            X-Idempotency-Key: d0e1f2a3-b4c5-6789-defa-890123456789
            X-Webhook-Signature: sha256=yyy666...

    + Body

            {
              "orderId": "ORD-001",
              "finalAmount": 2380,
              "currency": "THB",
              "completedAt": "2024-01-15T15:37:00Z"
            }

+ Response 202 (application/json)

        {
          "accepted": true,
          "orderId": "ORD-001",
          "posRecalcPending": false
        }

---

# Group Error Reference

All error responses follow a consistent envelope:

```json
{
  "error": "<machine_readable_code>",
  "detail": "<human_readable_explanation>",
  "traceId": "<opentelemetry_trace_id>"
}
```

| HTTP Status | `error` code | When it occurs |
|---|---|---|
| `400` | `invalid_parameter` | Query param or body field fails validation |
| `400` | `invalid_transition` | Order state machine rejects the requested transition |
| `401` | `unauthorized` | Bearer token missing, expired, or invalid |
| `403` | `forbidden` | Token valid but lacks permission for this resource |
| `404` | `not_found` | Resource ID does not exist |
| `409` | `conflict` | Idempotency key already processed with a different body |
| `422` | `unprocessable` | Body is valid JSON but semantically incorrect (e.g. receivedQty > orderedQty) |
| `500` | `internal_error` | Unexpected server error — check `traceId` in Grafana |
| `503` | `service_unavailable` | Outbox worker is paused or DB is unhealthy |
