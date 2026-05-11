# OMS Service Recommendation & ER Diagrams

> Based on analysis of `order-schema-old.sql` and the new OMS domain model (D018/D019).

---

## Service Recommendation

### Should the OMS have multiple services?

**Recommendation: Modular Monolith — 4 bounded modules, 1 deployment, separate schemas.**

Do NOT split into microservices yet. The reasons:

| Factor | Assessment |
|---|---|
| Team size | Likely small — microservices add overhead that hurts small teams |
| Transaction boundary | Order lifecycle, payment, and returns share the same Order aggregate — splitting requires distributed transactions (Saga overhead) |
| Current state | Old system is a single-DB monolith with no clear module boundaries — extract modules before extracting services |
| Order lifecycle | All state transitions need to be atomic — splitting Order and Payment into separate services means 2PC or Saga for every status update |

**The right path:** Clean internal module boundaries now → extract to independent services later if a specific module needs independent scaling.

---

### The 4 Modules

```
┌──────────────────────────────────────────────────────────────┐
│                    Sprint Connect OMS                         │
│                                                              │
│  ┌─────────────┐  ┌─────────────┐  ┌───────────────────┐  │
│  │   Order     │  │   Payment   │  │     Returns       │  │
│  │   Module    │  │   Module    │  │     Module        │  │
│  │  (schema:   │  │  (schema:   │  │   (schema:        │  │
│  │   orders)   │  │   payment)  │  │    returns)       │  │
│  └─────────────┘  └─────────────┘  └───────────────────┘  │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │            Configuration Module (schema: config)     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │        Audit / Logging  (separate DB — not domain)   │   │
│  └─────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
```

| Module | Owns | Old schema tables it replaces |
|---|---|---|
| **Order** | Order lifecycle, OrderLine, Package, DeliverySlot, Outbox | Order, SubOrder, OrderItem, OrderItemFulFillment, PackageTb, PackageInfo, OrderAddress, OrderCustomer, OrderOutboxTb, OrderSagaTb |
| **Payment** | Invoice, PaymentTransaction, CreditNote, Fees, Promotions | OrderPayment, OrderPaymentTransaction, OrderItemPayment, OrderItemAmout, OrderFeeModel, OrderPromotion |
| **Returns** | Return, ReturnItem, Refund | OrderReturn, OrderReturnItem |
| **Configuration** | StoreLocation, BusinessUnit, RolloutPolicy | BuTbl, StoreLocation, AllowedStatusSetting, OrderProcessConditionTb |

---

## Improvements Over Old Schema

| Old Problem | New Design Fix |
|---|---|
| `Order` + `SubOrder` duplication | Single `orders` table — `SubOrder` concept removed; FulfillmentType drives routing |
| `varchar(4)` status codes | Readable `status` enum strings |
| `INT IDENTITY` PKs | `UUID` PKs — distributed-safe, no coupling |
| FK via `SourceOrderId` strings | Proper UUID FK constraints |
| `PackageTb` per SKU per box | `order_packages` per package entity with `order_package_lines` join |
| State machine in `AllowedStatusSetting` config | State machine enforced in Order aggregate code |
| Logging in domain DB | Separate `audit` database |
| `OrderItemFulFillment` per item | `fulfillment_type` and `delivery_slot` at Order level |
| `unit_price` overwritten by POS | `original_unit_price` preserved on `order_lines`; recalculated price in `order_line_amounts` |
| No POS recalculation round tracking | `recalc_round` + `trigger_event` on `order_line_amounts`; multiple rows per line per round |
| No guard against premature TMS dispatch | `pos_recalc_pending` flag on `orders`; blocks `MarkPacked` until POS responds |
| `OrderSagaTb` flat table | Replaced by Outbox pattern (already in D018/D019) |
| No state-change audit trail | `order_status_history` records every domain transition: `from_status`, `to_status`, `changed_by`, `detail`, `changed_at` |
| No inbound event log from external systems | `order_webhook_logs` records each webhook received from WMS/TMS/POS: `source_system`, `event_type`, `detail`, `received_at` |

---

## ER Diagram — Order Module (schema: orders)

```mermaid
erDiagram
    orders {
        uuid order_id PK
        varchar order_number UK
        varchar source_order_id UK
        varchar channel_type
        varchar business_unit
        varchar store_id FK
        timestamptz order_date
        varchar status
        varchar pre_hold_status
        varchar hold_reason
        varchar fulfillment_type
        varchar payment_method
        bool substitution_flag
        bool pos_recalc_pending
        timestamptz created_at
        timestamptz updated_at
        varchar created_by
        varchar updated_by
    }

    order_lines {
        uuid order_line_id PK
        uuid order_id FK
        varchar sku
        nvarchar product_name
        varchar barcode
        decimal requested_amount
        decimal picked_amount
        varchar unit_of_measure
        decimal original_unit_price
        varchar currency
        varchar status
        bool is_substitute
        timestamptz created_at
        timestamptz updated_at
    }

    order_line_substitutions {
        uuid substitution_id PK
        uuid order_line_id FK
        varchar substitute_sku
        nvarchar substitute_product_name
        decimal substitute_unit_price
        decimal substituted_amount
        bool customer_approved
        timestamptz approved_at
        timestamptz created_at
    }

    order_packages {
        uuid package_id PK
        uuid order_id FK
        varchar tracking_id UK
        varchar carrier_package_id
        varchar third_party_logistic
        varchar vehicle_type
        varchar status
        decimal package_weight
        varchar delivery_note_number
        timestamptz created_at
        timestamptz updated_at
    }

    order_package_lines {
        uuid id PK
        uuid package_id FK
        uuid order_line_id FK
    }

    order_addresses {
        uuid address_id PK
        uuid order_id FK
        varchar address_type
        nvarchar first_name
        nvarchar last_name
        nvarchar company_name
        nvarchar address1
        nvarchar address2
        nvarchar subdistrict
        nvarchar district
        nvarchar province
        varchar postal_code
        varchar country_code
        varchar mobile_phone
        varchar email
        varchar latitude
        varchar longitude
        varchar tax_id
        timestamptz created_at
    }

    order_customers {
        uuid order_customer_id PK
        uuid order_id FK
        varchar source_customer_id
        varchar member_type
        varchar member_id
        timestamptz created_at
    }

    delivery_slots {
        uuid slot_id PK
        uuid order_id FK
        varchar store_id FK
        timestamptz scheduled_start
        timestamptz scheduled_end
        timestamptz created_at
        timestamptz updated_at
    }

    order_holds {
        uuid hold_id PK
        uuid order_id FK
        varchar hold_reason
        timestamptz held_at
        timestamptz released_at
        varchar held_by
        varchar released_by
    }

    order_outbox {
        uuid outbox_id PK
        uuid order_id FK
        varchar event_type
        jsonb event_payload
        varchar status
        int retry_count
        timestamptz created_at
        timestamptz published_at
        timestamptz next_retry_at
    }

    order_status_history {
        uuid history_id PK
        uuid order_id FK
        varchar from_status
        varchar to_status
        varchar changed_by
        varchar detail
        timestamptz changed_at
    }

    order_webhook_logs {
        uuid webhook_log_id PK
        uuid order_id
        varchar source_system
        varchar event_type
        varchar detail
        timestamptz received_at
    }

    orders ||--o{ order_lines : "contains"
    orders ||--o{ order_packages : "packed into"
    orders ||--o{ order_addresses : "has"
    orders ||--o| order_customers : "placed by"
    orders ||--o| delivery_slots : "scheduled in"
    orders ||--o{ order_holds : "paused via"
    orders ||--o{ order_outbox : "emits"
    orders ||--o{ order_status_history : "transitions recorded in"
    orders ||--o{ order_webhook_logs : "receives from"
    order_lines ||--o{ order_package_lines : "assigned to"
    order_packages ||--o{ order_package_lines : "contains"
    order_lines ||--o| order_line_substitutions : "replaced by"
```

---

## ER Diagram — Payment Module (schema: payment)

```mermaid
erDiagram
    order_payments {
        uuid payment_id PK
        uuid order_id
        varchar source_payment_id UK
        varchar currency
        varchar status
        timestamptz payment_date
        timestamptz created_at
        timestamptz updated_at
    }

    payment_transactions {
        uuid transaction_id PK
        uuid payment_id FK
        varchar payment_method
        varchar payment_sub_method
        decimal amount
        varchar currency
        varchar source_transaction_id
        varchar bill_address_ref
        timestamptz created_at
    }

    invoices {
        uuid invoice_id PK
        uuid order_id
        varchar invoice_number UK
        varchar invoice_type
        decimal total_amount
        varchar currency
        varchar status
        timestamptz generated_at
        timestamptz created_at
    }

    credit_notes {
        uuid credit_note_id PK
        uuid order_id
        uuid invoice_id FK
        varchar credit_note_number UK
        decimal amount
        varchar currency
        varchar reason
        varchar source
        varchar status
        timestamptz created_at
        timestamptz updated_at
    }

    order_line_amounts {
        uuid amount_id PK
        uuid order_line_id
        int recalc_round
        varchar trigger_event
        decimal recalculated_unit_price
        varchar currency
        decimal gross_amount
        decimal net_amount
        decimal unit_gross_amount
        decimal unit_net_amount
        timestamptz recalculated_at
        timestamptz created_at
    }

    order_line_taxes {
        uuid tax_id PK
        uuid amount_id FK
        varchar tax_type
        varchar tax_description
        decimal amount
        decimal rate
        timestamptz created_at
    }

    order_fees {
        uuid fee_id PK
        uuid order_id
        varchar source_fee_id
        varchar fee_code
        nvarchar fee_name
        varchar fee_type
        decimal amount
        varchar currency
        timestamptz created_at
        timestamptz updated_at
    }

    order_promotions {
        uuid promotion_id PK
        uuid order_id
        uuid order_line_id
        varchar source_promo_id
        varchar promo_code
        nvarchar promo_name
        varchar promo_type
        decimal discount_amount
        decimal discount_percentage
        varchar currency
        timestamptz created_at
    }

    order_payments ||--o{ payment_transactions : "split into"
    invoices ||--o{ credit_notes : "credited by"
    order_line_amounts ||--o{ order_line_taxes : "taxed via"
    order_fees }o--|| order_payments : "paid by"
```

---

## ER Diagram — Returns Module (schema: returns)

```mermaid
erDiagram
    returns {
        uuid return_id PK
        uuid order_id
        varchar return_order_number UK
        varchar invoice_id
        varchar credit_note_id
        varchar status
        varchar goods_receive_no
        varchar return_reason
        timestamptz requested_at
        timestamptz pickup_scheduled_at
        timestamptz picked_up_at
        timestamptz received_at
        timestamptz inspected_at
        timestamptz put_away_at
        timestamptz refunded_at
        timestamptz created_at
        timestamptz updated_at
        varchar created_by
        varchar updated_by
    }

    return_items {
        uuid return_item_id PK
        uuid return_id FK
        uuid order_line_id
        varchar sku
        nvarchar product_name
        varchar barcode
        decimal quantity
        varchar unit_of_measure
        decimal unit_price
        varchar currency
        varchar item_reason
        varchar condition
        varchar put_away_status
        varchar assigned_sloc
        varchar payment_method
        timestamptz inspected_at
        timestamptz put_away_at
        timestamptz created_at
        timestamptz updated_at
    }

    return_put_away_logs {
        uuid log_id PK
        uuid return_id FK
        uuid return_item_id FK
        varchar sku
        varchar assigned_sloc
        varchar condition
        decimal quantity
        varchar performed_by
        timestamptz performed_at
    }

    return_refunds {
        uuid refund_id PK
        uuid return_id FK
        decimal refund_amount
        varchar currency
        varchar refund_method
        varchar status
        varchar reference_no
        timestamptz processed_at
        timestamptz created_at
    }

    returns ||--o{ return_items : "contains"
    returns ||--o{ return_put_away_logs : "audited by"
    returns ||--o| return_refunds : "settled by"
    return_items ||--o{ return_put_away_logs : "tracked in"
```

---

## ER Diagram — Configuration Module (schema: config)

```mermaid
erDiagram
    store_locations {
        uuid store_id PK
        varchar source_bu
        varchar source_loc
        nvarchar store_name
        varchar business_unit FK
        bool is_active
        bool is_rolled_out
        nvarchar address1
        nvarchar address2
        nvarchar subdistrict
        nvarchar district
        nvarchar province
        varchar postal_code
        varchar country_code
        varchar latitude
        varchar longitude
        varchar mobile_phone
        varchar email
        timestamptz created_at
        timestamptz updated_at
    }

    business_units {
        uuid bu_id PK
        varchar bu_code UK
        nvarchar bu_name
        varchar company_code
        nvarchar company_name
        bool is_active
        timestamptz created_at
        timestamptz updated_at
    }

    rollout_policies {
        uuid policy_id PK
        uuid store_id FK
        bool is_rolled_out
        varchar integration_path
        timestamptz effective_from
        timestamptz effective_to
        varchar updated_by
        timestamptz updated_at
    }

    fulfillment_routing_rules {
        uuid rule_id PK
        varchar channel_type
        varchar fulfillment_type
        varchar business_unit
        bool requires_booking
        bool requires_tms
        varchar initial_pick_status
        int priority
        bool is_active
        timestamptz created_at
    }

    notification_templates {
        uuid template_id PK
        varchar template_name UK
        varchar event_type
        varchar channel
        nvarchar subject
        nvarchar body_template
        bool is_active
        timestamptz created_at
        timestamptz updated_at
    }

    business_units ||--o{ store_locations : "has"
    store_locations ||--o{ rollout_policies : "governed by"
```

---

## ER Diagram — Inbound Module (schema: inbound)

> Covers UC21 (Purchase Order Receipt), UC22 (Inter-store Transfer), and UC23 (Damaged Goods Return). These are entirely new tables — no legacy equivalents.

```mermaid
erDiagram
    purchase_orders {
        uuid purchase_order_id PK
        varchar po_number UK
        uuid supplier_id
        uuid store_id FK
        varchar status
        varchar goods_receive_no
        timestamptz created_at
        timestamptz updated_at
        varchar updated_by
    }

    purchase_order_lines {
        uuid po_line_id PK
        uuid purchase_order_id FK
        varchar sku
        int ordered_qty
        int received_qty
        decimal unit_cost
        varchar currency
        varchar condition
        varchar sloc
        timestamptz received_at
        timestamptz put_away_at
    }

    transfer_orders {
        uuid transfer_order_id PK
        varchar transfer_number UK
        uuid source_store_id FK
        uuid dest_store_id FK
        varchar status
        varchar tracking_id
        timestamptz created_at
        timestamptz updated_at
        varchar updated_by
    }

    transfer_order_lines {
        uuid to_line_id PK
        uuid transfer_order_id FK
        varchar sku
        int requested_qty
        int transferred_qty
        timestamptz confirmed_at
    }

    damaged_goods_receipts {
        uuid damaged_receipt_id PK
        uuid order_id FK
        varchar tracking_id
        varchar status
        timestamptz received_at
        timestamptz put_away_at
        varchar updated_by
    }

    damaged_goods_items {
        uuid item_id PK
        uuid damaged_receipt_id FK
        varchar sku
        varchar condition
        varchar sloc
        timestamptz confirmed_at
    }

    purchase_orders ||--o{ purchase_order_lines : "contains"
    transfer_orders ||--o{ transfer_order_lines : "contains"
    damaged_goods_receipts ||--o{ damaged_goods_items : "inspects"
```

---

## Summary: Old vs New

| Concern | Old Schema | New Design |
|---|---|---|
| Order identity | `INT IDENTITY` + `SourceOrderId varchar` | `UUID` PK — no dual identity needed |
| Sub-orders | `Order` + `SubOrder` (duplicated) | Single `orders` table — FulfillmentType handles routing |
| Package | Per-SKU rows in `PackageTb` | `order_packages` entity + `order_package_lines` join |
| Status | `varchar(4)` codes | Readable enum values |
| State machine | `AllowedStatusSetting` config table | Enforced in Order aggregate code |
| Hold/Resume | Not modelled | `order_holds` table + `pre_hold_status` on `orders` |
| Logging | Mixed in domain DB | Separate `audit` DB |
| Fulfillment routing | `OrderProcessConditionTb` + `OrderItemFulFillment` | `fulfillment_routing_rules` config + `FulfillmentRouter` domain service |
| Returns | `OrderReturn` + `OrderReturnItem` (flat) | `returns` module with status + `return_refunds` |
| Outbox | `OrderOutboxTb` (exists, good) | Improved: adds `retry_count`, `next_retry_at`, `jsonb` payload |
| Inbound (PO) | ❌ None | ➕ New: `purchase_orders` + `purchase_order_lines` (UC21) |
| Inbound (Transfer) | ❌ None | ➕ New: `transfer_orders` + `transfer_order_lines` (UC22) |
| Inbound (Damaged) | ❌ None | ➕ New: `damaged_goods_receipts` + `damaged_goods_items` (UC23) |
