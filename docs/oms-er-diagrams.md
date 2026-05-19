# Sprint Connect OMS — ER Diagrams

**Version:** 2.0  
**Architecture:** Modular Monolith — 5 bounded contexts, each owning its own MySQL schema.

---

## Module 1 — Order (schema: `orders`)

Core aggregate. Owns the order lifecycle state machine, delivery slots, packages, outbox, and all audit logs.

> `payment_flow` on the `orders` entity drives STS outbox routing. `PRE_PAID` orders forward the ABB/Tax Invoice to WMS + Gateway after `PickConfirmed`; `PAY_ON_DELIVERY` orders forward it to TMS + Gateway after the `Delivered` event. Credit notes follow the same split. No new tables are required for POD — routing is handled by existing `config.outbox_routing_rules` rows keyed on `(trigger_event, payment_flow)` for each POD outbox event.

```mermaid
erDiagram
    orders {
        bigint  order_id PK
        varchar order_number UK
        varchar source_order_id
        varchar channel_type
        varchar business_unit
        bigint  store_id FK
        varchar fulfillment_type
        varchar status
        varchar pre_hold_status
        varchar hold_reason
        bool    substitution_flag
        varchar payment_flow
        timestamptz created_at
        timestamptz updated_at
        varchar created_by
        varchar updated_by
    }

    order_lines {
        bigint  order_line_id PK
        bigint  order_id FK
        varchar sku
        nvarchar product_name
        varchar barcode
        varchar unit_of_measure
        decimal requested_amount
        decimal picked_amount
        int     picked_quantity
        int     shortfall_quantity
        enum    shortfall_reason
        decimal original_unit_price
        varchar status
        timestamptz created_at
        timestamptz updated_at
    }

    order_line_substitutions {
        bigint  substitution_id PK
        bigint  order_line_id FK
        varchar substitute_sku
        nvarchar substitute_product_name
        decimal substitute_unit_price
        decimal substituted_amount
        bool    customer_approved
        timestamptz approved_at
        timestamptz created_at
    }

    order_packages {
        bigint  package_id PK
        bigint  order_id FK
        varchar tracking_id
        varchar vehicle_type
        decimal package_weight
        varchar status
        timestamptz created_at
        timestamptz updated_at
    }

    order_package_lines {
        bigint  package_line_id PK
        bigint  package_id FK
        bigint  order_line_id FK
    }

    order_addresses {
        bigint  address_id PK
        bigint  order_id FK
        varchar address_type
        varchar first_name
        varchar last_name
        varchar mobile_phone
        varchar email
        nvarchar address1
        varchar subdistrict
        varchar district
        varchar province
        varchar postal_code
    }

    order_customers {
        bigint  customer_id PK
        bigint  order_id FK
        varchar external_customer_id
        varchar name
        varchar email
        varchar phone
    }

    delivery_slots {
        bigint  slot_id PK
        bigint  order_id FK
        bigint  store_id FK
        timestamptz scheduled_start
        timestamptz scheduled_end
        varchar booked_via
        varchar booking_ref
        timestamptz created_at
        timestamptz updated_at
    }

    order_holds {
        bigint  hold_id PK
        bigint  order_id FK
        varchar hold_reason
        timestamptz held_at
        varchar held_by
        timestamptz released_at
        varchar released_by
    }

    order_outbox {
        bigint  outbox_id PK
        bigint  order_id FK
        varchar event_type
        jsonb   event_payload
        varchar status
        timestamptz created_at
        timestamptz published_at
    }

    order_status_history {
        bigint  history_id PK
        bigint  order_id FK
        varchar from_status
        varchar to_status
        timestamptz changed_at
        varchar changed_by
        nvarchar detail
    }

    order_webhook_logs {
        bigint  webhook_log_id PK
        bigint  order_id FK
        varchar source_system
        varchar event_type
        nvarchar detail
        jsonb   raw_payload
        varchar idempotency_key UK
        timestamptz received_at
    }

    order_wave_events {
        bigint  id PK
        bigint  order_id FK
        varchar wave_id
        datetime started_at
        varchar idempotency_key UK
        datetime created_at
    }

    orders       ||--o{ order_lines             : "has"
    orders       ||--o{ order_packages          : "has"
    orders       ||--o{ order_addresses         : "has"
    orders       ||--o|  order_customers        : "has"
    orders       ||--o|  delivery_slots         : "has"
    orders       ||--o{ order_holds             : "has"
    orders       ||--o{ order_outbox            : "stages"
    orders       ||--o{ order_status_history    : "logs"
    orders       ||--o{ order_webhook_logs      : "logs"
    orders       ||--o{ order_wave_events       : "records"
    order_lines  ||--o{ order_line_substitutions: "has"
    order_packages ||--o{ order_package_lines   : "contains"
    order_lines    ||--o{ order_package_lines   : "in"
```

---

## Module 2 — Payment (schema: `payment`)

Tracks all financial records per order: invoices, credit notes, recalculated line amounts, taxes, fees, and promotions.

```mermaid
erDiagram
    order_payments {
        bigint  payment_id PK
        bigint  order_id
        varchar payment_method
        decimal total_amount
        varchar currency
        varchar status
        timestamptz created_at
        timestamptz updated_at
    }

    payment_transactions {
        bigint  transaction_id PK
        bigint  payment_id FK
        decimal amount
        varchar currency
        varchar payment_method
        varchar gateway_ref
        timestamptz created_at
    }

    invoices {
        bigint  invoice_id PK
        bigint  order_id
        varchar invoice_number UK
        varchar invoice_type
        decimal total_amount
        varchar currency
        varchar status
        varchar invoice_link
        varchar source_sts_ref
        timestamptz generated_at
        timestamptz issued_at
    }

    credit_notes {
        bigint  id PK
        bigint  order_id
        bigint  invoice_id FK
        varchar credit_note_number UK
        bigint  credit_amount
        decimal amount
        varchar currency
        varchar reason
        varchar status
        varchar credit_note_link
        varchar source_sts_ref
        datetime issued_at
        datetime received_at
        varchar idempotency_key UK
        datetime created_at
    }

    order_line_amounts {
        bigint  amount_id PK
        bigint  order_line_id FK
        int     recalc_round
        varchar trigger_event
        decimal original_unit_price
        decimal recalculated_unit_price
        decimal unit_net_amount
        timestamptz recalculated_at
        timestamptz created_at
    }

    order_line_taxes {
        bigint  tax_id PK
        bigint  amount_id FK
        varchar tax_type
        varchar tax_description
        decimal amount
        decimal rate
        timestamptz created_at
    }

    order_fees {
        bigint  fee_id PK
        bigint  order_id
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
        bigint  promotion_id PK
        bigint  order_id
        bigint  order_line_id
        varchar source_promo_id
        varchar promo_code
        nvarchar promo_name
        varchar promo_type
        decimal discount_amount
        decimal discount_percentage
        varchar currency
        timestamptz created_at
    }

    order_payments    ||--o{ payment_transactions  : "has"
    invoices          ||--o{ credit_notes          : "reversed by"
    order_line_amounts ||--o{ order_line_taxes     : "broken down by"
```

---

## Module 3 — Returns (schema: `returns`)

Tracks customer return requests from initiation through inspection, put-away, and refund.

```mermaid
erDiagram
    returns {
        bigint  return_id PK
        bigint  order_id
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
        bigint  return_item_id PK
        bigint  return_id FK
        bigint  order_line_id
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
        bigint  log_id PK
        bigint  return_id FK
        bigint  return_item_id FK
        varchar sku
        varchar assigned_sloc
        varchar condition
        decimal quantity
        varchar performed_by
        timestamptz performed_at
    }

    return_refunds {
        bigint  refund_id PK
        bigint  return_id FK
        decimal refund_amount
        varchar currency
        varchar refund_method
        varchar status
        varchar reference_no
        timestamptz processed_at
        timestamptz created_at
    }

    returns          ||--o{ return_items          : "contains"
    returns          ||--o{ return_put_away_logs  : "audited by"
    returns          ||--o|  return_refunds       : "settled by"
    return_items     ||--o{ return_put_away_logs  : "logged in"
```

---

## Module 4 — Configuration (schema: `config`)

Master data and business rules referenced by all other modules.

```mermaid
erDiagram
    store_locations {
        bigint  store_id PK
        varchar source_bu
        varchar source_loc
        nvarchar store_name
        varchar business_unit FK
        bool    is_active
        bool    is_rolled_out
        nvarchar address1
        varchar subdistrict
        varchar district
        varchar province
        varchar postal_code
        decimal latitude
        decimal longitude
        varchar mobile_phone
        varchar email
        timestamptz created_at
        timestamptz updated_at
    }

    business_units {
        bigint  bu_id PK
        varchar bu_code UK
        nvarchar bu_name
        varchar company_code
        nvarchar company_name
        bool    is_active
        timestamptz created_at
        timestamptz updated_at
    }

    rollout_policies {
        bigint  policy_id PK
        bigint  store_id FK
        bool    is_rolled_out
        varchar integration_path
        timestamptz effective_from
        timestamptz effective_to
        varchar updated_by
        timestamptz updated_at
    }

    fulfillment_routing_rules {
        bigint  rule_id PK
        varchar channel_type
        varchar fulfillment_type
        varchar business_unit
        bool    requires_booking
        bool    requires_tms
        varchar initial_pick_status
        int     priority
        bool    is_active
        timestamptz created_at
    }

    notification_templates {
        bigint  template_id PK
        varchar template_name UK
        varchar event_type
        varchar channel
        nvarchar subject
        nvarchar body_template
        bool    is_active
        timestamptz created_at
        timestamptz updated_at
    }

    outbox_routing_rules {
        bigint  rule_id PK
        varchar channel_type
        varchar business_unit
        varchar trigger_event
        varchar target_system
        varchar endpoint_key
        int     execution_order
        bool    is_active
        timestamptz created_at
    }

    store_locations    }o--|| business_units         : "belongs to"
    store_locations    ||--o{ rollout_policies        : "governed by"
    business_units     ||--o{ outbox_routing_rules    : "routes via"
```

---

## Module 5 — Inbound (schema: `inbound`)

Goods arriving at the warehouse from suppliers (POs), from other stores (Transfer Orders), or damaged packages returned by drivers.

```mermaid
erDiagram
    purchase_orders {
        bigint  purchase_order_id PK
        varchar po_number UK
        bigint  supplier_id
        bigint  store_id FK
        varchar status
        varchar goods_receive_no
        timestamptz created_at
        timestamptz updated_at
        varchar updated_by
    }

    purchase_order_lines {
        bigint  po_line_id PK
        bigint  purchase_order_id FK
        varchar sku
        int     ordered_qty
        int     received_qty
        decimal unit_cost
        varchar currency
        varchar condition
        varchar sloc
        timestamptz received_at
        timestamptz put_away_at
    }

    transfer_orders {
        bigint  transfer_order_id PK
        varchar transfer_number UK
        bigint  source_store_id FK
        bigint  dest_store_id FK
        varchar status
        varchar tracking_id
        timestamptz created_at
        timestamptz updated_at
        varchar updated_by
    }

    transfer_order_lines {
        bigint  to_line_id PK
        bigint  transfer_order_id FK
        varchar sku
        int     requested_qty
        int     transferred_qty
        timestamptz confirmed_at
    }

    damaged_goods_receipts {
        bigint  damaged_receipt_id PK
        bigint  order_id FK
        varchar tracking_id
        varchar status
        timestamptz received_at
        timestamptz put_away_at
        varchar updated_by
    }

    damaged_goods_items {
        bigint  item_id PK
        bigint  damaged_receipt_id FK
        varchar sku
        varchar condition
        varchar sloc
        decimal quantity
        timestamptz confirmed_at
    }

    purchase_orders        ||--o{ purchase_order_lines    : "has"
    transfer_orders        ||--o{ transfer_order_lines    : "has"
    damaged_goods_receipts ||--o{ damaged_goods_items     : "contains"
```

---

## Cross-Module References

| From module | Column | To module | References |
|---|---|---|---|
| `orders.orders` | `store_id` | `config` | `store_locations.store_id` |
| `orders.delivery_slots` | `store_id` | `config` | `store_locations.store_id` |
| `inbound.purchase_orders` | `store_id` | `config` | `store_locations.store_id` |
| `inbound.transfer_orders` | `source_store_id` | `config` | `store_locations.store_id` |
| `inbound.transfer_orders` | `dest_store_id` | `config` | `store_locations.store_id` |
| `returns.returns` | `order_id` | `orders` | `orders.order_id` |
| `returns.return_items` | `order_line_id` | `orders` | `order_lines.order_line_id` |
| `payment.invoices` | `order_id` | `orders` | `orders.order_id` |
| `payment.order_line_amounts` | `order_line_id` | `orders` | `order_lines.order_line_id` |
| `inbound.damaged_goods_receipts` | `order_id` | `orders` | `orders.order_id` |
| `orders.order_wave_events` | `order_id` | `orders` | `orders.order_id` |
| `payment.credit_notes` | `order_id` | `orders` | `orders.order_id` |

> Cross-module references are enforced at the application layer (not as foreign key constraints across schemas) to preserve bounded context isolation.

---

## Architecture Decision: Modular Monolith

| Criterion | Chosen Approach |
|---|---|
| Deployment | Single deployable unit (modular monolith) |
| Schema isolation | Each context owns one MySQL schema; no cross-schema JOINs |
| Event propagation | Outbox pattern — events are staged in `orders.order_outbox` and dispatched asynchronously |
| Cross-module consistency | Eventual consistency via outbox; no distributed transactions |
| Future migration path | Each module can be extracted to a microservice independently |
