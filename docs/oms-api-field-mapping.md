# Sprint Connect OMS — API Field Mapping

**Version:** 2.0

Maps every API request and response field to its source or target database table and column. Use this as the contract between the API layer and the database.

---

## Contents

1. [GET /orders — List Orders](#1-get-orders--list-orders)
2. [GET /orders/{id} — Get Order](#2-get-ordersid--get-order)
3. [GET /orders/{id}/timeline — Order Timeline](#3-get-ordersidtimeline--order-timeline)
4. [GET /orders/{id}/lines — List Order Lines](#4-get-ordersidlines)
5. [GET /orders/{id}/packages — List Packages](#5-get-ordersidpackages)
6. [GET /orders/{id}/webhooks — Webhook Events](#6-get-ordersidwebhooks)
7. [GET /orders/{id}/substitutions — Substitutions](#7-get-ordersidsubstitutions)
8. [GET /orders/{id}/delivery-slot — Delivery Slot](#8-get-ordersiddelivery-slot)
9. [POST /orders — Create Order](#9-post-orders--create-order)
10. [PATCH /orders/{id}/hold — Place Hold](#10-patch-ordersidhold)
11. [PATCH /orders/{id}/release-hold — Release Hold](#11-patch-ordersidrelease-hold)
12. [PATCH /orders/{id}/cancel — Cancel Order](#12-patch-ordersidcancel)
13. [POST /orders/{id}/substitutions/{subId}/approve](#13-post-ordersidsubstitutionssubidapprove)
14. [POST /orders/{id}/substitutions/{subId}/reject](#14-post-ordersidsubstitutionssubidreject)
15. [POST /orders/{id}/recalculate — Trigger Recalculation](#15-post-ordersidrecalculate)
16. [PATCH /orders/{id}/delivery-slot — Update Slot](#16-patch-ordersiddelivery-slot)
17. [POST /returns — Create Return](#17-post-returns--create-return)
18. [GET /returns — List Returns](#18-get-returns--list-returns)
19. [GET /returns/{id} — Get Return](#19-get-returnsid--get-return)
20. [GET /returns/{id}/items — Return Items](#20-get-returnsiditemss)
21. [PATCH /returns/{id}/cancel — Cancel Return](#21-patch-returnsidcancel)
22. [GET /returns/{id}/refund — Get Refund](#22-get-returnsidrefund)
23. [GET /inbound/purchase-orders — List POs](#23-get-inboundpurchase-orders--list-purchase-orders)
24. [GET /inbound/purchase-orders/{id} — Get PO](#24-get-inboundpurchase-ordersid--get-purchase-order)
25. [POST /inbound/purchase-orders — Create PO](#25-post-inboundpurchase-orders--create-purchase-order)
26. [GET /inbound/purchase-orders/{id}/goods-receipts](#26-get-inboundpurchase-ordersidgoods-receipts)
27. [GET /inbound/transfer-orders — List TOs](#27-get-inboundtransfer-orders--list-transfer-orders)
28. [GET /inbound/transfer-orders/{id} — Get TO](#28-get-inboundtransfer-ordersid--get-transfer-order)
29. [POST /inbound/transfer-orders — Create TO](#29-post-inboundtransfer-orders--create-transfer-order)
30. [GET /inbound/transfer-orders/{id}/confirmations](#30-get-inboundtransfer-ordersidconfirmations)
31. [GET /stock/{sku}/ledger — Stock Ledger](#31-get-stockskuledger--stock-ledger)
32. [Webhooks — WMS: booking-confirmed](#32-post-webhookswmsbooking-confirmed)
33. [Webhooks — WMS: pick-started](#33-post-webhookswmspick-started)
34. [Webhooks — WMS: pick-confirmed](#34-post-webhookswmspick-confirmed)
35. [Webhooks — WMS: packed](#35-post-webhookswmspacked)
36. [Webhooks — WMS: substitution-offered](#36-post-webhookswmssubstitution-offered)
37. [Webhooks — WMS: put-away-confirmed (Returns)](#37-post-webhookswmsput-away-confirmed)
38. [Webhooks — WMS: goods-receipt-confirmed](#38-post-webhookswmsgoods-receipt-confirmed)
39. [Webhooks — WMS: purchase-order-put-away-confirmed](#39-post-webhookswmspurchase-order-put-away-confirmed)
40. [Webhooks — WMS: transfer-pick-confirmed](#40-post-webhookswmstransfer-pick-confirmed)
41. [Webhooks — WMS: transfer-received](#41-post-webhookswmstransfer-received)
42. [Webhooks — WMS: damaged-goods-received](#42-post-webhookswmsdamaged-goods-received)
43. [Webhooks — WMS: damaged-goods-put-away](#43-post-webhookswmsdamaged-goods-put-away)
44. [Webhooks — TMS: package-dispatched](#44-post-webhookstmspackage-dispatched)
45. [Webhooks — TMS: package-delivered](#45-post-webhookstmspackage-delivered)
46. [Webhooks — TMS: package-damage-reported](#46-post-webhookstmspackage-damage-reported)
47. [Webhooks — POS: pos-collection-ready](#47-post-webhookspospos-collection-ready)
48. [Webhooks — POS: collected](#48-post-webhooksposcollected)
49. [Webhooks — POS: invoiced](#49-post-webhooksposinvoiced)
50. [Webhooks — POS: payment-confirmed](#50-post-webhookspospayment-confirmed)
51. [Webhooks — POS: recalculation-result](#51-post-webhooksposrecalculation-result)
52. [Webhooks — POS: pos-recalc-completed](#52-post-webhookspospos-recalc-completed)
53. [Cross-reference: which tables each read API touches](#53-cross-reference-read-api-table-usage)
54. [POST /webhooks/sts/abb-tax-invoice](#54-post-webhooksstsabb-tax-invoice)
55. [POST /webhooks/sts/credit-note](#55-post-webhooksstscredit-note)

---

## 1. GET /orders — List Orders

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `id` | string | `orders.orders` | `order_id` | |
| `orderNumber` | string | `orders.orders` | `order_number` | |
| `status` | string | `orders.orders` | `status` | |
| `fulfillmentType` | string | `orders.orders` | `fulfillment_type` | |
| `paymentMethod` | string | `orders.orders` | `payment_method` | |
| `store` | string | `config.store_locations` | `store_name` | JOIN on `orders.store_id` |
| `storeId` | string | `orders.orders` | `store_id` | |
| `lineCount` | number | `orders.order_lines` | COUNT(`order_line_id`) | Grouped by `order_id` |
| `totalAmount` | number | `payment.order_line_amounts` | SUM of latest `recalculated_unit_price × picked_amount` | Latest `recalc_round` per line |
| `currency` | string | `orders.orders` | `currency` | |
| `customer.name` | string | `orders.order_addresses` | `first_name` + `last_name` | `address_type = 'Delivery'` |
| `customer.phone` | string | `orders.order_addresses` | `mobile_phone` | |
| `deliverySlot.scheduledStart` | timestamp | `orders.delivery_slots` | `scheduled_start` | |
| `deliverySlot.scheduledEnd` | timestamp | `orders.delivery_slots` | `scheduled_end` | |
| `createdAt` | timestamp | `orders.orders` | `created_at` | |
| `updatedAt` | timestamp | `orders.orders` | `updated_at` | |
| *(filter: `channel`)* | — | `orders.orders` | `channel_type` | Optional query param; allowed values: `Gateway`, `Marketplace`, `Kiosk`, `POSTerminal`, `BulkImport`, `Web`, `App`, `POS`, `CallCenter` |

---

## 2. GET /orders/{id} — Get Order

### Root fields

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `id` | string | `orders.orders` | `order_id` | |
| `orderNumber` | string | `orders.orders` | `order_number` | |
| `status` | string | `orders.orders` | `status` | |
| `fulfillmentType` | string | `orders.orders` | `fulfillment_type` | |
| `paymentMethod` | string | `orders.orders` | `payment_method` | |
| `posRecalcPending` | boolean | `orders.orders` | `pos_recalc_pending` | |
| `substitutionFlag` | boolean | `orders.orders` | `substitution_flag` | |
| `store` | string | `config.store_locations` | `store_name` | JOIN on `orders.store_id` |
| `storeId` | string | `orders.orders` | `store_id` | |
| `totalAmount` | number | `payment.order_line_amounts` | Computed | SUM of latest recalculated amounts |
| `currency` | string | `orders.orders` | `currency` | |
| `createdAt` | timestamp | `orders.orders` | `created_at` | |
| `updatedAt` | timestamp | `orders.orders` | `updated_at` | |

### `customer` object

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `name` | string | `orders.order_addresses` | `first_name` + `last_name` | `address_type = 'Delivery'` |
| `phone` | string | `orders.order_addresses` | `mobile_phone` | |
| `email` | string | `orders.order_addresses` | `email` | |

### `deliveryAddress` object

| Response Field | Type | Table | Column |
|---|---|---|---|
| `address1` | string | `orders.order_addresses` | `address1` |
| `subdistrict` | string | `orders.order_addresses` | `subdistrict` |
| `district` | string | `orders.order_addresses` | `district` |
| `province` | string | `orders.order_addresses` | `province` |
| `postalCode` | string | `orders.order_addresses` | `postal_code` |

### `deliverySlot` object

| Response Field | Type | Table | Column |
|---|---|---|---|
| `slotId` | string | `orders.delivery_slots` | `slot_id` |
| `scheduledStart` | timestamp | `orders.delivery_slots` | `scheduled_start` |
| `scheduledEnd` | timestamp | `orders.delivery_slots` | `scheduled_end` |

### `lines[]` array

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `orderLineId` | string | `orders.order_lines` | `order_line_id` | |
| `sku` | string | `orders.order_lines` | `sku` | |
| `productName` | string | `orders.order_lines` | `product_name` | |
| `barcode` | string | `orders.order_lines` | `barcode` | |
| `unitOfMeasure` | string | `orders.order_lines` | `unit_of_measure` | |
| `requestedQty` | number | `orders.order_lines` | `requested_amount` | |
| `pickedQty` | number | `orders.order_lines` | `picked_amount` | |
| `unitPrice` | number | `orders.order_lines` | `original_unit_price` | |
| `recalculatedUnitPrice` | number | `payment.order_line_amounts` | `recalculated_unit_price` | Latest `recalc_round` for this line |
| `currency` | string | `orders.order_lines` | `currency` | |
| `status` | string | `orders.order_lines` | `status` | `Active` or `Voided` |

### `packages[]` array

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `packageId` | string | `orders.order_packages` | `package_id` | |
| `trackingId` | string | `orders.order_packages` | `tracking_id` | |
| `vehicleType` | string | `orders.order_packages` | `vehicle_type` | |
| `weight` | number | `orders.order_packages` | `package_weight` | |
| `status` | string | `orders.order_packages` | `status` | |
| `lineIds[]` | string[] | `orders.order_package_lines` | `order_line_id` | All lines in this package |

---

## 3. GET /orders/{id}/timeline — Order Timeline

### `order` summary object

| Response Field | Type | Table | Column |
|---|---|---|---|
| `orderNumber` | string | `orders.orders` | `order_number` |
| `status` | string | `orders.orders` | `status` |
| `fulfillmentType` | string | `orders.orders` | `fulfillment_type` |
| `store` | string | `config.store_locations` | `store_name` |

### `events[]` array

Each event row is built from one of three source tables:

| `type` value | Primary source table |
|---|---|
| `domain` | `orders.order_status_history` |
| `webhook` | `orders.order_webhook_logs` |
| `outbox` | `orders.order_outbox` |
| `bridge` | Derived marker — synthesized, no stored row |

| Response Field | Type | `domain` source | `webhook` source | `outbox` source |
|---|---|---|---|---|
| `occurredAt` | timestamp | `order_status_history.changed_at` | `order_webhook_logs.received_at` | `order_outbox.created_at` |
| `time` | string | HH:MM from `changed_at` | HH:MM from `received_at` | HH:MM from `created_at` |
| `phase` | string | Annotated at query time | Annotated at query time | Annotated at query time |
| `type` | string | `'domain'` (fixed) | `'webhook'` (fixed) | `'outbox'` (fixed) |
| `system` | string | `'OMS'` (fixed) | `order_webhook_logs.source_system` | Derived from `order_outbox.event_type` |
| `event` | string | `order_status_history.to_status` | `order_webhook_logs.event_type` | `order_outbox.event_type` |
| `detail` | string | `order_status_history.detail` | `order_webhook_logs.detail` | Derived from `order_outbox.event_payload` |
| `outStatus` | string \| null | `null` | `null` | `order_outbox.status` |

### `summary` object

| Response Field | Type | Derived from |
|---|---|---|
| `totalEvents` | number | COUNT of all events across all three tables |
| `inboundPhaseEvents` | number | COUNT where `phase = 'inbound'` |
| `outboundPhaseEvents` | number | COUNT where `phase = 'outbound'` |
| `orderToDeliveredMinutes` | number | `order_status_history(Delivered).changed_at − order_status_history(Pending).changed_at` |
| `totalEndToEndMinutes` | number | `last_event.occurredAt − first_event.occurredAt` |

---

## 4. GET /orders/{id}/lines

Response: `orderId` + `lines[]` array. Fields identical to the `lines[]` array in [Section 2](#2-get-ordersid--get-order).

---

## 5. GET /orders/{id}/packages

Response: `orderId` + `packages[]` array. Fields identical to `packages[]` in [Section 2](#2-get-ordersid--get-order).

---

## 6. GET /orders/{id}/webhooks

Response: `orderId` + `webhooks[]` array.

| Response Field | Type | Table | Column |
|---|---|---|---|
| `webhookLogId` | string | `orders.order_webhook_logs` | `webhook_log_id` |
| `sourceSystem` | string | `orders.order_webhook_logs` | `source_system` |
| `eventType` | string | `orders.order_webhook_logs` | `event_type` |
| `detail` | string | `orders.order_webhook_logs` | `detail` |
| `receivedAt` | timestamp | `orders.order_webhook_logs` | `received_at` |

---

## 7. GET /orders/{id}/substitutions

Response: `orderId` + `substitutions[]` array.

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `substitutionId` | string | `orders.order_line_substitutions` | `substitution_id` | |
| `orderLineId` | string | `orders.order_line_substitutions` | `order_line_id` | |
| `originalSku` | string | `orders.order_lines` | `sku` | JOIN on `order_line_id` |
| `originalProductName` | string | `orders.order_lines` | `product_name` | |
| `substituteSku` | string | `orders.order_line_substitutions` | `substitute_sku` | |
| `substituteProductName` | string | `orders.order_line_substitutions` | `substitute_product_name` | |
| `substituteUnitPrice` | number | `orders.order_line_substitutions` | `substitute_unit_price` | |
| `substitutedAmount` | number | `orders.order_line_substitutions` | `substituted_amount` | |
| `customerApproved` | boolean \| null | `orders.order_line_substitutions` | `customer_approved` | `null` = pending |
| `approvedAt` | timestamp \| null | `orders.order_line_substitutions` | `approved_at` | |
| `createdAt` | timestamp | `orders.order_line_substitutions` | `created_at` | |

---

## 8. GET /orders/{id}/delivery-slot

| Response Field | Type | Table | Column |
|---|---|---|---|
| `orderId` | string | `orders.orders` | `order_id` |
| `slotId` | string | `orders.delivery_slots` | `slot_id` |
| `scheduledStart` | timestamp | `orders.delivery_slots` | `scheduled_start` |
| `scheduledEnd` | timestamp | `orders.delivery_slots` | `scheduled_end` |
| `storeId` | string | `orders.delivery_slots` | `store_id` |

---

## 9. POST /orders — Create Order

All writes in a single DB transaction.

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `sourceOrderId` | `orders.orders` | `source_order_id` | INSERT |
| `channelType` | `orders.orders` | `channel_type` | INSERT |
| `businessUnit` | `orders.orders` | `business_unit` | INSERT |
| `storeId` | `orders.orders` | `store_id` | INSERT |
| `fulfillmentType` | `orders.orders` | `fulfillment_type` | INSERT |
| `paymentMethod` | `orders.orders` | `payment_method` | INSERT |
| `customer.name` | `orders.order_addresses` | `first_name`, `last_name` | INSERT — `address_type = 'Delivery'` |
| `customer.phone` | `orders.order_addresses` | `mobile_phone` | INSERT |
| `customer.email` | `orders.order_addresses` | `email` | INSERT |
| `deliveryAddress.address1` | `orders.order_addresses` | `address1` | INSERT |
| `deliveryAddress.subdistrict` | `orders.order_addresses` | `subdistrict` | INSERT |
| `deliveryAddress.district` | `orders.order_addresses` | `district` | INSERT |
| `deliveryAddress.province` | `orders.order_addresses` | `province` | INSERT |
| `deliveryAddress.postalCode` | `orders.order_addresses` | `postal_code` | INSERT |
| `deliverySlot.scheduledStart` | `orders.delivery_slots` | `scheduled_start` | INSERT |
| `deliverySlot.scheduledEnd` | `orders.delivery_slots` | `scheduled_end` | INSERT |
| `lines[].sku` | `orders.order_lines` | `sku` | INSERT per line |
| `lines[].productName` | `orders.order_lines` | `product_name` | INSERT |
| `lines[].barcode` | `orders.order_lines` | `barcode` | INSERT |
| `lines[].requestedQty` | `orders.order_lines` | `requested_amount` | INSERT |
| `lines[].unitPrice` | `orders.order_lines` | `original_unit_price` | INSERT |
| `lines[].unitOfMeasure` | `orders.order_lines` | `unit_of_measure` | INSERT |
| *(derived)* | `orders.orders` | `status = 'Pending'`, `order_number`, `created_at` | INSERT |
| *(derived)* | `orders.order_status_history` | `from_status = null`, `to_status = 'Pending'`, `changed_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderCreatedEvent'`, `status = 'Pending'` | INSERT |

---

## 10. PATCH /orders/{id}/hold

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `holdReason` | `orders.orders` | `hold_reason` | UPDATE |
| `heldBy` | `orders.orders` | `updated_by` | UPDATE |
| *(derived)* | `orders.orders` | `pre_hold_status` ← current `status` | UPDATE |
| *(derived)* | `orders.orders` | `status → 'OnHold'`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_status_history` | `from_status`, `to_status = 'OnHold'`, `changed_by`, `changed_at` | INSERT |
| *(derived)* | `orders.order_holds` | `order_id`, `hold_reason`, `held_at`, `held_by` | INSERT |

---

## 11. PATCH /orders/{id}/release-hold

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `releasedBy` | `orders.orders` | `updated_by` | UPDATE |
| *(derived)* | `orders.orders` | `status` ← `pre_hold_status` | UPDATE |
| *(derived)* | `orders.orders` | `pre_hold_status → null`, `hold_reason → null`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_status_history` | `from_status = 'OnHold'`, `to_status = pre_hold_status`, `changed_by`, `changed_at` | INSERT |
| *(derived)* | `orders.order_holds` | `released_at`, `released_by` | UPDATE — latest open hold record |

---

## 12. PATCH /orders/{id}/cancel

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `reason` | `orders.order_status_history` | `detail` | Part of INSERT |
| `cancelledBy` | `orders.orders` | `updated_by` | UPDATE |
| *(derived)* | `orders.orders` | `status → 'Cancelled'`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_status_history` | `from_status`, `to_status = 'Cancelled'`, `detail`, `changed_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderCancelledEvent'` | INSERT |

---

## 13. POST /orders/{id}/substitutions/{subId}/approve

| Field | Table Written | Column | Action |
|---|---|---|---|
| *(derived — subId)* | `orders.order_line_substitutions` | `customer_approved = true`, `approved_at` | UPDATE |
| *(derived)* | `orders.orders` | `updated_at` | UPDATE |
| *(derived — if posRecalcPending)* | `orders.order_outbox` | `event_type = 'SubstitutionApprovedEvent'` | INSERT |

---

## 14. POST /orders/{id}/substitutions/{subId}/reject

| Field | Table Written | Column | Action |
|---|---|---|---|
| *(derived — subId)* | `orders.order_line_substitutions` | `customer_approved = false`, `approved_at` | UPDATE |
| *(derived)* | `orders.order_lines` | `status → 'Voided'` for original line | UPDATE |
| *(derived)* | `orders.orders` | `updated_at` | UPDATE |
| *(derived)* | `orders.order_outbox` | `event_type = 'SubstitutionRejectedEvent'` | INSERT — triggers recalc |

---

## 15. POST /orders/{id}/recalculate

| Field | Table Written | Column | Action |
|---|---|---|---|
| *(derived)* | `orders.orders` | `pos_recalc_pending = true`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_outbox` | `event_type = 'RecalcRequestedEvent'` | INSERT |

---

## 16. PATCH /orders/{id}/delivery-slot

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `scheduledStart` | `orders.delivery_slots` | `scheduled_start` | UPDATE |
| `scheduledEnd` | `orders.delivery_slots` | `scheduled_end` | UPDATE |
| *(derived)* | `orders.delivery_slots` | `updated_at` | UPDATE |

---

## 17. POST /returns — Create Return

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `returns.returns` | `order_id` | INSERT |
| `returnReason` | `returns.returns` | `return_reason` | INSERT |
| `items[].orderLineId` | `returns.return_items` | `order_line_id` | INSERT per item |
| `items[].sku` | `returns.return_items` | `sku` | INSERT |
| `items[].quantity` | `returns.return_items` | `quantity` | INSERT |
| `items[].itemReason` | `returns.return_items` | `item_reason` | INSERT |
| `requestedBy` | `returns.returns` | `created_by` | INSERT |
| *(derived)* | `returns.returns` | `status = 'ReturnRequested'`, `return_order_number`, `requested_at`, `created_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'ReturnRequestedEvent'` | INSERT — triggers TMS pickup scheduling |

---

## 18. GET /returns — List Returns

| Response Field | Type | Table | Column |
|---|---|---|---|
| `id` | string | `returns.returns` | `return_id` |
| `returnOrderNumber` | string | `returns.returns` | `return_order_number` |
| `orderId` | string | `returns.returns` | `order_id` |
| `status` | string | `returns.returns` | `status` |
| `returnReason` | string | `returns.returns` | `return_reason` |
| `requestedAt` | timestamp | `returns.returns` | `requested_at` |
| `refundedAt` | timestamp \| null | `returns.returns` | `refunded_at` |
| `createdAt` | timestamp | `returns.returns` | `created_at` |
| `updatedAt` | timestamp | `returns.returns` | `updated_at` |

---

## 19. GET /returns/{id} — Get Return

Root fields identical to list above, plus:

| Response Field | Type | Table | Column |
|---|---|---|---|
| `invoiceId` | string \| null | `returns.returns` | `invoice_id` |
| `creditNoteId` | string \| null | `returns.returns` | `credit_note_id` |
| `goodsReceiveNo` | string \| null | `returns.returns` | `goods_receive_no` |
| `pickupScheduledAt` | timestamp \| null | `returns.returns` | `pickup_scheduled_at` |
| `pickedUpAt` | timestamp \| null | `returns.returns` | `picked_up_at` |
| `receivedAt` | timestamp \| null | `returns.returns` | `received_at` |
| `inspectedAt` | timestamp \| null | `returns.returns` | `inspected_at` |
| `putAwayAt` | timestamp \| null | `returns.returns` | `put_away_at` |

---

## 20. GET /returns/{id}/items

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
| `condition` | string \| null | `returns.return_items` | `condition` | Set at inspection |
| `putAwayStatus` | string \| null | `returns.return_items` | `put_away_status` | |
| `assignedSloc` | string \| null | `returns.return_items` | `assigned_sloc` | |
| `inspectedAt` | timestamp \| null | `returns.return_items` | `inspected_at` | |
| `putAwayAt` | timestamp \| null | `returns.return_items` | `put_away_at` | |

---

## 21. PATCH /returns/{id}/cancel

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `reason` | `returns.returns` | Stored in status history detail | UPDATE |
| `cancelledBy` | `returns.returns` | `updated_by` | UPDATE |
| *(derived)* | `returns.returns` | `status → 'Cancelled'`, `updated_at` | UPDATE |

---

## 22. GET /returns/{id}/refund

### `refund` object

| Response Field | Type | Table | Column |
|---|---|---|---|
| `refundId` | string | `returns.return_refunds` | `refund_id` |
| `refundAmount` | number | `returns.return_refunds` | `refund_amount` |
| `currency` | string | `returns.return_refunds` | `currency` |
| `refundMethod` | string | `returns.return_refunds` | `refund_method` |
| `status` | string | `returns.return_refunds` | `status` |
| `referenceNo` | string | `returns.return_refunds` | `reference_no` |
| `processedAt` | timestamp | `returns.return_refunds` | `processed_at` |

### `creditNote` object

| Response Field | Type | Table | Column |
|---|---|---|---|
| `creditNoteId` | string | `payment.credit_notes` | `credit_note_id` |
| `creditNoteNumber` | string | `payment.credit_notes` | `credit_note_number` |
| `invoiceId` | string | `payment.credit_notes` | `invoice_id` |
| `amount` | number | `payment.credit_notes` | `amount` |
| `currency` | string | `payment.credit_notes` | `currency` |
| `reason` | string | `payment.credit_notes` | `reason` |
| `status` | string | `payment.credit_notes` | `status` |

---

## 23. GET /inbound/purchase-orders — List Purchase Orders

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `id` | string | `inbound.purchase_orders` | `purchase_order_id` | |
| `poNumber` | string | `inbound.purchase_orders` | `po_number` | |
| `supplier` | string | External supplier registry | — | Resolved via `supplier_id` |
| `supplierId` | string | `inbound.purchase_orders` | `supplier_id` | |
| `lines` | number | `inbound.purchase_order_lines` | COUNT(`po_line_id`) | Grouped by `purchase_order_id` |
| `status` | string | `inbound.purchase_orders` | `status` | |
| `store` | string | `config.store_locations` | `store_name` | JOIN on `purchase_orders.store_id` |
| `value` | number | `inbound.purchase_order_lines` | SUM(`ordered_qty × unit_cost`) | |
| `goodsReceiveNo` | string \| null | `inbound.purchase_orders` | `goods_receive_no` | |
| `createdAt` | timestamp | `inbound.purchase_orders` | `created_at` | |
| `updatedAt` | timestamp | `inbound.purchase_orders` | `updated_at` | |

---

## 24. GET /inbound/purchase-orders/{id} — Get Purchase Order

Root fields identical to list above plus `storeId`. Additional `lines[]`:

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `poLineId` | string | `inbound.purchase_order_lines` | `po_line_id` | |
| `sku` | string | `inbound.purchase_order_lines` | `sku` | |
| `productName` | string | External product catalog | — | Resolved by SKU |
| `orderedQty` | number | `inbound.purchase_order_lines` | `ordered_qty` | |
| `receivedQty` | number | `inbound.purchase_order_lines` | `received_qty` | Updated by GoodsReceiptConfirmed |
| `unitCost` | number | `inbound.purchase_order_lines` | `unit_cost` | |
| `currency` | string | `inbound.purchase_order_lines` | `currency` | |
| `condition` | string | `inbound.purchase_order_lines` | `condition` | Set by PutAwayConfirmed |
| `sloc` | string | `inbound.purchase_order_lines` | `sloc` | Storage location at put-away |
| `receivedAt` | timestamp | `inbound.purchase_order_lines` | `received_at` | |
| `putAwayAt` | timestamp | `inbound.purchase_order_lines` | `put_away_at` | |

---

## 25. POST /inbound/purchase-orders — Create Purchase Order

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `poNumber` | `inbound.purchase_orders` | `po_number` | INSERT |
| `supplierId` | `inbound.purchase_orders` | `supplier_id` | INSERT |
| `storeId` | `inbound.purchase_orders` | `store_id` | INSERT |
| `lines[].sku` | `inbound.purchase_order_lines` | `sku` | INSERT per line |
| `lines[].orderedQty` | `inbound.purchase_order_lines` | `ordered_qty` | INSERT |
| `lines[].unitCost` | `inbound.purchase_order_lines` | `unit_cost` | INSERT |
| `lines[].currency` | `inbound.purchase_order_lines` | `currency` | INSERT |
| *(derived)* | `inbound.purchase_orders` | `status = 'Created'`, `created_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'PurchaseOrderCreatedEvent'` | INSERT — notifies WMS |

---

## 26. GET /inbound/purchase-orders/{id}/goods-receipts

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `goodsReceiveNo` | string | `inbound.purchase_orders` | `goods_receive_no` | |
| `status` | string | `inbound.purchase_orders` | `status` | |
| `receivedAt` | timestamp | `inbound.purchase_order_lines` | MIN(`received_at`) | |
| `putAwayAt` | timestamp | `inbound.purchase_order_lines` | MIN(`put_away_at`) | |
| `lines[].sku` | string | `inbound.purchase_order_lines` | `sku` | |
| `lines[].receivedQty` | number | `inbound.purchase_order_lines` | `received_qty` | |
| `lines[].condition` | string | `inbound.purchase_order_lines` | `condition` | |
| `lines[].sloc` | string | `inbound.purchase_order_lines` | `sloc` | |

---

## 27. GET /inbound/transfer-orders — List Transfer Orders

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `id` | string | `inbound.transfer_orders` | `transfer_order_id` | |
| `transferNumber` | string | `inbound.transfer_orders` | `transfer_number` | |
| `source` | string | `config.store_locations` | `store_name` | JOIN on `source_store_id` |
| `sourceStoreId` | string | `inbound.transfer_orders` | `source_store_id` | |
| `dest` | string | `config.store_locations` | `store_name` | JOIN on `dest_store_id` |
| `destStoreId` | string | `inbound.transfer_orders` | `dest_store_id` | |
| `lines` | number | `inbound.transfer_order_lines` | COUNT(`to_line_id`) | |
| `status` | string | `inbound.transfer_orders` | `status` | |
| `tracking` | string \| null | `inbound.transfer_orders` | `tracking_id` | Set when TMS dispatch registered |
| `createdAt` | timestamp | `inbound.transfer_orders` | `created_at` | |
| `updatedAt` | timestamp | `inbound.transfer_orders` | `updated_at` | |

---

## 28. GET /inbound/transfer-orders/{id} — Get Transfer Order

Root fields identical to list above. Additional `lines[]`:

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `toLineId` | string | `inbound.transfer_order_lines` | `to_line_id` | |
| `sku` | string | `inbound.transfer_order_lines` | `sku` | |
| `productName` | string | External product catalog | — | Resolved by SKU |
| `requestedQty` | number | `inbound.transfer_order_lines` | `requested_qty` | |
| `transferredQty` | number | `inbound.transfer_order_lines` | `transferred_qty` | Set by TransferPickConfirmed |
| `confirmedAt` | timestamp | `inbound.transfer_order_lines` | `confirmed_at` | |

---

## 29. POST /inbound/transfer-orders — Create Transfer Order

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `sourceStoreId` | `inbound.transfer_orders` | `source_store_id` | INSERT |
| `destStoreId` | `inbound.transfer_orders` | `dest_store_id` | INSERT |
| `lines[].sku` | `inbound.transfer_order_lines` | `sku` | INSERT per line |
| `lines[].requestedQty` | `inbound.transfer_order_lines` | `requested_qty` | INSERT |
| *(derived)* | `inbound.transfer_orders` | `status = 'Created'`, `transfer_number`, `created_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'TransferOrderCreatedEvent'` | INSERT — notifies source WMS |

---

## 30. GET /inbound/transfer-orders/{id}/confirmations

| Response Field | Type | Table | Column | Notes |
|---|---|---|---|---|
| `type` | string | `orders.order_webhook_logs` | `event_type` | `TransferPickConfirmed` or `TransferReceived` |
| `confirmedAt` | timestamp | `orders.order_webhook_logs` | `received_at` | |
| `confirmedBy` | string | `orders.order_webhook_logs` | `source_system` | Always `WMS` |
| `tracking` | string | `inbound.transfer_orders` | `tracking_id` | |

---

## 31. GET /stock/{sku}/ledger — Stock Ledger

### Root fields

| Response Field | Type | Source | Notes |
|---|---|---|---|
| `sku` | string | Query path parameter | |
| `skuName` | string | External product catalog | Resolved by SKU |
| `unitPrice` | number | External catalog or `orders.order_lines.original_unit_price` | Latest known price |
| `currency` | string | External catalog | |

### `locations[]` array

| Response Field | Type | Table | Column |
|---|---|---|---|
| `storeId` | string | `config.store_locations` | `store_id` |
| `storeName` | string | `config.store_locations` | `store_name` |
| `balance` | number | Computed | SUM(`qtyChange`) across all events |

### `locations[].events[]` — source by event type

| `event` value | Source table | Key columns |
|---|---|---|
| `PurchaseOrderPutAwayConfirmed` | `inbound.purchase_order_lines` | `put_away_at`, `received_qty` |
| `TransferPickConfirmed` (out) | `inbound.transfer_order_lines` | `confirmed_at`, `transferred_qty` |
| `TransferReceived` (in) | `inbound.transfer_orders` | `updated_at` when status→Received; qty from `transferred_qty` |
| `PickConfirmed` | `orders.order_status_history` | `changed_at` where `to_status = 'PickConfirmed'`; qty from `order_lines.picked_amount` |

| Response Field | Type | Notes |
|---|---|---|
| `dir` | string | `'in'` (stock arrives) or `'out'` (stock leaves) |
| `ref` | string | PO number, transfer number, or order number |
| `refType` | string | `PurchaseOrder`, `TransferOrder`, or `Order` |
| `qtyChange` | number | Positive for in, negative for out |
| `balance` | number | Running sum after this event |

---

## 32. POST /webhooks/wms/booking-confirmed

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status → 'BookingConfirmed'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'BookingConfirmed'`, `changed_at` | INSERT |
| `orderId` | `orders.order_webhook_logs` | `source_system = 'WMS'`, `event_type = 'BookingConfirmed'`, `received_at` | INSERT |
| `wmsBookingRef` | `orders.order_webhook_logs` | Part of `detail` | |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderBookedEvent'` | INSERT |

---

## 33. POST /webhooks/wms/pick-started

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status → 'PickStarted'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'PickStarted'`, `changed_at` | INSERT |
| `pickerId` | `orders.order_webhook_logs` | Part of `detail` | |
| `startedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PickStarted'` | INSERT |

---

## 34. POST /webhooks/wms/pick-confirmed

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status → 'PickConfirmed'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'PickConfirmed'`, `changed_at` | INSERT |
| `orderId` | `orders.order_webhook_logs` | `source_system = 'WMS'`, `event_type = 'PickConfirmed'`, `received_at` | INSERT |
| `lines[].orderLineId` | `orders.order_lines` | `picked_amount` | UPDATE per line |
| `lines[].substituted = true` | `orders.order_line_substitutions` | `order_line_id`, `substitute_sku`, etc. | INSERT if substituted |
| *(derived)* | `orders.order_outbox` | `event_type = 'PickConfirmedEvent'` | INSERT — triggers POS recalculation |

---

## 35. POST /webhooks/wms/packed

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status → 'Packed'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'Packed'`, `changed_at` | INSERT |
| `orderId` | `orders.order_webhook_logs` | `event_type = 'Packed'`, `received_at` | INSERT |
| `packages[].trackingId` | `orders.order_packages` | `tracking_id` | INSERT per package |
| `packages[].vehicleType` | `orders.order_packages` | `vehicle_type` | INSERT |
| `packages[].weight` | `orders.order_packages` | `package_weight` | INSERT |
| `packages[].lineIds[]` | `orders.order_package_lines` | `package_id`, `order_line_id` | INSERT per line per package |
| *(derived)* | `orders.order_outbox` | `event_type = 'PackedEvent'` | INSERT — notifies TMS to dispatch |

---

## 36. POST /webhooks/wms/substitution-offered

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `substitution_flag = true`, `pos_recalc_pending = true`, `updated_at` | UPDATE |
| `orderLineId` | `orders.order_line_substitutions` | `order_line_id` | INSERT |
| `substituteSku` | `orders.order_line_substitutions` | `substitute_sku` | INSERT |
| `substituteProductName` | `orders.order_line_substitutions` | `substitute_product_name` | INSERT |
| `substituteUnitPrice` | `orders.order_line_substitutions` | `substitute_unit_price` | INSERT |
| `substitutedAmount` | `orders.order_line_substitutions` | `substituted_amount` | INSERT |
| `offeredAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'SubstitutionOffered'` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'SubstitutionOfferedEvent'` | INSERT — notifies customer |

---

## 37. POST /webhooks/wms/put-away-confirmed

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `returnId` | `returns.returns` | `status → 'PutAway'`, `put_away_at` | UPDATE |
| `items[].sku` | `returns.return_items` | Matched by `return_id + sku` | |
| `items[].condition` | `returns.return_items` | `condition`, `put_away_status = 'PutAway'` | UPDATE |
| `items[].sloc` | `returns.return_items` | `assigned_sloc`, `put_away_at` | UPDATE |
| `items[]` | `returns.return_put_away_logs` | Full log entry per item | INSERT |
| *(derived)* | `returns.return_refunds` | `return_id`, `refund_amount`, `refund_method`, `status = 'Pending'` | INSERT — atomic with put-away |
| *(derived)* | `payment.credit_notes` | `order_id`, `invoice_id`, `amount`, `reason = 'Return'`, `status = 'Issued'` | INSERT — atomic |

---

## 38. POST /webhooks/wms/goods-receipt-confirmed

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `purchaseOrderId` | `inbound.purchase_orders` | `status → 'FullyReceived'` or `'PartiallyReceived'`, `goods_receive_no`, `updated_at` | UPDATE |
| `lines[].sku` | `inbound.purchase_order_lines` | Matched by `purchase_order_id + sku` | |
| `lines[].receivedQty` | `inbound.purchase_order_lines` | `received_qty`, `received_at` | UPDATE per line |
| *(derived)* | `orders.order_outbox` | `event_type = 'GoodsReceiptConfirmedEvent'` | INSERT |

> **Note:** `order_webhook_logs` is scoped to `order_id`. PO webhooks do not carry an `order_id` — a separate `inbound_webhook_logs` table may be needed for PO-level audit.

---

## 39. POST /webhooks/wms/purchase-order-put-away-confirmed

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `purchaseOrderId` | `inbound.purchase_orders` | `status → 'Closed'`, `updated_at` | UPDATE |
| `items[].sku` | `inbound.purchase_order_lines` | Matched by `purchase_order_id + sku` | |
| `items[].condition` | `inbound.purchase_order_lines` | `condition` | UPDATE |
| `items[].sloc` | `inbound.purchase_order_lines` | `sloc` | UPDATE |
| `putAwayAt` | `inbound.purchase_order_lines` | `put_away_at` | UPDATE per line |
| *(derived)* | `orders.order_outbox` | `event_type = 'PurchaseOrderClosedEvent'` | INSERT — signals stock available |

---

## 40. POST /webhooks/wms/transfer-pick-confirmed

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `transferOrderId` | `inbound.transfer_orders` | `status → 'PickConfirmed'`, `updated_at` | UPDATE |
| `lines[].sku` | `inbound.transfer_order_lines` | Matched by `transfer_order_id + sku` | |
| `lines[].transferredQty` | `inbound.transfer_order_lines` | `transferred_qty` | UPDATE per line |
| `confirmedAt` | `inbound.transfer_order_lines` | `confirmed_at` | UPDATE per line |
| *(derived)* | `orders.order_outbox` | `event_type = 'TransferPickConfirmedEvent'` | INSERT — notifies TMS to dispatch |

---

## 41. POST /webhooks/wms/transfer-received

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `transferOrderId` | `inbound.transfer_orders` | `status → 'Completed'`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_outbox` | `event_type = 'TransferReceivedEvent'` | INSERT |

---

## 42. POST /webhooks/wms/damaged-goods-received

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `pre_hold_status` ← current `status`; `status → 'OnHold'`; `hold_reason = 'PackageDamaged'`; `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'OnHold'`, `detail`, `changed_at` | INSERT |
| `orderId` | `orders.order_webhook_logs` | `source_system = 'WMS'`, `event_type = 'DamagedGoodsReceived'`, `received_at` | INSERT |
| `trackingId` | `inbound.damaged_goods_receipts` | `tracking_id`, `order_id`, `received_at`, `status = 'Received'` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'DamagedGoodsReceivedEvent'` | INSERT |

---

## 43. POST /webhooks/wms/damaged-goods-put-away

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `damagedReceiptId` | `inbound.damaged_goods_receipts` | `status → 'PutAway'`, `put_away_at`, `updated_at` | UPDATE |
| `items[].sku` | `inbound.damaged_goods_items` | `sku` | INSERT per item |
| `items[].condition` | `inbound.damaged_goods_items` | `condition` | INSERT |
| `items[].sloc` | `inbound.damaged_goods_items` | `sloc` | INSERT |
| `items[].qty` | `inbound.damaged_goods_items` | `quantity` | INSERT |
| `putAwayAt` | `inbound.damaged_goods_items` | `confirmed_at` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'DamagedGoodsPutAwayEvent'` | INSERT |

---

## 44. POST /webhooks/tms/package-dispatched

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `trackingId` | `orders.order_packages` | `status → 'OutForDelivery'`, `updated_at` | UPDATE — lookup by `tracking_id` |
| *(derived from package)* | `orders.orders` | `status → 'OutForDelivery'`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_status_history` | `to_status = 'OutForDelivery'`, `changed_at` | INSERT |
| `dispatchedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PackageDispatched'` | INSERT |

---

## 45. POST /webhooks/tms/package-delivered

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `trackingId` | `orders.order_packages` | `status → 'Delivered'`, `updated_at` | UPDATE |
| *(derived)* | `orders.orders` | `status → 'Delivered'`, `updated_at` | UPDATE |
| *(derived)* | `orders.order_status_history` | `to_status = 'Delivered'`, `changed_at` | INSERT |
| `deliveredAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PackageDelivered'` | INSERT |
| `proofOfDelivery` | `orders.order_webhook_logs` | `detail` | Stored as audit detail |
| *(derived)* | `orders.order_outbox` | `event_type = 'DeliveredEvent'` | INSERT — triggers invoice via POS |
| *(derived)* | `payment.invoices` | `order_id`, `invoice_number`, `total_amount`, `status = 'Generated'` | INSERT |

---

## 46. POST /webhooks/tms/package-damage-reported

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `trackingId` | `orders.order_packages` | `status → 'Damaged'`, `updated_at` | UPDATE |
| *(derived)* | `orders.orders` | `pre_hold_status` ← current `status`; `status → 'OnHold'`; `hold_reason = 'PackageDamaged'` | UPDATE |
| *(derived)* | `orders.order_status_history` | `to_status = 'OnHold'`, `detail` = reason | INSERT |
| `reportedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PackageDamageReported'`; `detail` = driverNote | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'PackageDamagedEvent'` | INSERT — triggers return-to-warehouse workflow |

---

## 47. POST /webhooks/pos/pos-collection-ready

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status → 'ReadyForCollection'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'ReadyForCollection'`, `changed_at` | INSERT |
| `notifiedAt` | `orders.order_webhook_logs` | `received_at`; `source_system = 'POS'`; `event_type = 'PosCollectionReady'` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'ReadyForCollectionEvent'` | INSERT — notifies customer |

---

## 48. POST /webhooks/pos/collected

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status → 'Collected'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'Collected'`, `changed_at` | INSERT |
| `collectedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'Collected'` | INSERT |
| `collectedBy` | `orders.order_webhook_logs` | `detail` | |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderCollectedEvent'` | INSERT |
| *(derived)* | `payment.invoices` | `order_id`, `invoice_number`, `total_amount`, `status = 'Generated'` | INSERT — triggered for Click & Collect |

---

## 49. POST /webhooks/pos/invoiced

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status → 'Invoiced'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'Invoiced'`, `changed_at` | INSERT |
| `invoiceNumber` | `payment.invoices` | `invoice_number`, `status = 'Issued'` | UPDATE — matches existing Generated invoice |
| `totalAmount` | `payment.invoices` | `total_amount` | UPDATE |
| `invoicedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'Invoiced'` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderInvoicedEvent'` | INSERT |

---

## 50. POST /webhooks/pos/payment-confirmed

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `status → 'Paid'`, `updated_at` | UPDATE |
| `orderId` | `orders.order_status_history` | `to_status = 'Paid'`, `changed_at` | INSERT |
| `paidAmount` | `payment.payment_transactions` | `amount` | INSERT new transaction |
| `paymentMethod` | `payment.payment_transactions` | `payment_method` | INSERT |
| `currency` | `payment.payment_transactions` | `currency` | INSERT |
| `paidAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PaymentConfirmed'` | INSERT |
| *(derived)* | `orders.order_outbox` | `event_type = 'OrderPaidEvent'` | INSERT |

---

## 51. POST /webhooks/pos/recalculation-result

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `pos_recalc_pending → false`, `updated_at` | UPDATE |
| `adjustedAmount` | `payment.order_line_amounts` | Per recalc round per line | INSERT per line |
| `promotionsApplied[].promoCode` | `payment.order_promotions` | `promo_code` | INSERT per promo |
| `promotionsApplied[].discountAmount` | `payment.order_promotions` | `discount_amount` | |
| `promotionsApplied[].description` | `payment.order_promotions` | `promo_name` | |
| `recalculatedAt` | `payment.order_line_amounts` | `recalculated_at`, `created_at` | |
| `recalculatedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'RecalculationResult'` | INSERT |
| *(derived)* | `payment.order_line_amounts` | `recalc_round` = prior MAX + 1; `trigger_event = 'PickConfirmed'` | INSERT per line |

---

## 52. POST /webhooks/pos/pos-recalc-completed

| Request Field | Table Written | Column | Action |
|---|---|---|---|
| `orderId` | `orders.orders` | `pos_recalc_pending → false`, `updated_at` | UPDATE |
| `completedAt` | `orders.order_webhook_logs` | `received_at`; `event_type = 'PosRecalcCompleted'` | INSERT |
| `finalAmount` | `orders.order_webhook_logs` | `detail` | Audit record only |
| *(derived)* | `orders.order_outbox` | `event_type = 'RecalcCompletedEvent'` | INSERT — unblocks packing workflow |

---

## 53. Cross-Reference: Read API Table Usage

`R` = table is read by this endpoint. `R(ct)` = only the count is used.

| Table | List Orders | Get Order | Timeline | Lines | Packages | Webhooks | Substitutions | Slot | List POs | Get PO | List TOs | Get TO | Stock Ledger |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| `orders.orders` | R | R | R | | | | | R | | | | | |
| `orders.order_lines` | R(ct) | R | | R | | | R | | | | | | R |
| `orders.order_addresses` | R | R | R | | | | | | | | | | |
| `orders.order_customers` | | R | | | | | | | | | | | |
| `orders.order_packages` | | R | | | R | | | | | | | | |
| `orders.order_package_lines` | | R | | | R | | | | | | | | |
| `orders.delivery_slots` | R | R | | | | | | R | | | | | |
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

---

## 54. POST /webhooks/sts/abb-tax-invoice

**Trigger:** STS sends the ABB/Tax Invoice document link. Pre-paid: fires after `PickConfirmed`, OMS forwards to WMS + Gateway. POD: fires after `Delivered`, OMS forwards to TMS + Gateway. Routing decided by `orders.is_prepaid`.

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.order_webhook_logs` | `source_system = 'STS'`, `event_type = 'AbbTaxInvoice'`, `received_at`, `detail` = invoiceNumber | INSERT |
| `invoiceNumber` | `payment.invoices` | `invoice_number`, `invoice_type = 'AbbTaxInvoice'`, `status = 'Generated'` | INSERT |
| `invoiceLink` | `payment.invoices` | `invoice_link` | UPDATE |
| `amount` | `payment.invoices` | `total_amount` | UPDATE |
| `currency` | `payment.invoices` | `currency` | UPDATE |
| `issuedAt` | `payment.invoices` | `generated_at` | UPDATE |
| *(X-Idempotency-Key)* | `payment.invoices` | `source_sts_ref` | UPDATE |
| *(if is_prepaid = true)* | `orders.order_outbox` | `event_type = 'AbbTaxInvoiceSentToWmsEvent'`, payload includes `invoiceLink` | INSERT → WMS for printing before dispatch |
| *(if is_prepaid = false)* | `orders.order_outbox` | `event_type = 'AbbTaxInvoiceSentToTmsEvent'`, payload includes `invoiceLink` | INSERT → TMS driver receipt after delivery |
| *(always)* | `orders.order_outbox` | `event_type = 'AbbTaxInvoiceSentToGatewayEvent'`, payload includes `invoiceLink` | INSERT → CFW Gateway for customer |

---

## 55. POST /webhooks/sts/credit-note

**Trigger:** STS sends a Credit Note document link as a separate webhook when a credit note exists for the order. Pre-paid: forwards to WMS. POD: forwards to TMS. Routing decided by `orders.is_prepaid`.

### Request body → DB writes

| Request Field | Table Written | Column Updated | Action |
|---|---|---|---|
| `orderId` | `orders.order_webhook_logs` | `source_system = 'STS'`, `event_type = 'StsCreditNote'`, `received_at`, `detail` = creditNoteNumber | INSERT |
| `creditNoteNumber` | `payment.credit_notes` | `credit_note_number`, `reason = 'PriceAdjustment'`, `source = 'STS'`, `status = 'Issued'` | INSERT |
| `creditNoteLink` | `payment.credit_notes` | `credit_note_link` | UPDATE |
| `amount` | `payment.credit_notes` | `amount` | UPDATE |
| `currency` | `payment.credit_notes` | `currency` | UPDATE |
| *(X-Idempotency-Key)* | `payment.credit_notes` | `source_sts_ref` | UPDATE |
| *(if is_prepaid = true)* | `orders.order_outbox` | `event_type = 'CreditNoteSentToWmsEvent'`, payload includes `creditNoteLink` | INSERT → WMS |
| *(if is_prepaid = false)* | `orders.order_outbox` | `event_type = 'CreditNoteSentToTmsEvent'`, payload includes `creditNoteLink` | INSERT → TMS |

---

**STS webhook write targets** (not read endpoints — shown separately):

| Webhook | Tables Written |
|---|---|
| `POST /webhooks/sts/abb-tax-invoice` | `orders.order_webhook_logs`, `payment.invoices` (`invoice_link`, `source_sts_ref`, `generated_at`), `orders.order_outbox` ×2 (WMS or TMS + Gateway) |
| `POST /webhooks/sts/credit-note` | `orders.order_webhook_logs`, `payment.credit_notes` (`credit_note_link`, `source_sts_ref`), `orders.order_outbox` ×1 (WMS or TMS) |
