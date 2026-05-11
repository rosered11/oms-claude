# OMS API — Field-to-Database Mapping

Maps every response field (read APIs) and every request body field (webhook and write APIs) to the exact table and column in the ER diagrams.

**Schema prefixes used throughout:**
| Schema | Module |
|---|---|
| `orders.*` | Order Module |
| `payment.*` | Payment Module |
| `returns.*` | Returns Module |
| `config.*` | Configuration Module |
| `inbound.*` | Inbound Module |

---

## Contents

1. [GET /orders — List Orders](#1-get-orders--list-orders)
2. [GET /orders/{id} — Get Order](#2-get-ordersid--get-order)
3. [GET /orders/{id}/timeline — Get Order Timeline](#3-get-ordersidtimeline--get-order-timeline)
4. [GET /orders/{id}/lines — List Order Lines](#4-get-ordersidlines--list-order-lines)
5. [GET /orders/{id}/packages — List Packages](#5-get-ordersidpackages--list-packages)
6. [GET /orders/{id}/webhooks — List Webhook Events](#6-get-ordersidwebhooks--list-webhook-events)
7. [GET /orders/{id}/substitutions — List Substitutions](#7-get-ordersidsubstitutions--list-substitutions)
8. [GET /orders/{id}/delivery-slot — Get Delivery Slot](#8-get-ordersiddelivery-slot--get-delivery-slot)
9. [POST /orders — Create Order](#9-post-orders--create-order)
10. [PATCH /orders/{id}/hold — Place Hold](#10-patch-ordersidhold--place-hold)
11. [PATCH /orders/{id}/release-hold — Release Hold](#11-patch-ordersidrelease-hold--release-hold)
12. [PATCH /orders/{id}/cancel — Cancel Order](#12-patch-ordersidcancel--cancel-order)
13. [POST /orders/{id}/substitutions/{subId}/approve](#13-post-ordersidsubstitutionssubidapprove)
14. [POST /orders/{id}/substitutions/{subId}/reject](#14-post-ordersidsubstitutionssubidreject)
15. [POST /orders/{id}/recalculate — Trigger Recalculation](#15-post-ordersidrecalculate--trigger-recalculation)
16. [PATCH /orders/{id}/delivery-slot — Update Delivery Slot](#16-patch-ordersiddelivery-slot--update-delivery-slot)
17. [GET /orders/{id}/timeline — Get Order Timeline](#3-get-ordersidtimeline--get-order-timeline)
18. [POST /returns — Create Return](#18-post-returns--create-return)
19. [GET /returns — List Returns](#19-get-returns--list-returns)
20. [GET /returns/{id} — Get Return](#20-get-returnsid--get-return)
21. [GET /returns/{id}/items — List Return Items](#21-get-returnsiditemslist-return-items)
22. [PATCH /returns/{id}/cancel — Cancel Return](#22-patch-returnsidcancel--cancel-return)
23. [GET /returns/{id}/refund — Get Refund](#23-get-returnsidrefund--get-refund)
24. [GET /inbound/purchase-orders — List Purchase Orders](#24-get-inboundpurchase-orders--list-purchase-orders)
25. [GET /inbound/purchase-orders/{id} — Get Purchase Order](#25-get-inboundpurchase-ordersid--get-purchase-order)
26. [POST /inbound/purchase-orders — Create Purchase Order](#26-post-inboundpurchase-orders--create-purchase-order)
27. [GET /inbound/purchase-orders/{id}/goods-receipts](#27-get-inboundpurchase-ordersidgoods-receipts)
28. [GET /inbound/transfer-orders — List Transfer Orders](#28-get-inboundtransfer-orders--list-transfer-orders)
29. [GET /inbound/transfer-orders/{id} — Get Transfer Order](#29-get-inboundtransfer-ordersid--get-transfer-order)
30. [POST /inbound/transfer-orders — Create Transfer Order](#30-post-inboundtransfer-orders--create-transfer-order)
31. [GET /inbound/transfer-orders/{id}/confirmations](#31-get-inboundtransfer-ordersidconfirmations)
32. [GET /stock/{sku}/ledger — Get Stock Ledger](#32-get-stockskuledger--get-stock-ledger)
33. [POST /webhooks/wms/booking-confirmed](#33-post-webhookswmsbooking-confirmed)
34. [POST /webhooks/wms/pick-started](#34-post-webhookswmspick-started)
35. [POST /webhooks/wms/pick-confirmed](#35-post-webhookswmspick-confirmed)
36. [POST /webhooks/wms/packed](#36-post-webhookswmspacked)
37. [POST /webhooks/wms/substitution-offered](#37-post-webhookswmssubstitution-offered)
38. [POST /webhooks/wms/put-away-confirmed](#38-post-webhookswmsput-away-confirmed)
39. [POST /webhooks/wms/goods-receipt-confirmed](#39-post-webhookswmsgoods-receipt-confirmed)
40. [POST /webhooks/wms/purchase-order-put-away-confirmed](#40-post-webhookswmspurchase-order-put-away-confirmed)
41. [POST /webhooks/wms/transfer-pick-confirmed](#41-post-webhookswmstransfer-pick-confirmed)
42. [POST /webhooks/wms/transfer-received](#42-post-webhookswmstransfer-received)
43. [POST /webhooks/wms/damaged-goods-received](#43-post-webhookswmsdamaged-goods-received)
44. [POST /webhooks/wms/damaged-goods-put-away](#44-post-webhookswmsdamaged-goods-put-away)
45. [POST /webhooks/tms/package-dispatched](#45-post-webhookstmspackage-dispatched)
46. [POST /webhooks/tms/package-delivered](#46-post-webhookstmspackage-delivered)
47. [POST /webhooks/tms/package-damage-reported](#47-post-webhookstmspackage-damage-reported)
48. [POST /webhooks/pos/pos-collection-ready](#48-post-webhookspospos-collection-ready)
49. [POST /webhooks/pos/collected](#49-post-webhooksposcollected)
50. [POST /webhooks/pos/invoiced](#50-post-webhooksposinvoiced)
51. [POST /webhooks/pos/payment-confirmed](#51-post-webhookspospayment-confirmed)
52. [POST /webhooks/pos/recalculation-result](#52-post-webhooksposrecalculation-result)
53. [POST /webhooks/pos/pos-recalc-completed](#53-post-webhookspospos-recalc-completed)

---

## 1. GET /orders — List Orders

Response: `items[]` array (paginated).

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `id` | string | `orders.orders` | `order_id` | UUID displayed as order_number string |
| `orderNumber` | string | `orders.orders` | `order_number` | |
| `customer` | string | `orders.order_addresses` | `first_name + last_name` | `address_type = 'Delivery'`; concat at query time |
| `items` | number | `orders.order_lines` | COUNT(`order_line_id`) | Grouped by `order_id` |
| `type` | string | `orders.orders` | `fulfillment_type` | Enum: `Delivery`, `Express`, `ClickAndCollect` |
| `status` | string | `orders.orders` | `status` | |
| `store` | string | `config.store_locations` | `store_name` | JOIN on `orders.store_id = store_locations.store_id` |
| `amount` | number | `payment.order_line_amounts` | SUM `net_amount` | Latest `recalc_round` per `order_line_id`; falls back to `orders.order_lines.original_unit_price × picked_amount` |
| `holdReason` | string \| null | `orders.orders` | `hold_reason` | `null` when not on hold |
| `orderDate` | string | `orders.orders` | `order_date` | Date portion (YYYY-MM-DD) |
| `channelType` | string | `orders.orders` | `channel_type` | e.g. `Web`, `App`, `POS` |
| `businessUnit` | string | `orders.orders` | `business_unit` | e.g. `SC01` |
| `paymentMethod` | string | `orders.orders` | `payment_method` | e.g. `CreditCard`, `PromptPay` |
| `substitutionFlag` | boolean | `orders.orders` | `substitution_flag` | `true` if any line has a recorded substitution |
| `posRecalcPending` | boolean | `orders.orders` | `pos_recalc_pending` | `true` while awaiting POS recalculation response |
| `deliverySlot.scheduledStart` | timestamp | `orders.delivery_slots` | `scheduled_start` | `null` for `ClickAndCollect` with no slot |
| `deliverySlot.scheduledEnd` | timestamp | `orders.delivery_slots` | `scheduled_end` | |
| `createdAt` | timestamp | `orders.orders` | `created_at` | |
| `updatedAt` | timestamp | `orders.orders` | `updated_at` | |
| `total` | number | — | — | COUNT of matching rows; pagination metadata |
| `page` | number | — | — | Query parameter echo |
| `limit` | number | — | — | Query parameter echo |

---

## 2. GET /orders/{id} — Get Order

Response: single order object with nested `customer`, `deliveryAddress`, `deliverySlot`, `lines[]`, `packages[]`.

### Root fields

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `id` | string | `orders.orders` | `order_id` | |
| `orderNumber` | string | `orders.orders` | `order_number` | |
| `type` | string | `orders.orders` | `fulfillment_type` | |
| `channelType` | string | `orders.orders` | `channel_type` | |
| `businessUnit` | string | `orders.orders` | `business_unit` | |
| `status` | string | `orders.orders` | `status` | |
| `store` | string | `config.store_locations` | `store_name` | JOIN `orders.store_id` |
| `amount` | number | `payment.order_line_amounts` | SUM `net_amount` | Latest recalc round |
| `originalAmount` | number | `orders.order_lines` | SUM(`original_unit_price × requested_amount`) | Before POS recalculation |
| `orderDate` | string | `orders.orders` | `order_date` | Date portion (YYYY-MM-DD) |
| `paymentMethod` | string | `orders.orders` | `payment_method` | |
| `substitutionFlag` | boolean | `orders.orders` | `substitution_flag` | |
| `posRecalcPending` | boolean | `orders.orders` | `pos_recalc_pending` | |
| `preHoldStatus` | string \| null | `orders.orders` | `pre_hold_status` | Status saved when order placed on hold; `null` when not OnHold |
| `holdReason` | string \| null | `orders.orders` | `hold_reason` | |
| `createdAt` | timestamp | `orders.orders` | `created_at` | |
| `updatedAt` | timestamp | `orders.orders` | `updated_at` | |

### `customer` object

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `customer.name` | string | `orders.order_addresses` | `first_name + last_name` | `address_type = 'Delivery'` |
| `customer.phone` | string | `orders.order_addresses` | `mobile_phone` | |
| `customer.email` | string | `orders.order_addresses` | `email` | |

### `deliveryAddress` object

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `deliveryAddress.address1` | string | `orders.order_addresses` | `address1` | `address_type = 'Delivery'` |
| `deliveryAddress.subdistrict` | string | `orders.order_addresses` | `subdistrict` | |
| `deliveryAddress.district` | string | `orders.order_addresses` | `district` | |
| `deliveryAddress.province` | string | `orders.order_addresses` | `province` | |
| `deliveryAddress.postalCode` | string | `orders.order_addresses` | `postal_code` | |

### `deliverySlot` object

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `deliverySlot.scheduledStart` | timestamp | `orders.delivery_slots` | `scheduled_start` | Full ISO 8601 timestamp |
| `deliverySlot.scheduledEnd` | timestamp | `orders.delivery_slots` | `scheduled_end` | |

### `lines[]` array

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `orderLineId` | string | `orders.order_lines` | `order_line_id` | |
| `sku` | string | `orders.order_lines` | `sku` | |
| `productName` | string | `orders.order_lines` | `product_name` | Denormalized from product catalog at order creation |
| `barcode` | string | `orders.order_lines` | `barcode` | EAN/UPC |
| `status` | string | `orders.order_lines` | `status` | Per-line status (may differ from order status) |
| `isSubstitute` | boolean | `orders.order_lines` | `is_substitute` | `true` if this line was added as a substitute |
| `requestedQty` | number | `orders.order_lines` | `requested_amount` | |
| `pickedQty` | number | `orders.order_lines` | `picked_amount` | Updated by `PickConfirmed` webhook |
| `unitPrice` | number | `payment.order_line_amounts` | `recalculated_unit_price` | Latest recalc round; falls back to `order_lines.original_unit_price` |
| `totalPrice` | number | `payment.order_line_amounts` | `net_amount` | Latest recalc round per line |
| `unitOfMeasure` | string | `orders.order_lines` | `unit_of_measure` | |

### `packages[]` array

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `packageId` | string | `orders.order_packages` | `package_id` | |
| `trackingId` | string | `orders.order_packages` | `tracking_id` | |
| `vehicleType` | string | `orders.order_packages` | `vehicle_type` | |
| `weight` | number | `orders.order_packages` | `package_weight` | Decimal kg |
| `status` | string | `orders.order_packages` | `status` | |
| `lineIds` | array | `orders.order_package_lines` | `order_line_id[]` | All lines assigned to this package; grouped by `package_id` |

---

## 3. GET /orders/{id}/timeline — Get Order Timeline

Response: `order` summary + `events[]` + `summary` metrics.

### `order` summary object

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `order.id` | string | `orders.orders` | `order_id` | |
| `order.customer` | string | `orders.order_addresses` | `first_name + last_name` | |
| `order.store` | string | `config.store_locations` | `store_name` | |
| `order.items` | number | `orders.order_lines` | COUNT(`order_line_id`) | |
| `order.status` | string | `orders.orders` | `status` | |
| `order.type` | string | `orders.orders` | `fulfillment_type` | |
| `order.amount` | number | `payment.order_line_amounts` | SUM `net_amount` | Latest recalc round |
| `order.linkedPoId` | string \| null | `orders.order_webhook_logs` | `detail` | Parsed from first `PurchaseOrderCreated` webhook log entry |

### `events[]` array

Each event row is built from one of three source tables depending on `type`:

| `type` value | Primary source table | |
|---|---|---|
| `domain` | `orders.order_status_history` | OMS state transition |
| `webhook` | `orders.order_webhook_logs` | Received from WMS / TMS / POS |
| `outbox` | `orders.order_outbox` | Dispatched to WMS / TMS / POS |
| `bridge` | Derived marker | No stored row; synthesized between PO put-away and first order event |

| Response Field | Type | `domain` source | `webhook` source | `outbox` source |
|---|---|---|---|---|
| `id` | number | Row sequence | Row sequence | Row sequence |
| `occurredAt` | timestamp | `order_status_history.changed_at` | `order_webhook_logs.received_at` | `order_outbox.created_at` |
| `time` | string | HH:MM from `changed_at` | HH:MM from `received_at` | HH:MM from `created_at` |
| `phase` | string | Annotated at query time (`inbound`/`outbound`) | Annotated at query time | Annotated at query time |
| `type` | string | `'domain'` (fixed) | `'webhook'` (fixed) | `'outbox'` (fixed) |
| `system` | string | `'OMS'` (fixed) | `order_webhook_logs.source_system` | Destination system derived from `order_outbox.event_type` |
| `event` | string | `order_status_history.to_status` | `order_webhook_logs.event_type` | `order_outbox.event_type` |
| `detail` | string | `order_status_history.detail` | `order_webhook_logs.detail` | Derived from `order_outbox.event_payload` |
| `outStatus` | string \| null | `null` | `null` | `order_outbox.status` |

### `summary` object

| Response Field | Type | Derived from |
|---|---|---|
| `totalEvents` | number | COUNT of all events across all three tables |
| `inboundPhaseEvents` | number | COUNT where `phase = 'inbound'` |
| `bridgeEvents` | number | COUNT of synthesized bridge markers |
| `outboundPhaseEvents` | number | COUNT where `phase = 'outbound'` |
| `inboundToStockAvailableMinutes` | number | `bridge.occurredAt − first_inbound_event.occurredAt` |
| `orderToDeliveredMinutes` | number | `order_status_history(Delivered).changed_at − order_status_history(Pending).changed_at` |
| `totalEndToEndMinutes` | number | `last_event.occurredAt − first_event.occurredAt` |

---

## 4. GET /orders/{id}/lines — List Order Lines

Response: `orderId` + `lines[]` array. Fields identical to the `lines[]` array documented in [Section 2](#2-get-ordersid--get-order).

---

## 5. GET /orders/{id}/packages — List Packages

Response: `orderId` + `packages[]` array. Fields identical to the `packages[]` array documented in [Section 2](#2-get-ordersid--get-order).

---

## 6. GET /orders/{id}/webhooks — List Webhook Events

Response: `orderId` + `webhooks[]` array.

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `webhookLogId` | string | `orders.order_webhook_logs` | `webhook_log_id` | |
| `sourceSystem` | string | `orders.order_webhook_logs` | `source_system` | `WMS`, `TMS`, or `POS` |
| `eventType` | string | `orders.order_webhook_logs` | `event_type` | |
| `detail` | string | `orders.order_webhook_logs` | `detail` | Human-readable summary |
| `receivedAt` | timestamp | `orders.order_webhook_logs` | `received_at` | |

---

## 7. GET /orders/{id}/substitutions — List Substitutions

Response: `orderId` + `substitutions[]` array.

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `substitutionId` | string | `orders.order_line_substitutions` | `substitution_id` | |
| `orderLineId` | string | `orders.order_line_substitutions` | `order_line_id` | FK to original line |
| `originalSku` | string | `orders.order_lines` | `sku` | Via JOIN on `order_line_id` |
| `originalProductName` | string | `orders.order_lines` | `product_name` | |
| `substituteSku` | string | `orders.order_line_substitutions` | `substitute_sku` | |
| `substituteProductName` | string | `orders.order_line_substitutions` | `substitute_product_name` | |
| `substituteUnitPrice` | number | `orders.order_line_substitutions` | `substitute_unit_price` | |
| `substitutedAmount` | number | `orders.order_line_substitutions` | `substituted_amount` | |
| `customerApproved` | boolean \| null | `orders.order_line_substitutions` | `customer_approved` | `null` = pending decision |
| `approvedAt` | timestamp \| null | `orders.order_line_substitutions` | `approved_at` | Set on approval or rejection |
| `createdAt` | timestamp | `orders.order_line_substitutions` | `created_at` | |

---

## 8. GET /orders/{id}/delivery-slot — Get Delivery Slot

Response: delivery slot for the order.

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `orderId` | string | `orders.orders` | `order_id` | |
| `slotId` | string | `orders.delivery_slots` | `slot_id` | |
| `scheduledStart` | timestamp | `orders.delivery_slots` | `scheduled_start` | |
| `scheduledEnd` | timestamp | `orders.delivery_slots` | `scheduled_end` | |
| `storeId` | string | `orders.delivery_slots` | `store_id` | |

---

## 9. POST /orders — Create Order

**Trigger:** Customer or system creates a new outbound order.

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `sourceOrderId` | `orders.orders` | `source_order_id` | INSERT |
| `channelType` | `orders.orders` | `channel_type` | INSERT |
| `businessUnit` | `orders.orders` | `business_unit` | INSERT |
| `storeId` | `orders.orders` | `store_id` | INSERT |
| `fulfillmentType` | `orders.orders` | `fulfillment_type` | INSERT |
| `paymentMethod` | `orders.orders` | `payment_method` | INSERT |
| `customer.name` | `orders.order_addresses` | `first_name`, `last_name` | INSERT with `address_type = 'Delivery'` |
| `customer.phone` | `orders.order_addresses` | `mobile_phone` | INSERT |
| `customer.email` | `orders.order_addresses` | `email` | INSERT |
| `deliveryAddress.*` | `orders.order_addresses` | `address1`, `subdistrict`, `district`, `province`, `postal_code` | INSERT |
| `deliverySlot.scheduledStart` | `orders.delivery_slots` | `scheduled_start` | INSERT |
| `deliverySlot.scheduledEnd` | `orders.delivery_slots` | `scheduled_end` | INSERT |
| `lines[].sku` | `orders.order_lines` | `sku` | INSERT per line |
| `lines[].productName` | `orders.order_lines` | `product_name` | INSERT (denormalized) |
| `lines[].barcode` | `orders.order_lines` | `barcode` | INSERT |
| `lines[].requestedQty` | `orders.order_lines` | `requested_amount` | INSERT |
| `lines[].unitPrice` | `orders.order_lines` | `original_unit_price` | INSERT |
| `lines[].unitOfMeasure` | `orders.order_lines` | `unit_of_measure` | INSERT |
| *(derived)* | `orders.orders` | `status = 'Pending'`, `order_number`, `created_at` | INSERT |
| *(derived)* | `orders.order_status_history` | `from_status = null`, `to_status = 'Pending'`, `changed_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderCreatedEvent'`, `event_payload`, `status = 'Pending'` | INSERT |

---

## 10. PATCH /orders/{id}/hold — Place Hold

**Trigger:** Supervisor places active order on hold for manual review.

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `holdReason` | `orders.orders` | `hold_reason` | UPDATE |
| `heldBy` | `orders.orders` | `updated_by` | UPDATE |
| *(derived — saves current status)* | `orders.orders` | `pre_hold_status` = current `status` | UPDATE |
| *(derived)* | `orders.orders` | `status` → `'OnHold'`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_status_history` | `from_status`, `to_status = 'OnHold'`, `changed_by = heldBy`, `changed_at` | INSERT |
| *(derived)* | `orders.order_holds` | `order_id`, `hold_reason`, `held_at`, `held_by` | INSERT |

---

## 11. PATCH /orders/{id}/release-hold — Release Hold

**Trigger:** Supervisor releases order back to its pre-hold status.

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `releasedBy` | `orders.orders` | `updated_by` | UPDATE |
| *(derived — restores saved status)* | `orders.orders` | `status` ← `pre_hold_status` | UPDATE |
| *(derived)* | `orders.orders` | `pre_hold_status` → `null`, `hold_reason` → `null`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_status_history` | `from_status = 'OnHold'`, `to_status = pre_hold_status`, `changed_by`, `changed_at` | INSERT |
| *(derived)* | `orders.order_holds` | `released_at`, `released_by` | UPDATE latest open hold record |

---

## 12. PATCH /orders/{id}/cancel — Cancel Order

**Trigger:** Customer or supervisor cancels before dispatch.

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `reason` | `orders.order_status_history` | `detail` | Part of INSERT |
| `cancelledBy` | `orders.orders` | `updated_by` | UPDATE |
| *(derived)* | `orders.orders` | `status` → `'Cancelled'`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_status_history` | `from_status`, `to_status = 'Cancelled'`, `changed_by`, `detail`, `changed_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderCancelledEvent'`, `event_payload` | INSERT |

---

## 13. POST /orders/{id}/substitutions/{subId}/approve

**Trigger:** Customer approves a proposed substitution.

### DB writes

| Field | Table Written | Column Updated | Action |
|---|---|---|---|
| *(derived — subId)* | `orders.order_line_substitutions` | `customer_approved = true`, `approved_at` | UPDATE |
| *(derived)* | `orders.orders` | `updated_at` | UPDATE |
| *(derived — if posRecalcPending)* | `orders.order_outbox` | `event_type = 'SubstitutionApprovedEvent'` | INSERT |

---

## 14. POST /orders/{id}/substitutions/{subId}/reject

**Trigger:** Customer rejects a proposed substitution; original line is voided.

### DB writes

| Field | Table Written | Column Updated | Action |
|---|---|---|---|
| *(derived — subId)* | `orders.order_line_substitutions` | `customer_approved = false`, `approved_at` | UPDATE |
| *(derived)* | `orders.order_lines` | `status → 'Voided'` for original line | UPDATE |
| *(derived)* | `orders.orders` | `updated_at` | UPDATE |
| *(derived)* | `orders.order_outbox` | `event_type = 'SubstitutionRejectedEvent'` | INSERT (triggers recalc) |

---

## 15. POST /orders/{id}/recalculate — Trigger Recalculation

**Trigger:** Manual or automatic POS recalculation trigger.

### DB writes

| Field | Table Written | Column Updated | Action |
|---|---|---|---|
| *(derived)* | `orders.orders` | `pos_recalc_pending = true`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_outbox` | `event_type = 'RecalcRequestedEvent'`, `event_payload` | INSERT |

---

## 16. PATCH /orders/{id}/delivery-slot — Update Delivery Slot

**Trigger:** Customer or operator reschedules the delivery window.

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `scheduledStart` | `orders.delivery_slots` | `scheduled_start` | UPDATE |
| `scheduledEnd` | `orders.delivery_slots` | `scheduled_end` | UPDATE |
| *(derived)* | `orders.delivery_slots` | `updated_at` | UPDATE |

---

## 17. GET /orders/{id}/timeline

See [Section 3](#3-get-ordersidtimeline--get-order-timeline).

---

## 18. POST /returns — Create Return

**Trigger:** Customer initiates a return for a delivered or paid order.

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `returns.returns` | `order_id` | INSERT |
| `returnReason` | `returns.returns` | `return_reason` | INSERT |
| `items[].orderLineId` | `returns.return_items` | `order_line_id` | INSERT per item |
| `items[].sku` | `returns.return_items` | `sku` | INSERT |
| `items[].quantity` | `returns.return_items` | `quantity` | INSERT |
| `items[].itemReason` | `returns.return_items` | `item_reason` | INSERT |
| `requestedBy` | `returns.returns` | `created_by` | INSERT |
| *(derived)* | `returns.returns` | `status = 'ReturnRequested'`, `return_order_number`, `requested_at`, `created_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'ReturnRequestedEvent'`, `event_payload` | INSERT (triggers TMS pickup scheduling) |

---

## 19. GET /returns — List Returns

Response: `items[]` array (paginated).

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `id` | string | `returns.returns` | `return_id` | |
| `returnOrderNumber` | string | `returns.returns` | `return_order_number` | |
| `orderId` | string | `returns.returns` | `order_id` | |
| `status` | string | `returns.returns` | `status` | |
| `returnReason` | string | `returns.returns` | `return_reason` | |
| `requestedAt` | timestamp | `returns.returns` | `requested_at` | |
| `refundedAt` | timestamp \| null | `returns.returns` | `refunded_at` | |
| `createdAt` | timestamp | `returns.returns` | `created_at` | |
| `updatedAt` | timestamp | `returns.returns` | `updated_at` | |

---

## 20. GET /returns/{id} — Get Return

Response: full return record. Root fields identical to the list item above, plus:

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `invoiceId` | string \| null | `returns.returns` | `invoice_id` | Linked invoice (from original order) |
| `creditNoteId` | string \| null | `returns.returns` | `credit_note_id` | Issued credit note |
| `goodsReceiveNo` | string \| null | `returns.returns` | `goods_receive_no` | Set when WMS receives returned goods |
| `pickupScheduledAt` | timestamp \| null | `returns.returns` | `pickup_scheduled_at` | |
| `pickedUpAt` | timestamp \| null | `returns.returns` | `picked_up_at` | |
| `receivedAt` | timestamp \| null | `returns.returns` | `received_at` | |
| `inspectedAt` | timestamp \| null | `returns.returns` | `inspected_at` | |
| `putAwayAt` | timestamp \| null | `returns.returns` | `put_away_at` | |

---

## 21. GET /returns/{id}/items — List Return Items

Response: `returnId` + `items[]` array.

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `returnItemId` | string | `returns.return_items` | `return_item_id` | |
| `orderLineId` | string | `returns.return_items` | `order_line_id` | |
| `sku` | string | `returns.return_items` | `sku` | |
| `productName` | string | `returns.return_items` | `product_name` | Denormalized at return creation |
| `barcode` | string | `returns.return_items` | `barcode` | |
| `quantity` | number | `returns.return_items` | `quantity` | |
| `unitOfMeasure` | string | `returns.return_items` | `unit_of_measure` | |
| `unitPrice` | number | `returns.return_items` | `unit_price` | |
| `currency` | string | `returns.return_items` | `currency` | |
| `itemReason` | string | `returns.return_items` | `item_reason` | |
| `condition` | string \| null | `returns.return_items` | `condition` | Set at inspection: `Resellable`, `Repairable`, `Dispose` |
| `putAwayStatus` | string \| null | `returns.return_items` | `put_away_status` | |
| `assignedSloc` | string \| null | `returns.return_items` | `assigned_sloc` | Storage location assigned at put-away |
| `inspectedAt` | timestamp \| null | `returns.return_items` | `inspected_at` | |
| `putAwayAt` | timestamp \| null | `returns.return_items` | `put_away_at` | |

---

## 22. PATCH /returns/{id}/cancel — Cancel Return

**Trigger:** Operator cancels a return before goods receipt.

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `reason` | `returns.returns` | Stored in `updated_by` or status history detail | UPDATE |
| `cancelledBy` | `returns.returns` | `updated_by` | UPDATE |
| *(derived)* | `returns.returns` | `status → 'Cancelled'`, `updated_at` | UPDATE |

---

## 23. GET /returns/{id}/refund — Get Refund

Response: `returnId` + `refund` object + `creditNote` object.

### `refund` object

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `refundId` | string | `returns.return_refunds` | `refund_id` | |
| `refundAmount` | number | `returns.return_refunds` | `refund_amount` | |
| `currency` | string | `returns.return_refunds` | `currency` | |
| `refundMethod` | string | `returns.return_refunds` | `refund_method` | |
| `status` | string | `returns.return_refunds` | `status` | |
| `referenceNo` | string | `returns.return_refunds` | `reference_no` | |
| `processedAt` | timestamp | `returns.return_refunds` | `processed_at` | |

### `creditNote` object

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `creditNoteId` | string | `payment.credit_notes` | `credit_note_id` | |
| `creditNoteNumber` | string | `payment.credit_notes` | `credit_note_number` | |
| `invoiceId` | string | `payment.credit_notes` | `invoice_id` | |
| `amount` | number | `payment.credit_notes` | `amount` | |
| `currency` | string | `payment.credit_notes` | `currency` | |
| `reason` | string | `payment.credit_notes` | `reason` | `'Return'` |
| `status` | string | `payment.credit_notes` | `status` | |

---

## 24. GET /inbound/purchase-orders — List Purchase Orders

Response: `items[]` array.

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `id` | string | `inbound.purchase_orders` | `purchase_order_id` | |
| `poNumber` | string | `inbound.purchase_orders` | `po_number` | |
| `supplier` | string | External supplier registry | — | Supplier name resolved via `supplier_id`; not in OMS ER |
| `supplierId` | string | `inbound.purchase_orders` | `supplier_id` | |
| `lines` | number | `inbound.purchase_order_lines` | COUNT(`po_line_id`) | Grouped by `purchase_order_id` |
| `status` | string | `inbound.purchase_orders` | `status` | Enum: `Created`, `PartiallyReceived`, `FullyReceived`, `Closed` |
| `store` | string | `config.store_locations` | `store_name` | JOIN `inbound.purchase_orders.store_id` |
| `value` | number | `inbound.purchase_order_lines` | SUM(`ordered_qty × unit_cost`) | |
| `goodsReceiveNo` | string \| null | `inbound.purchase_orders` | `goods_receive_no` | |
| `createdAt` | timestamp | `inbound.purchase_orders` | `created_at` | |
| `updatedAt` | timestamp | `inbound.purchase_orders` | `updated_at` | |

---

## 25. GET /inbound/purchase-orders/{id} — Get Purchase Order

Root fields identical to the list item above. Additional fields:

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `storeId` | string | `inbound.purchase_orders` | `store_id` | |

### `lines[]` array

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `poLineId` | string | `inbound.purchase_order_lines` | `po_line_id` | |
| `sku` | string | `inbound.purchase_order_lines` | `sku` | |
| `productName` | string | External product catalog | — | Resolved by SKU |
| `orderedQty` | number | `inbound.purchase_order_lines` | `ordered_qty` | |
| `receivedQty` | number | `inbound.purchase_order_lines` | `received_qty` | Updated by `GoodsReceiptConfirmed` webhook |
| `unitCost` | number | `inbound.purchase_order_lines` | `unit_cost` | |
| `currency` | string | `inbound.purchase_order_lines` | `currency` | |
| `condition` | string | `inbound.purchase_order_lines` | `condition` | Set by `PutAwayConfirmed`: `Resellable`, `Repairable`, `Dispose` |
| `sloc` | string | `inbound.purchase_order_lines` | `sloc` | Storage location set at put-away |
| `receivedAt` | timestamp | `inbound.purchase_order_lines` | `received_at` | Set by `GoodsReceiptConfirmed` |
| `putAwayAt` | timestamp | `inbound.purchase_order_lines` | `put_away_at` | Set by `PurchaseOrderPutAwayConfirmed` |

---

## 26. POST /inbound/purchase-orders — Create Purchase Order

**Trigger:** ERP/JDA or operator creates a PO for expected inbound goods.

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `poNumber` | `inbound.purchase_orders` | `po_number` | INSERT |
| `supplierId` | `inbound.purchase_orders` | `supplier_id` | INSERT |
| `storeId` | `inbound.purchase_orders` | `store_id` | INSERT |
| `lines[].sku` | `inbound.purchase_order_lines` | `sku` | INSERT per line |
| `lines[].orderedQty` | `inbound.purchase_order_lines` | `ordered_qty` | INSERT |
| `lines[].unitCost` | `inbound.purchase_order_lines` | `unit_cost` | INSERT |
| `lines[].currency` | `inbound.purchase_order_lines` | `currency` | INSERT |
| *(derived)* | `inbound.purchase_orders` | `status = 'Created'`, `created_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'PurchaseOrderCreatedEvent'`, `event_payload` | INSERT (notifies WMS of expected receipt) |

---

## 27. GET /inbound/purchase-orders/{id}/goods-receipts

Response: `purchaseOrderId` + `goodsReceipts[]` array.

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `goodsReceiveNo` | string | `inbound.purchase_orders` | `goods_receive_no` | |
| `status` | string | `inbound.purchase_orders` | `status` | |
| `receivedAt` | timestamp | `inbound.purchase_order_lines` | MIN(`received_at`) | Earliest receipt timestamp |
| `putAwayAt` | timestamp | `inbound.purchase_order_lines` | MIN(`put_away_at`) | |
| `lines[].sku` | string | `inbound.purchase_order_lines` | `sku` | |
| `lines[].receivedQty` | number | `inbound.purchase_order_lines` | `received_qty` | |
| `lines[].condition` | string | `inbound.purchase_order_lines` | `condition` | |
| `lines[].sloc` | string | `inbound.purchase_order_lines` | `sloc` | |

---

## 28. GET /inbound/transfer-orders — List Transfer Orders

Response: `items[]` array.

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `id` | string | `inbound.transfer_orders` | `transfer_order_id` | |
| `transferNumber` | string | `inbound.transfer_orders` | `transfer_number` | |
| `source` | string | `config.store_locations` | `store_name` | JOIN on `transfer_orders.source_store_id` |
| `sourceStoreId` | string | `inbound.transfer_orders` | `source_store_id` | |
| `dest` | string | `config.store_locations` | `store_name` | JOIN on `transfer_orders.dest_store_id` |
| `destStoreId` | string | `inbound.transfer_orders` | `dest_store_id` | |
| `lines` | number | `inbound.transfer_order_lines` | COUNT(`to_line_id`) | |
| `status` | string | `inbound.transfer_orders` | `status` | Enum: `Created`, `PickConfirmed`, `InTransit`, `Received`, `Completed` |
| `tracking` | string \| null | `inbound.transfer_orders` | `tracking_id` | Set when TMS dispatch registered |
| `createdAt` | timestamp | `inbound.transfer_orders` | `created_at` | |
| `updatedAt` | timestamp | `inbound.transfer_orders` | `updated_at` | |

---

## 29. GET /inbound/transfer-orders/{id} — Get Transfer Order

Root fields identical to list item. Additional `lines[]`:

### `lines[]` array

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `toLineId` | string | `inbound.transfer_order_lines` | `to_line_id` | |
| `sku` | string | `inbound.transfer_order_lines` | `sku` | |
| `productName` | string | External product catalog | — | Resolved by SKU |
| `requestedQty` | number | `inbound.transfer_order_lines` | `requested_qty` | |
| `transferredQty` | number | `inbound.transfer_order_lines` | `transferred_qty` | Set by `TransferPickConfirmed` webhook |
| `confirmedAt` | timestamp | `inbound.transfer_order_lines` | `confirmed_at` | Set by `TransferPickConfirmed` webhook |

---

## 30. POST /inbound/transfer-orders — Create Transfer Order

**Trigger:** Operator creates a transfer order between stores/DCs.

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `sourceStoreId` | `inbound.transfer_orders` | `source_store_id` | INSERT |
| `destStoreId` | `inbound.transfer_orders` | `dest_store_id` | INSERT |
| `lines[].sku` | `inbound.transfer_order_lines` | `sku` | INSERT per line |
| `lines[].requestedQty` | `inbound.transfer_order_lines` | `requested_qty` | INSERT |
| *(derived)* | `inbound.transfer_orders` | `status = 'Created'`, `transfer_number`, `created_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'TransferOrderCreatedEvent'`, `event_payload` | INSERT (notifies source store WMS to pick) |

---

## 31. GET /inbound/transfer-orders/{id}/confirmations

Response: `transferOrderId` + `confirmations[]` array.

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `type` | string | `orders.order_webhook_logs` | `event_type` | `TransferPickConfirmed` or `TransferReceived` |
| `confirmedAt` | timestamp | `orders.order_webhook_logs` | `received_at` | |
| `confirmedBy` | string | `orders.order_webhook_logs` | `source_system` | Always `WMS` |
| `tracking` | string | `inbound.transfer_orders` | `tracking_id` | |

---

## 32. GET /stock/{sku}/ledger — Get Stock Ledger

OMS does not own stock counts; this endpoint aggregates stock-movement events OMS recorded.

### Root fields

| Response Field | Type | Source | Notes |
|---|---|---|---|
| `sku` | string | Query path parameter | Filter key |
| `skuName` | string | External product catalog | Resolved by SKU |
| `unitPrice` | number | External product catalog or `orders.order_lines.original_unit_price` | Latest known price |
| `currency` | string | External product catalog | |

### `locations[]` array

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `storeId` | string | `config.store_locations` | `store_id` | Grouped from event sources |
| `storeName` | string | `config.store_locations` | `store_name` | |
| `balance` | number | Computed | SUM(`qtyChange`) across all events for this location | |

### `locations[].events[]` array

Each event row is sourced from a different table depending on `event` type:

| `event` value | Source table | Key columns |
|---|---|---|
| `PurchaseOrderPutAwayConfirmed` | `inbound.purchase_order_lines` | `put_away_at`, `ordered_qty` |
| `TransferPickConfirmed` (out) | `inbound.transfer_order_lines` | `confirmed_at`, `transferred_qty` |
| `TransferReceived` (in) | `inbound.transfer_orders` | `updated_at` when status→`Received`; qty from `transfer_order_lines.transferred_qty` |
| `PickConfirmed` | `orders.order_status_history` | `changed_at` where `to_status = 'PickConfirmed'`; qty from `order_lines.picked_amount` |

| Response Field | Type | `PurchaseOrderPutAwayConfirmed` | `TransferPickConfirmed` / `TransferReceived` | `PickConfirmed` |
|---|---|---|---|---|
| `id` | number | Row sequence | Row sequence | Row sequence |
| `occurredAt` | timestamp | `purchase_order_lines.put_away_at` | `transfer_order_lines.confirmed_at` | `order_status_history.changed_at` |
| `time` | string | HH:MM | HH:MM | HH:MM |
| `dir` | string | `'in'` | `'out'` (pick) / `'in'` (receive) | `'out'` |
| `ref` | string | `purchase_orders.po_number` | `transfer_orders.transfer_number` | `orders.order_number` |
| `refType` | string | `'PurchaseOrder'` | `'TransferOrder'` | `'Order'` |
| `event` | string | `'PurchaseOrderPutAwayConfirmed'` | `'TransferPickConfirmed'` / `'TransferReceived'` | `'PickConfirmed'` |
| `qtyChange` | number | `+purchase_order_lines.received_qty` | `±transfer_order_lines.transferred_qty` | `−order_lines.picked_amount` |
| `balance` | number | Running sum | Running sum | Running sum |
| `detail` | string | `order_webhook_logs.detail` | `order_webhook_logs.detail` | `order_status_history.detail` |

---

## 33. POST /webhooks/wms/booking-confirmed

**Trigger:** WMS confirms it has reserved stock and can fulfil the order (UC2).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status` → `'BookingConfirmed'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `from_status`, `to_status = 'BookingConfirmed'`, `changed_at` | INSERT |
| `orderId` | `orders.order_webhook_logs` | `source_system = 'WMS'`, `event_type = 'BookingConfirmed'`, `detail`, `received_at` | INSERT |
| `wmsBookingRef` | `orders.order_webhook_logs` | Part of `detail` | |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderBookedEvent'`, `event_payload` | INSERT |

---

## 34. POST /webhooks/wms/pick-started

**Trigger:** WMS picker begins collecting items from shelves (UC3).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status` → `'PickStarted'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `from_status`, `to_status = 'PickStarted'`, `changed_at`, `detail` | INSERT |
| `pickerId` | `orders.order_webhook_logs` | Part of `detail` | |
| `startedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PickStarted'` | INSERT |

---

## 35. POST /webhooks/wms/pick-confirmed

**Trigger:** WMS confirms actual picked quantities per line (UC4).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status` → `'PickConfirmed'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `from_status`, `to_status = 'PickConfirmed'`, `changed_at`, `detail` | INSERT |
| `orderId` | `orders.order_webhook_logs` | `source_system = 'WMS'`, `event_type = 'PickConfirmed'`, `detail`, `received_at` | INSERT |
| `lines[].orderLineId` | `orders.order_lines` | `picked_amount` | UPDATE per line |
| `lines[].substituted = true` | `orders.order_line_substitutions` | `order_line_id`, `substitute_sku`, etc. | INSERT if substituted |
| `pickedAt` | `orders.order_webhook_logs` | `received_at` | |
| *(derived)* | `orders.order_outbox` | `event_type = 'PickConfirmedEvent'` | INSERT (triggers POS recalculation) |

---

## 36. POST /webhooks/wms/packed

**Trigger:** WMS confirms order is packed into one or more packages (UC4).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status` → `'Packed'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `from_status`, `to_status = 'Packed'`, `changed_at` | INSERT |
| `orderId` | `orders.order_webhook_logs` | `source_system = 'WMS'`, `event_type = 'Packed'`, `received_at` | INSERT |
| `packages[].trackingId` | `orders.order_packages` | `tracking_id` | INSERT per package |
| `packages[].vehicleType` | `orders.order_packages` | `vehicle_type` | INSERT |
| `packages[].weight` | `orders.order_packages` | `package_weight` | INSERT |
| `packages[].lineIds[]` | `orders.order_package_lines` | `package_id`, `order_line_id` | INSERT per line per package |
| `packedAt` | `orders.order_packages` | `created_at` | |
| *(derived)* | `orders.order_outbox` | `event_type = 'PackedEvent'` | INSERT (notifies TMS to dispatch) |

---

## 37. POST /webhooks/wms/substitution-offered

**Trigger:** WMS offers an alternative SKU when original line cannot be fully fulfilled (UC5).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `substitution_flag = true`, `pos_recalc_pending = true`, `updated_at` | UPDATE |
| `orderLineId` | `orders.order_line_substitutions` | `order_line_id` | INSERT |
| `substituteSku` | `orders.order_line_substitutions` | `substitute_sku` | INSERT |
| `substituteProductName` | `orders.order_line_substitutions` | `substitute_product_name` | INSERT |
| `substituteUnitPrice` | `orders.order_line_substitutions` | `substitute_unit_price` | INSERT |
| `substitutedAmount` | `orders.order_line_substitutions` | `substituted_amount` | INSERT |
| `offeredAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'SubstitutionOffered'` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'SubstitutionOfferedEvent'` | INSERT (notifies customer) |

---

## 38. POST /webhooks/wms/put-away-confirmed

**Trigger:** WMS confirms returned goods are shelved after a return receipt (UC14).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `returnId` | `returns.returns` | `status` → `'PutAway'`, `put_away_at` | UPDATE |
| `items[].sku` | `returns.return_items` | `sku` | Matched by `return_id + sku` |
| `items[].condition` | `returns.return_items` | `condition`, `put_away_status = 'PutAway'` | UPDATE |
| `items[].sloc` | `returns.return_items` | `assigned_sloc`, `put_away_at` | UPDATE |
| `items[].sku + sloc + condition` | `returns.return_put_away_logs` | `return_id`, `return_item_id`, `sku`, `assigned_sloc`, `condition`, `quantity`, `performed_at` | INSERT |
| `putAwayAt` | `returns.returns` | `put_away_at` | |
| *(derived)* | `returns.return_refunds` | `return_id`, `refund_amount`, `refund_method`, `status = 'Pending'` | INSERT (atomic with put-away) |
| *(derived)* | `payment.credit_notes` | `order_id`, `invoice_id`, `amount`, `reason = 'Return'`, `status = 'Issued'` | INSERT |

---

## 39. POST /webhooks/wms/goods-receipt-confirmed

**Trigger:** WMS confirms physical goods arrived at dock against a Purchase Order (UC21).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `purchaseOrderId` | `inbound.purchase_orders` | `status` → `'FullyReceived'` or `'PartiallyReceived'`, `goods_receive_no`, `updated_at` | UPDATE |
| `goodsReceiveNo` | `inbound.purchase_orders` | `goods_receive_no` | |
| `lines[].sku` | `inbound.purchase_order_lines` | Matched by `purchase_order_id + sku` | |
| `lines[].receivedQty` | `inbound.purchase_order_lines` | `received_qty`, `received_at` | UPDATE per line |
| `receivedAt` | `inbound.purchase_order_lines` | `received_at` | |
| *(derived)* | `orders.order_outbox` | `event_type = 'GoodsReceiptConfirmedEvent'`, `event_payload` | INSERT |

> **Note:** `order_webhook_logs` is scoped to `orders.order_id`. PO webhooks do not carry an `order_id` — a separate `inbound_webhook_logs` table may be needed for PO-level audit.

---

## 40. POST /webhooks/wms/purchase-order-put-away-confirmed

**Trigger:** WMS confirms inbound goods are shelved; closes the PO (UC21).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `purchaseOrderId` | `inbound.purchase_orders` | `status` → `'Closed'`, `updated_at` | UPDATE |
| `items[].sku` | `inbound.purchase_order_lines` | Matched by `purchase_order_id + sku` | |
| `items[].condition` | `inbound.purchase_order_lines` | `condition` | UPDATE |
| `items[].sloc` | `inbound.purchase_order_lines` | `sloc` | UPDATE |
| `items[].qty` | `inbound.purchase_order_lines` | Validates against `received_qty` | — |
| `putAwayAt` | `inbound.purchase_order_lines` | `put_away_at` | UPDATE per line |
| *(derived)* | `orders.order_outbox` | `event_type = 'PurchaseOrderClosedEvent'`, `event_payload` | INSERT (signals stock available to WMS) |

---

## 41. POST /webhooks/wms/transfer-pick-confirmed

**Trigger:** WMS at source store confirms items packed for transfer (UC22).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `transferOrderId` | `inbound.transfer_orders` | `status` → `'PickConfirmed'`, `updated_at` | UPDATE |
| `lines[].sku` | `inbound.transfer_order_lines` | Matched by `transfer_order_id + sku` | |
| `lines[].transferredQty` | `inbound.transfer_order_lines` | `transferred_qty` | UPDATE per line |
| `confirmedAt` | `inbound.transfer_order_lines` | `confirmed_at` | UPDATE per line |
| *(derived)* | `orders.order_outbox` | `event_type = 'TransferPickConfirmedEvent'` | INSERT (notifies TMS to dispatch) |

---

## 42. POST /webhooks/wms/transfer-received

**Trigger:** WMS at destination store confirms stock arrived and put away (UC22).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `transferOrderId` | `inbound.transfer_orders` | `status` → `'Completed'`, `updated_at` | UPDATE |
| `receivedAt` | `inbound.transfer_orders` | `updated_at` | UPDATE |
| *(derived)* | `orders.order_outbox` | `event_type = 'TransferReceivedEvent'`, `event_payload` | INSERT |

---

## 43. POST /webhooks/wms/damaged-goods-received

**Trigger:** WMS checks in a damaged package returned by TMS driver (UC23).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `pre_hold_status` ← current `status`; `status` → `'OnHold'`; `hold_reason = 'PackageDamaged'`; `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'OnHold'`, `changed_at`, `detail` | INSERT |
| `orderId` | `orders.order_webhook_logs` | `source_system = 'WMS'`, `event_type = 'DamagedGoodsReceived'`, `received_at` | INSERT |
| `trackingId` | `inbound.damaged_goods_receipts` | `tracking_id` | INSERT |
| `orderId` | `inbound.damaged_goods_receipts` | `order_id` | INSERT |
| `receivedAt` | `inbound.damaged_goods_receipts` | `received_at`, `status = 'Received'` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'DamagedGoodsReceivedEvent'` | INSERT |

---

## 44. POST /webhooks/wms/damaged-goods-put-away

**Trigger:** WMS confirms damaged goods inspected and shelved/disposed (UC23).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `damagedReceiptId` | `inbound.damaged_goods_receipts` | `status → 'PutAway'`, `put_away_at`, `updated_at` | UPDATE |
| `items[].sku` | `inbound.damaged_goods_items` | `sku` | INSERT per item |
| `items[].condition` | `inbound.damaged_goods_items` | `condition` | INSERT |
| `items[].sloc` | `inbound.damaged_goods_items` | `sloc` | INSERT |
| `items[].qty` | `inbound.damaged_goods_items` | `qty` | INSERT |
| `putAwayAt` | `inbound.damaged_goods_items` | `confirmed_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'DamagedGoodsPutAwayEvent'` | INSERT |

---

## 45. POST /webhooks/tms/package-dispatched

**Trigger:** TMS driver collected the package; goods out for delivery (UC7).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `trackingId` | `orders.order_packages` | `status` → `'OutForDelivery'`, `updated_at` | UPDATE — lookup by `tracking_id` |
| *(derived from package)* | `orders.orders` | `status` → `'OutForDelivery'`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_status_history` | `from_status`, `to_status = 'OutForDelivery'`, `changed_at` | INSERT |
| `dispatchedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PackageDispatched'` | INSERT |

---

## 46. POST /webhooks/tms/package-delivered

**Trigger:** TMS confirms package delivered to customer (UC8).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `trackingId` | `orders.order_packages` | `status` → `'Delivered'`, `updated_at` | UPDATE |
| *(derived from package)* | `orders.orders` | `status` → `'Delivered'`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_status_history` | `to_status = 'Delivered'`, `changed_at`, `detail` | INSERT |
| `deliveredAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PackageDelivered'` | INSERT |
| `proofOfDelivery` | `orders.order_webhook_logs` | `detail` | Stored as audit detail |
| *(derived)* | `orders.order_outbox` | `event_type = 'DeliveredEvent'` | INSERT (→ POS for invoice) |
| *(derived)* | `payment.invoices` | `order_id`, `invoice_number`, `total_amount`, `status = 'Generated'`, `generated_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'InvoiceGeneratedEvent'` | INSERT (→ POS for payment link) |

---

## 47. POST /webhooks/tms/package-damage-reported

**Trigger:** TMS driver reports a package is damaged before or during delivery attempt (UC20).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `trackingId` | `orders.order_packages` | `status` → `'Damaged'`, `updated_at` | UPDATE — lookup by `tracking_id` |
| *(derived from package)* | `orders.orders` | `pre_hold_status` ← current `status`; `status` → `'OnHold'`; `hold_reason = 'PackageDamaged'`; `updated_at` | UPDATE |
| *(derived)* | `orders.order_status_history` | `to_status = 'OnHold'`, `changed_at`, `detail` = reason | INSERT |
| `reportedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PackageDamageReported'`; `detail` = driverNote | INSERT |
| `reason`, `driverNote` | `orders.order_webhook_logs` | `detail` | |
| *(derived)* | `orders.order_outbox` | `event_type = 'PackageDamagedEvent'` | INSERT (triggers return-to-warehouse workflow) |

---

## 48. POST /webhooks/pos/pos-collection-ready

**Trigger:** POS notifies OMS that a Click & Collect order is packed and ready for pickup (UC10).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status` → `'ReadyForCollection'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'ReadyForCollection'`, `changed_at` | INSERT |
| `notifiedAt` | `orders.order_webhook_logs` | `received_at`; `source_system = 'POS'`; `event_type = 'PosCollectionReady'` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'ReadyForCollectionEvent'` | INSERT (notifies customer) |

---

## 49. POST /webhooks/pos/collected

**Trigger:** POS confirms customer collected the Click & Collect order at the store (UC11).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status` → `'Collected'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'Collected'`, `changed_at` | INSERT |
| `collectedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'Collected'` | INSERT |
| `collectedBy` | `orders.order_webhook_logs` | `detail` | |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderCollectedEvent'` | INSERT |
| *(derived)* | `payment.invoices` | `order_id`, `invoice_number`, `total_amount`, `status = 'Generated'` | INSERT (triggered for Click & Collect) |

---

## 50. POST /webhooks/pos/invoiced

**Trigger:** POS confirms a fiscal invoice has been issued to the customer (UC12).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status` → `'Invoiced'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'Invoiced'`, `changed_at` | INSERT |
| `invoiceNumber` | `payment.invoices` | `invoice_number`, `status = 'Issued'` | UPDATE (matches existing Generated invoice) |
| `totalAmount` | `payment.invoices` | `total_amount` | UPDATE |
| `invoicedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'Invoiced'` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderInvoicedEvent'` | INSERT |

---

## 51. POST /webhooks/pos/payment-confirmed

**Trigger:** POS confirms the customer has paid (UC13).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status` → `'Paid'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'Paid'`, `changed_at` | INSERT |
| `paidAmount` | `payment.payment_transactions` | `amount` | INSERT new transaction |
| `paymentMethod` | `payment.payment_transactions` | `payment_method` | INSERT |
| `currency` | `payment.payment_transactions` | `currency` | INSERT |
| `paidAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PaymentConfirmed'` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderPaidEvent'` | INSERT |

---

## 52. POST /webhooks/pos/recalculation-result

**Trigger:** POS returns the final adjusted total after applying promotions to actual picked quantities (UC15).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `pos_recalc_pending` → `false`, `updated_at` | UPDATE |
| `adjustedAmount` | `payment.order_line_amounts` | Stored per recalc round | INSERT per line |
| `originalAmount` | `payment.order_line_amounts` | Validated against prior amounts | — |
| `currency` | `payment.order_line_amounts` | `currency` | |
| `promotionsApplied[].promoCode` | `payment.order_promotions` | `promo_code` | INSERT per promo |
| `promotionsApplied[].discountAmount` | `payment.order_promotions` | `discount_amount` | |
| `promotionsApplied[].description` | `payment.order_promotions` | `promo_name` | |
| `recalculatedAt` | `payment.order_line_amounts` | `recalculated_at`, `created_at` | |
| `recalculatedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'RecalculationResult'` | INSERT |
| *(derived)* | `payment.order_line_amounts` | `recalc_round` = prior MAX + 1; `trigger_event = 'PickConfirmed'` | INSERT per order line |

---

## 53. POST /webhooks/pos/pos-recalc-completed

**Trigger:** POS confirms the full recalculation cycle is closed and the order may proceed to packing (UC15).

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `pos_recalc_pending` → `false` (confirmation; already false after `recalculation-result`), `updated_at` | UPDATE |
| `completedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PosRecalcCompleted'` | INSERT |
| `finalAmount` | `orders.order_webhook_logs` | `detail` (audit record only) | |
| *(derived)* | `orders.order_outbox` | `event_type = 'RecalcCompletedEvent'` | INSERT (unblocks packing workflow if awaiting) |

---

## Cross-reference: which tables each read API touches

| Table | List Orders | Get Order | Timeline | Lines | Packages | Webhooks | Substitutions | Slot | List POs | Get PO | List TOs | Get TO | Stock Ledger |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| `orders.orders` | R | R | R | | | | | R | | | | | |
| `orders.order_lines` | R(ct) | R | | R | | | R | | | | | | R |
| `orders.order_addresses` | R | R | R | | | | | | | | | | |
| `orders.order_customers` | | R | | | | | | | | | | | |
| `orders.order_packages` | | R | | | R | | | | | | | | |
| `orders.order_package_lines` | | R | | | R | | | | | | | | |
| `orders.delivery_slots` | R | R | | | | | | R | | | | | |
| `orders.order_holds` | | | | | | | | | | | | | |
| `orders.order_status_history` | | | R | | | | | | | | | | R |
| `orders.order_webhook_logs` | | | R | | | R | | | | | | | R |
| `orders.order_outbox` | | | R | | | | | | | | | | |
| `orders.order_line_substitutions` | | R | | R | | | R | | | | | | |
| `payment.order_line_amounts` | R | R | | R | | | | | | | | | |
| `payment.invoices` | | | | | | | | | | | | | |
| `payment.credit_notes` | | | | | | | | | | | | | |
| `config.store_locations` | R | R | R | | | | | R | R | R | R | R | R |
| `inbound.purchase_orders` | | | | | | | | | R | R | | | |
| `inbound.purchase_order_lines` | | | | | | | | | R | R | | | R |
| `inbound.transfer_orders` | | | | | | | | | | | R | R | R |
| `inbound.transfer_order_lines` | | | | | | | | | | | R | R | R |
| `returns.returns` | | | | | | | | | | | | | |
| `returns.return_items` | | | | | | | | | | | | | |
| `returns.return_refunds` | | | | | | | | | | | | | |
