# Sprint Connect OMS — Outbox Field Mapping

**Version:** 1.0  
**Last updated:** 2026-05-16  
**Scope:** All four outbox adapter integrations dispatched by `oms-outbox-worker`

---

## Overview

This document provides field-level traceability from every OMS database column through the internal DTO layer to the exact external API request field sent to each downstream system. The OMS dispatches outbox events to four external targets — POS (recalculation), Gateway (status update), and TMS/WMS (tax invoice and credit note). For each integration the table below names the external request field, its data type and mandatory flag as defined by the external API spec, the OMS DTO property that supplies the value, the originating database column, and any transformation or encoding rule applied by the payload builder before dispatch. All monetary values in OMS are stored in baht with decimal precision (e.g. `2450.00`). No unit conversion is needed before transmission — values are passed directly.

---

## Integration 1 — POS Recalculation

**Trigger event:** `PosRecalculateEvent` fired on `POST /api/orders/{id}/recalculate`  
**Endpoint key:** `pos.recalculate`  
**Target URL:** `https://pos.internal/api/recalculate`  
**Auth:** StaticToken (`accessToken` request header = `pos-access-token`)  
**Payload builder:** `PosRecalcPayload.Build(OrderDto order, List<OrderPromotionDto> promotions)`

### Request Header Fields

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `accessToken` | string | M | — | — | Hardcoded: read from endpoint config `Headers["accessToken"]` = `pos-access-token` |
| `refId` | string | O | — | — | Hardcoded: `""` (empty string); caller-side reference, not populated by OMS |

### Request Body Fields

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `Orderno` | string | M | `OrderDto.OrderNumber` | `orders.order_number` | Direct mapping. Format: `WPYYMMsssnnnnnnn`, max 16 chars |
| `StoreCode` | string | M | `OrderDto.StoreId` | `orders.store_id` | Direct mapping. References `config.store_locations` |
| `CustomerSegment` | string | M | — | — | Hardcoded: `"STANDARD"` — customer tier not yet surfaced in OMS |
| `CustomerCDP_ID` | string | M | `OrderDto.ExternalCustomerId ?? OrderDto.Id` | `order_customers.external_customer_id` or `orders.order_id` | Prefers CDP ID from customer snapshot; falls back to internal OMS order ID |
| `SaleChannel` | string | M | `OrderDto.ChannelType` | `orders.channel_type` | Mapped: `Gateway`→`CFW`, `App`→`CHEF`, `POS`→`CHO`, all others→`CFW` |
| `salesource` | string | M | `OrderDto.SubChannel` | `orders.sub_channel` | Mapped: `WA`→`WA`, `XB`→`XB`, `CF`→`CF`, `CO`→`CO`, all others→`WA` |
| `OrderItems` | array | M | `OrderDto.Lines` | `order_lines` (all rows for this order) | One element per order line; see child fields below |
| `OrderItems[].SEQ` | int | M | — | — | Computed: 1-based index position (`idx + 1`) within the lines list; not stored in DB |
| `OrderItems[].SK_CODE` | string | M | `OrderLineDto.Sku` | `order_lines.sku` | Direct mapping |
| `OrderItems[].QNT` | decimal | M | `OrderLineDto.RequestedAmount` | `order_lines.requested_amount` | Direct mapping. For weight items: `QNT = QNTItem * AvGatewayeight` per spec; OMS sends raw `requested_amount` |
| `OrderItems[].WeightItemFlag` | boolean | M | `OrderLineDto.Uom` | `order_lines.unit_of_measure` | Computed: `true` if `Uom == "KG"`, otherwise `false` |
| `OrderItems[].AvGatewayeight` | decimal | O | — | — | Hardcoded: `null` — average weight not tracked at the OMS order-line level |
| `OrderItems[].QNTItem` | decimal | O | — | — | Hardcoded: `null` — unit item count not tracked separately from `requested_amount` |
| `OrderItems[].itemUnit` | string | M | `OrderLineDto.Uom` | `order_lines.unit_of_measure` | Direct mapping. Expected values: `Each`, `KG` |
| `OrderItems[].AMT` | decimal | M | `OrderLineDto.UnitPrice * OrderLineDto.RequestedAmount` | `order_lines.original_unit_price`, `order_lines.requested_amount` | Computed: `UnitPrice * RequestedAmount`. Represents gross amount before discount |
| `OrderItems[].UPC` | decimal | M | `OrderLineDto.UnitPrice` | `order_lines.original_unit_price` | Direct mapping. Unit price in baht |
| `OrderItems[].CTLID` | string[] | O | `OrderPromotionDto.SourcePromoId` (filtered by `OrderLineId` or non-null `SourcePromoId`) | `payment.order_promotions.source_promo_id` | Filtered from the promotions list: promotions linked to this line ID or any promotion with a non-null `SourcePromoId`. Sends list of POS promotion control IDs |
| `OrderItems[].PriceRequestUPC` | decimal | O | — | — | Hardcoded: `null` — action price not tracked in OMS |
| `OrderItems[].DiscountCode` | string | O | — | — | Hardcoded: `null` — line-level discount codes resolved by POS, not stored in OMS |
| `OrderItems[].ExcludedBMGN` | boolean | O | — | — | Hardcoded: `false` — all lines are eligible for BMGN promotions by default |
| `OrderItems[].ReferenceSEQ` | string | M | `OrderLineDto.Id` | `order_lines.order_line_id` | OMS internal line ID used as the stable reference back to the original line |
| `couponItem` | array | M | — | — | Hardcoded: `[]` (empty array) — coupon redemption is handled by POS, not passed from OMS |

---

## Integration 2 — Gateway Update Status

**Trigger event:** `OutForDeliveryEvent` raised when TMS fires `PackageDispatched` webhook  
**Endpoint key:** `Gateway.out-for-delivery`  
**Target URL:** `https://Gateway.internal/api/status-update`  
**Auth:** StaticToken (`static-Gateway-token`)  
**Payload builder:** `GatewayUpdateStatusPayload.Build(OrderDto order, OrderPaymentDto? payment)`

### Request Header Fields

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `x-api-key` | string | M | — | — | Hardcoded: `static-Gateway-token` from endpoint config `StaticToken` |
| `x-channel` | string | M | — | — | Hardcoded: `"TWD"` (per spec) |

### Request Body Fields

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `order_id` | string | M | `OrderDto.OrderNumber` | `orders.order_number` | Direct mapping. External-facing order reference |
| `sale_channel` | string | M | `OrderDto.ChannelType` | `orders.channel_type` | Mapped: `Gateway`→`CFW`, `App`→`CHEF`, `POS`→`CHO`, all others→`CFW` |
| `sale_source` | string | M | `OrderDto.SubChannel` | `orders.sub_channel` | Mapped: `WA`→`WA`, `XB`→`XB`, `CF`→`CF`, `CO`→`CO`, all others→`WA` |
| `order_status` | string | M | — | — | Hardcoded: `"DELIVERED"` — this payload is only ever sent on the OutForDelivery→Delivered transition |
| `updated_at` | datetime | M | — | — | Hardcoded: `DateTime.UtcNow` at dispatch time — reflects the moment OMS is notifying Gateway |
| `updated_by` | string | M | — | — | Hardcoded: `"OMS"` |
| `payments` | array | O | `OrderPaymentDto` (if present) | `payment.order_payments` | Empty array `[]` when no payment record exists; one element when payment is present; see child fields below |
| `payments[].payment_type` | string | M | `OrderDto.PaymentFlow` | `orders.payment_flow` | Direct pass-through. `orders.payment_flow` already holds the string value (`"PRE_PAID"` or `"PAY_ON_DELIVERY"`); no conversion needed |
| `payments[].payment_method` | string | M | `OrderPaymentDto.PaymentMethod` | `payment.order_payments.payment_method` | Mapped: `CreditCard`→`CREDIT_CARD`, `QRCode`→`QR_CODE`, `PayOnDelivery`→`POD`, all others→`POD` |
| `payments[].payment_jd` | string | M | `OrderPaymentDto.PaymentMethod` | `payment.order_payments.payment_method` | Direct mapping of raw OMS payment method string (e.g. `"CreditCard"`). Used by Gateway as Payment ID reference for PaymentLink flows |
| `payments[].payment_amount` | decimal | M | `OrderPaymentDto.TotalAmount` | `payment.order_payments.total_amount` | Direct mapping. Value already in baht |
| `payments[].tendor` | string | M | `OrderPaymentDto.PaymentMethod` | `payment.order_payments.payment_method` | Mapped: `CreditCard`→`WCRD`, `QRCode`→`QRPP`, `PayOnDelivery`→`WCOD`, all others→`WCOD` |
| `payments[].payment_datetime` | datetime nullable | O | `OrderPaymentDto.CreatedAt` | `payment.order_payments.created_at` | Direct mapping; `null` if no payment record |
| `payments[].payment_status` | string | M | `OrderPaymentDto.Status` | `payment.order_payments.status` | Mapped: `"Captured"`→`"PAID"`, all others→`"UNPAID"` |
| `payments[].paid_at` | datetime | M | `OrderPaymentDto.UpdatedAt` | `payment.order_payments.updated_at` | Direct mapping; represents the time the payment status last changed |
| `payments[].created_at` | datetime | M | `OrderPaymentDto.CreatedAt` | `payment.order_payments.created_at` | Direct mapping |
| `payments[].created_by` | string | M | — | — | Hardcoded: `"OMS"` |
| `payments[].updated_at` | datetime | M | `OrderPaymentDto.UpdatedAt` | `payment.order_payments.updated_at` | Direct mapping |
| `payments[].updated_by` | string | M | — | — | Hardcoded: `"OMS"` |

**Fields from spec not populated by this builder (sent as absent / not included):**

| External Field | Reason not populated |
|---|---|
| `payments[].additional_info` | Not tracked in OMS payment model |
| `payments[].approve_code` | Not returned by OMS payment gateway integration |
| `payments[].batch_id` | Not tracked in OMS payment model |
| `payments[].creditcard` | Masked card number not stored in OMS |
| `payments[].payment_reason` | Not applicable for successful delivery status updates |
| `payments[].trace_no` | Gateway trace number not surfaced to OMS |

---

## Integration 2b — WaveStarted Forwarding to Gateway

**Trigger event:** `WaveStartedSentToGateway` — fired when WMS sends `POST /api/webhooks/wms/wave-started` and the order is in `PickStarted` status  
**Endpoint key:** `Gateway.wave-started`  
**Target URL:** `https://Gateway.internal/api/status-update`  
**Auth:** StaticToken (`x-api-key: static-Gateway-token`)  
**Channel scope:** All channel types (`"*"` wildcard rule) — consistent with `OutForDelivery` and `Delivered` Gateway dispatch  
**Payload builder:** Inline in `WaveStartedHandler.Handle()` — no dedicated builder class

### Request Header Fields

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `x-api-key` | string | M | — | — | Hardcoded: `static-Gateway-token` from endpoint config `StaticToken` |
| `x-channel` | string | M | — | — | Hardcoded: `"TWD"` from endpoint config `Headers` |

### Request Body Fields

Uses `GatewayUpdateStatusPayload.Build(order, payment, "WAVE_STARTED")` — same builder and field mapping as Integration 2, with `order_status` set to `"WAVE_STARTED"`. See Integration 2 for the full field-level mapping.

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `order_id` | string | M | `OrderDto.OrderNumber` | `orders.order_number` | Direct mapping |
| `sale_channel` | string | M | `OrderDto.ChannelType` | `orders.channel_type` | Mapped: `Gateway`→`CFW`, `App`→`CHEF`, `POS`→`CHO`, all others→`CFW` |
| `sale_source` | string | M | `OrderDto.SubChannel` | `orders.sub_channel` | Mapped: `WA`→`WA`, `XB`→`XB`, `CF`→`CF`, `CO`→`CO`, all others→`WA` |
| `order_status` | string | M | — | — | Hardcoded: `"WAVE_STARTED"` |
| `updated_at` | datetime | M | — | — | Hardcoded: `DateTime.UtcNow` at dispatch time |
| `updated_by` | string | M | — | — | Hardcoded: `"OMS"` |
| `payments` | array | O | `OrderPaymentDto` (if present) | `payment.order_payments` | Same mapping as Integration 2 — empty `[]` when no payment record exists |

---

## Integration 2c — PickConfirmed Forwarding to Gateway

**Trigger event:** `PickConfirmedEvent` — fired when WMS sends `POST /api/webhooks/wms/pick-confirmed` and the order transitions to `PickConfirmed` status  
**Endpoint key:** `Gateway.pick-confirm`  
**Target URL:** `https://Gateway.internal/api/status-update`  
**Auth:** StaticToken (`x-api-key: static-Gateway-token`)  
**Channel scope:** All channel types (`"*"` wildcard rule)  
**Payload builder:** `GatewayUpdateStatusPayload.Build(order, payment, "PICK_CONFIRMED")`

### Request Header Fields

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `x-api-key` | string | M | — | — | Hardcoded: `static-Gateway-token` from endpoint config `StaticToken` |
| `x-channel` | string | M | — | — | Hardcoded: `"TWD"` from endpoint config `Headers` |

### Request Body Fields

Uses `GatewayUpdateStatusPayload.Build(order, payment, "PICK_CONFIRMED")` — same builder and field mapping as Integration 2, with `order_status` set to `"PICK_CONFIRMED"`. See Integration 2 for the full field-level mapping.

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `order_id` | string | M | `OrderDto.OrderNumber` | `orders.order_number` | Direct mapping |
| `sale_channel` | string | M | `OrderDto.ChannelType` | `orders.channel_type` | Mapped: `Gateway`→`CFW`, `App`→`CHEF`, `POS`→`CHO`, all others→`CFW` |
| `sale_source` | string | M | `OrderDto.SubChannel` | `orders.sub_channel` | Mapped: `WA`→`WA`, `XB`→`XB`, `CF`→`CF`, `CO`→`CO`, all others→`WA` |
| `order_status` | string | M | — | — | Hardcoded: `"PICK_CONFIRMED"` |
| `updated_at` | datetime | M | — | — | Hardcoded: `DateTime.UtcNow` at dispatch time |
| `updated_by` | string | M | — | — | Hardcoded: `"OMS"` |
| `payments` | array | O | `OrderPaymentDto` (if present) | `payment.order_payments` | Same mapping as Integration 2 — empty `[]` when no payment record exists |

---

## Integration 3 — TMS/WMS Tax Invoice

**Trigger events:**  
- `ABBTaxInvoiceSentToTMS` (dispatched to TMS) when STS fires `ABBTaxInvoiceReceived`  
- `ABBTaxInvoiceSentToWMS` (dispatched to WMS) when STS fires `ABBTaxInvoiceReceived`  

**Endpoint keys:**  
- `tms.abb-tax-invoice` → `https://tms.internal/api/invoices` (StaticToken: `static-tms-token`)  
- `wms.tax-invoice` → `https://wms.internal/api/invoices` (StaticToken: `static-wms-token`)  

**Payload builder:** `TmsWmsTaxInvoicePayload.Build(OrderDto order, InvoiceDto invoice, List<OrderLineDto> lines)`

> Both `tms.abb-tax-invoice` and `wms.tax-invoice` receive an identical payload from this builder. The same field mapping applies to both endpoints. Routing is controlled by `config.outbox_routing_rules` based on `payment_method`: Prepaid orders route to WMS; POD orders route to TMS.

### Request Header Fields

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `x-api-key` | string | M | — | — | `static-tms-token` or `static-wms-token` from endpoint config `StaticToken` |
| `x-channel` | string | M | — | — | Hardcoded: `"TWD"` (per spec) |

### Request Body Fields

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `order_id` | string | M | `OrderDto.OrderNumber` | `orders.order_number` | Direct mapping |
| `sale_channel` | string | M | `OrderDto.ChannelType` | `orders.channel_type` | Mapped: `Gateway`→`CFW`, `App`→`CHEF`, `POS`→`CHO`, all others→`CFW` |
| `sale_source` | string | M | `OrderDto.SubChannel` | `orders.sub_channel` | Mapped: `WA`→`WA`, `XB`→`XB`, `CF`→`CF`, `CO`→`CO`, all others→`WA` |
| `document_type` | string | M | — | — | Hardcoded: `"INV"` — this builder always dispatches a tax invoice document type |
| `documents` | array | M | `InvoiceDto` | `payment.invoices` | Always a single-element array; see child fields below |
| `documents[].abb_id` | string | M | `InvoiceDto.InvoiceNumber` | `payment.invoices.invoice_number` | Direct mapping. The ABB receipt number issued by STS (e.g. `ABB-2024-001`) |
| `documents[].tax_invoice_id` | string | M | `InvoiceDto.InvoiceNumber` | `payment.invoices.invoice_number` | Same value as `abb_id` — both fields receive the OMS invoice number in this builder |
| `documents[].cn_abb_id` | string | O | — | — | Hardcoded: `null` — this builder handles invoice dispatch, not credit notes |
| `documents[].cn_tax_id` | string | O | — | — | Hardcoded: `null` — this builder handles invoice dispatch, not credit notes |
| `documents[].url` | string | M | `InvoiceDto.InvoiceLink ?? ""` | `payment.invoices.invoice_link` | Direct mapping. PDF download URL from STS. Sent as empty string `""` when `is_success` is `false` (STS failed to generate document) |
| `documents[].is_success` | boolean | M | `InvoiceDto.Status` | `payment.invoices.status` | Computed: `true` when `Status == "Issued"`, otherwise `false` |
| `documents[].document_created_datetime` | datetime | M | `InvoiceDto.IssuedAt ?? InvoiceDto.GeneratedAt` | `payment.invoices.issued_at` or `payment.invoices.generated_at` | Prefers `issued_at` (when STS has finalised the document); falls back to `generated_at` |

---

## Integration 4 — TMS/WMS Credit Note

**Trigger events:**  
- `CreditNoteSentToTMS` (dispatched to TMS) when STS fires `CreditNoteReceived`  
- `CreditNoteSentToWMS` (dispatched to WMS) when STS fires `CreditNoteReceived`  

**Endpoint keys:**  
- `tms.credit-note` → (not in current seed; implied by routing pattern for POD orders)  
- `wms.credit-note` → `https://wms.internal/api/credit-notes` (StaticToken: `static-wms-token`)  

**Payload builder:** `TmsWmsCreditNotePayload.Build(OrderDto order, CreditNoteDto creditNote, InvoiceDto? invoice, List<ReturnItemDto> returnItems)`

> Routing is controlled by `config.outbox_routing_rules`: Prepaid orders route credit notes to `wms.credit-note`; POD orders route to `tms.credit-note`. The payload shape is identical for both endpoints.

### Request Header Fields

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `x-api-key` | string | M | — | — | `static-wms-token` or TMS equivalent from endpoint config `StaticToken` |
| `x-channel` | string | M | — | — | Hardcoded: `"TWD"` (per spec) |

### Request Body Fields

| External Field | Data Type | Mandatory | OMS Source (DTO Field) | OMS DB Column | Transform / Notes |
|---|---|---|---|---|---|
| `store_code` | string | M | `OrderDto.StoreId` | `orders.store_id` | Direct mapping |
| `member_id` | string | M | `OrderDto.ExternalCustomerId ?? OrderDto.Id` | `order_customers.external_customer_id` or `orders.order_id` | Prefers CDP member ID; falls back to OMS internal order ID |
| `customer_segment` | string | M | — | — | Hardcoded: `"STANDARD"` — customer tier not yet surfaced in OMS |
| `sale_channel` | string | M | `OrderDto.ChannelType` | `orders.channel_type` | Mapped: `Gateway`→`CFW`, `App`→`CHEF`, `POS`→`CHO`, all others→`CFW` |
| `salesource` | string | M | `OrderDto.SubChannel` | `orders.sub_channel` | Mapped: `WA`→`WA`, `XB`→`XB`, `CF`→`CF`, `CO`→`CO`, all others→`WA` |
| `order_id` | string | M | `OrderDto.OrderNumber` | `orders.order_number` | Direct mapping |
| `abb_id` | string | M | `CreditNoteDto.CreditNoteNumber` | `payment.credit_notes.credit_note_number` | The credit note number issued by STS (e.g. `CN-RET-001`) |
| `tax_invoice_id` | string | O | `InvoiceDto.InvoiceNumber` | `payment.invoices.invoice_number` | Direct mapping when invoice exists; `null` when no invoice is linked to the return |
| `reference_abb_id` | string | M | `InvoiceDto.InvoiceNumber ?? CreditNoteDto.CreditNoteNumber` | `payment.invoices.invoice_number` or `payment.credit_notes.credit_note_number` | Prefers the original invoice number as ABB reference; falls back to the credit note number itself |
| `reference_order_datetime` | datetime | O | `OrderDto.OrderDate` | `orders.created_at` | Direct mapping of the original order creation datetime |
| `transaction_status` | string | M | — | — | Hardcoded: `"FULLY_CN"` — OMS currently models all credit note dispatches as full credit notes |
| `order_datetime` | time | M | `OrderDto.OrderDate` | `orders.created_at` | Formatted: `yyyy-MM-dd HH:mm:ss` — POS expects this exact format |
| `items` | array | M | `List<ReturnItemDto>` | `returns.return_items` | One element per return item; see child fields below |
| `items[].line_item_no` | int | M | — | — | Computed: 1-based index position (`idx + 1`) within the return items list |
| `items[].sku_code` | string | M | `ReturnItemDto.Sku` | `returns.return_items.sku` | Direct mapping |
| `items[].pr_code` | string | M | `ReturnItemDto.Barcode` | `returns.return_items.barcode` | Direct mapping. Supplier barcode used for WMS scanning |
| `items[].is_weight_item` | boolean | M | `ReturnItemDto.Uom` | `returns.return_items.uom` | Computed: `true` if `Uom == "KG"`, otherwise `false` |
| `items[].avg_weight` | decimal | M | — | — | Hardcoded: `0` — average weight not tracked on return items in OMS |
| `items[].unit_price` | decimal | M | `ReturnItemDto.UnitPrice` | `returns.return_items.unit_price` | Direct mapping. Value already in baht |
| `items[].return_quantity` | decimal | M | `ReturnItemDto.Quantity` | `returns.return_items.quantity` | Direct mapping |
| `items[].return_line_item_price` | decimal | M | `ReturnItemDto.UnitPrice * ReturnItemDto.Quantity` | `returns.return_items.unit_price`, `returns.return_items.quantity` | Computed: `UnitPrice * Quantity`. Value in baht |
| `items[].sale_unit` | string | M | `ReturnItemDto.Uom` | `returns.return_items.uom` | Direct mapping. Expected values: `Each`, `KG` |
| `billing_address` | object | nullable | — | — | Hardcoded: `null` — billing address is nullable for POS/E-Ordering sale sources per spec; OMS always sends `null` |
| `created_at` | datetime | M | — | — | Hardcoded: `DateTime.UtcNow` at dispatch time |
| `created_by` | string | M | — | — | Hardcoded: `"OMS"` |
| `updated_at` | datetime | M | — | — | Hardcoded: `DateTime.UtcNow` at dispatch time |
| `updated_by` | string | M | — | — | Hardcoded: `"OMS"` |

**Spec fields not populated by this builder:**

| External Field | Reason not populated |
|---|---|
| `billing_address.firstname` | `billing_address` is always `null` from OMS; address data is in `order_addresses` but not mapped here |
| `billing_address.lastname` | Same — see above |
| `billing_address.is_company` | Same — see above |
| `billing_address.company_name` | Same — see above |
| `billing_address.tax_id` | Same — see above |
| `billing_address.branch_id` | Same — see above |
| `billing_address.phone_no` | Same — see above |
| `billing_address.building` | Same — see above |
| `billing_address.address_no` | Same — see above |
| `billing_address.floor` | Same — see above |
| `billing_address.room` | Same — see above |
| `billing_address.moo` | Same — see above |
| `billing_address.soi` | Same — see above |
| `billing_address.road` | Same — see above |
| `billing_address.district` | Same — see above |
| `billing_address.subdistrict` | Same — see above |
| `billing_address.province` | Same — see above |
| `billing_address.zipcode` | Same — see above |
| `billing_address.email` | Same — see above |
| `billing_address.is_hq` | Same — see above |

---

## Endpoint Configuration

The following endpoint keys are seeded in `InMemoryStore.SeedEndpointConfigs()`. Auth header names follow the pattern defined in `OutboxEndpointConfig.Headers` and `StaticToken` fields.

| Endpoint Key | Target URL | Auth Type | Auth Detail | Used By Integration |
|---|---|---|---|---|
| `pos.recalculate` | `https://pos.internal/api/recalculate` | StaticToken | Header: `accessToken: pos-access-token`; `refId: ""` | Integration 1 — POS Recalculation |
| `Gateway.out-for-delivery` | `https://Gateway.internal/api/status-update` | StaticToken | `static-Gateway-token` | Integration 2 — Gateway Update Status |
| `Gateway.wave-started` | `https://Gateway.internal/api/status-update` | StaticToken | `static-Gateway-token` (header: `x-api-key`); `x-channel: TWD` | Integration 2b — WaveStarted Forwarding to Gateway |
| `tms.abb-tax-invoice` | `https://tms.internal/api/invoices` | StaticToken | `static-tms-token` | Integration 3 — Tax Invoice (POD flow) |
| `wms.tax-invoice` | `https://wms.internal/api/invoices` | StaticToken | `static-wms-token` | Integration 3 — Tax Invoice (Prepaid flow) |
| `wms.credit-note` | `https://wms.internal/api/credit-notes` | StaticToken | `static-wms-token` | Integration 4 — Credit Note (Prepaid flow) |
| `gateway.abb-invoice` | `https://Gateway.internal/api/invoices` | StaticToken | `static-Gateway-token` | Invoice forwarding to Gateway (all channels) |
| `gateway.credit-note` | `https://Gateway.internal/api/credit-notes` | StaticToken | `static-Gateway-token` | Credit note forwarding to Gateway (all channels) |
| `wms.create-order` | `https://wms.internal/api/orders` | OAuth2ClientCredentials | Token URL: `https://wms.internal/oauth/token`; Client ID: `oms-client` | Order creation (not covered by this document) |
| `tms.pick-confirm` | `https://tms.internal/api/picks` | StaticToken | `static-tms-token` | Pick confirmation (not covered by this document) |
| `tms.pack-confirm` | `https://tms.internal/api/packs` | StaticToken | `static-tms-token` | Pack confirmation (not covered by this document) |
| `wms.cancel-order` | `https://wms.internal/api/orders/cancel` | OAuth2ClientCredentials | Token URL: `https://wms.internal/oauth/token`; Client ID: `oms-client` | Order cancellation (not covered by this document) |
| `tms.cancel-booking` | `https://tms.internal/api/bookings/cancel` | StaticToken | `static-tms-token` | Booking cancellation (not covered by this document) |
| `Gateway.order-cancelled` | `https://Gateway.internal/api/orders/cancel` | StaticToken | `static-Gateway-token` | Order cancellation notification (not covered by this document) |
| `tiktok.order-create` | `https://api.tiktokshop.com/orders` | OAuth2ClientCredentials | Token URL: `https://auth.tiktokshop.com/token`; Client ID: `oms-tiktok` | TikTok marketplace (not covered by this document) |
| `tiktok.pick-confirm` | `https://api.tiktokshop.com/picks` | OAuth2ClientCredentials | Token URL: `https://auth.tiktokshop.com/token`; Client ID: `oms-tiktok` | TikTok marketplace (not covered by this document) |
| `tiktok.awb-notify` | `https://api.tiktokshop.com/awb` | OAuth2ClientCredentials | Token URL: `https://auth.tiktokshop.com/token`; Client ID: `oms-tiktok` | TikTok marketplace (not covered by this document) |
| `lazada.order-create` | `https://api.lazada.com/orders` | OAuth2ClientCredentials | Token URL: `https://auth.lazada.com/token`; Client ID: `oms-lazada` | Lazada marketplace (not covered by this document) |
| `lazada.pick-confirm` | `https://api.lazada.com/picks` | OAuth2ClientCredentials | Token URL: `https://auth.lazada.com/token`; Client ID: `oms-lazada` | Lazada marketplace (not covered by this document) |
| `lazada.pack-confirm` | `https://api.lazada.com/packs` | OAuth2ClientCredentials | Token URL: `https://auth.lazada.com/token`; Client ID: `oms-lazada` | Lazada marketplace (not covered by this document) |

---

## Channel and Sale Source Mapping Reference

The following lookup is applied identically in all four payload builders. It is extracted here as a single reference.

### `ChannelType` → `SaleChannel` / `sale_channel`

| OMS `ChannelType` | External value |
|---|---|
| `Gateway` | `CFW` |
| `App` | `CHEF` |
| `POS` | `CHO` |
| `Marketplace`, `Kiosk`, `POSTerminal`, `BulkImport`, `Web`, `CallCenter`, or any other | `CFW` (fallback) |

### `SubChannel` → `salesource` / `sale_source`

| OMS `SubChannel` | External value |
|---|---|
| `WA` | `WA` |
| `XB` | `XB` |
| `CF` | `CF` |
| `CO` | `CO` |
| `*` or any other value | `WA` (fallback) |
