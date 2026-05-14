# Sprint Connect OMS — Ubiquitous Language Glossary

**Version:** 2.0

This glossary defines terms that carry a precise, agreed meaning across business and engineering teams. When in doubt, use these definitions — not dictionary definitions or informal usage. Terms in **bold** within a definition refer to other entries in this glossary.

---

## A

**ABB Invoice**
A pre-delivery invoice issued by OMS to WMS for a **Prepaid Order** after **Pick Confirmed** and before **Dispatch**. Short for "Advance Billing Before delivery." The ABB Invoice locks in the final basket amount before the driver leaves the warehouse.

**ABBInvoiceSentToWMS**
An outbox event dispatched to WMS when STS sends the ABB/Tax invoice to OMS. OMS forwards the invoice link to WMS so the warehouse can proceed with dispatch. See **Prepaid Flow** and **STS**.

**Aggregate**
A cluster of domain objects treated as a single unit for data changes. The **Order** is the primary aggregate in OMS. All changes to an order's state must go through the Order aggregate — no direct column updates.

**Amount Recalculation**
See **POS Recalculation**.

**Approved Substitution**
A **Substitution** that the customer has accepted. The original order line remains active at the substitute SKU and price. Triggers a **POS Recalculation** if not already in progress.

**Assigned Sloc**
The specific storage location code (e.g. `A-12`, `REPAIR-BAY-2`) assigned to a product after it is inspected and put away in the warehouse. Short for "storage location."

---

## B

**Barcode**
A product identifier used for warehouse scanning during **Picking**. Stored on the **Order Line** at order creation so picking can proceed without a live catalogue lookup.

**Booking**
The act of reserving a **Delivery Slot** with TMS and CBE before a **Prepaid Order** is created. Booking happens via the customer journey: Customer → GW → PS → TMS → CBE. A booking must succeed before the Sale Order is submitted to OMS.

**Booking Confirmed**
An order status. For **Post-Paid Orders**, WMS sends a webhook confirming it has reserved stock for the order. The order moves from **Pending** to **Booking Confirmed**. For **Prepaid Orders**, this step is skipped — the booking was already made via TMS/CBE before order creation.

**Bounded Context**
A module with a clear boundary that owns its own schema and vocabulary. OMS has five: **Order**, **Payment**, **Returns**, **Config**, and **Inbound**. Terms may mean different things in different bounded contexts — context always clarifies meaning.

**Business Unit (BU)**
An organizational unit (e.g. `CMG`, `CFR`) that owns a set of orders and stores. OMS enforces strict data isolation between BUs — a CMG operator cannot access CFR data. Every order and store belongs to exactly one business unit. The JWT claim `business_unit` is required on all API requests; the application layer filters all queries by this value. Cross-BU mutations return `403 forbidden_business_unit`.

---

## C

**Cancellation**
The act of stopping an order before it reaches the warehouse for dispatch. Cancellation is only allowed from **Pending**, **Booking Confirmed**, or **On Hold**. A cancelled order triggers stock release back to WMS.

**CBE (CHG Backend)**
The system that manages delivery slot availability and booking records. Slot requests from customers are routed through GW → PS → TMS → CBE. Relevant only in the **Prepaid Flow**.

**Channel Type**
Where an order originated: `Gateway`, `Marketplace`, `Kiosk`, `POSTerminal`, `BulkImport`, `Web`, `App`, `POS`, or `CallCenter`. Determines routing rules, notification templates, and outbox routing via `config.outbox_routing_rules`.

**Click & Collect**
A **Fulfillment Type** where the customer places an order online but collects it in person at a store. The order lifecycle ends with **Ready for Collection** → **Collected** rather than **Delivered**.

**Collected**
An order status. The customer has physically collected the order at the store. Applies to **Click & Collect** orders only. Triggers invoice generation.

**Condition**
The physical state of a product assessed by warehouse staff during inspection. Three values: `Resellable` (can go directly back on the shelf), `Repairable` (needs fixing before it can be sold), `Dispose` (written off as a loss).

**Credit Note**
A document issued by STS reducing the amount owed by the customer — for example, for rejected items or price corrections. In OMS the credit note is stored in `payment.credit_notes` keyed by `credit_note_number` from STS. Distinct from an internal financial reversal: STS issues it, OMS records and forwards it. Also see **CreditNoteSentToWMS**.

**CreditNoteSentToWMS**
An outbox event dispatched to WMS when OMS receives a credit note from STS. For prepaid orders, OMS forwards the credit note link to WMS; for post-paid (delivery) orders, OMS forwards it to TMS.

---

## D

**Damaged Goods Receipt**
A warehouse record created when a TMS driver returns a damaged package to the warehouse dock. Links the damaged package back to the original **Order** via **Tracking ID**.

**Delivered**
An order status. TMS has confirmed the package was physically handed to the customer. Triggers **Invoice** generation via outbox → POS.

**DeliveredSentToGW**
An outbox event dispatched to GW when TMS confirms package delivery. Notifies the customer-facing gateway so the customer can receive their delivery confirmation.

**Delivery**
A **Fulfillment Type** where a driver delivers the order to the customer's address during a scheduled **Delivery Slot**.

**Delivery Fee**
An **Order Fee** charged on top of the product total for the cost of home delivery.

**Delivery Slot**
The agreed time window during which a driver will deliver an order — e.g. `14:00–16:00`. For **Prepaid Orders**, the slot is booked via TMS/CBE before order creation. For other orders, it is assigned at order creation and can be rescheduled until the order is **Out for Delivery**.

**Dispatch**
The moment a TMS driver picks up a packed order from the warehouse and begins the delivery journey. Triggers the **Out for Delivery** status transition.

**Domain Event**
A record of something that happened in the domain — e.g. `OrderCreatedEvent`, `PickConfirmedEvent`. Domain events are staged in the **Outbox** and dispatched to external systems.

---

## E

**Express**
A **Fulfillment Type** for accelerated delivery, typically within a shorter time window than standard **Delivery**. Follows the same state machine as Delivery.

---

## F

**Fee**
See **Order Fee**.

**Fulfillment Routing Rule**
A configuration record that determines how an order should be handled based on its **Channel Type**, **Fulfillment Type**, and **Business Unit**. Specifies whether a **Delivery Slot** booking is required and whether a TMS shipment must be created.

**Fulfillment Type**
How an order reaches the customer. Three values: `Delivery`, `Express`, `ClickAndCollect`.

---

## G

**GRN (Goods Receipt Note)**
The warehouse reference number issued when goods physically arrive at the dock. Set by WMS. Recorded on the **Purchase Order** as `goods_receive_no`. Also used on **Returns** when the returned goods arrive at the warehouse.

**GW (CFW Gateway)**
The customer-facing API gateway. All customer requests (slot queries, bookings, sale orders) enter through GW. OMS also sends customer notifications (out for delivery, delivered, tax invoice) back through GW via the **Outbox**.

---

## H

**Hold**
See **On Hold**.

**Hold Reason**
The recorded reason an order was placed **On Hold** — e.g. `ManualReview`, `PackageDamaged`. Stored on the order and cleared when the hold is released.

---

## I

**Idempotency Key**
A UUID sent by external systems in the `X-Idempotency-Key` header on every **Webhook** call. If OMS receives the same key twice, the second request is ignored and the first result is returned. Prevents duplicate processing caused by network retries.

**Inbound**
Goods arriving at the warehouse. Covers **Purchase Orders** (from suppliers), **Transfer Orders** (from other stores), and **Damaged Goods Receipts** (packages returned by drivers). Owned by the `inbound` schema.

**Inspection**
The warehouse activity of examining returned items to assign a **Condition**. Inspection occurs after returned goods are physically received and before **Put Away**.

**Invoice**
A fiscal document recording the amount the customer owes (or paid) for an order. In OMS, `Standard` invoices are issued after **Delivered** or **Collected**; `ABB` invoices are issued before **Dispatch** for **Prepaid Orders**; `TaxInvoice` documents are issued by STS post-delivery.

**Invoiced**
An order status. POS has confirmed a fiscal invoice has been issued to the customer. The order transitions here after **Delivered** or **Collected**.

---

## L

**Line**
See **Order Line**.

---

## M

**MarketplaceAdapter**
The ACL (Anti-Corruption Layer) adapter used by the outbox worker to call marketplace-specific APIs — e.g. TikTok, Lazada — at specific order lifecycle events. The `MarketplaceAdapter` resolves `endpoint_key` values from `config.outbox_routing_rules` to real API URLs and handles marketplace authentication and payload transformation. Different marketplace BUs receive different events; routing is driven by the `outbox_routing_rules` table.

**Modular Monolith**
The architectural style of OMS. All bounded contexts (Order, Payment, Returns, Config, Inbound) are deployed as a single application unit but each owns a separate database schema with no cross-schema JOINs. This enables clear domain boundaries while avoiding distributed systems complexity.

---

## O

**On Hold**
An order status indicating the order is paused and requires manual intervention or is awaiting damage assessment. The order's previous status is saved as `pre_hold_status` and restored when the hold is released. Can be triggered manually (supervisor) or automatically (damaged package).

**OMS (Order Management System)**
This system. Also referred to as **Sprint Connect** or **SC** in sequence diagrams and event names.

**Order**
The central business object representing a customer's request for goods. An order has a lifecycle managed by the **Order State Machine**, owns one or more **Order Lines**, and is fulfilled via **WMS**, **TMS**, and **POS**.

**Order Fee**
An additional charge on top of product totals — e.g. delivery fee, service charge, platform fee. Stored in `payment.order_fees`. Displayed on the receipt.

**Order Line**
One product item within an order. Stores the original requested quantity and the picked quantity (set when WMS confirms picking). A line can be **Voided** if the customer rejects a **Substitution**.

**Order Number**
The human-readable reference for an order shown to customers and staff — e.g. `ORD-001`. Generated by OMS. Distinct from **Source Order ID**.

**Order State Machine**
The set of allowed status transitions for an order. Enforced as an invariant — no direct status writes are allowed outside the state machine. Invalid transitions return a `409 invalid_transition` error.

**Outbox**
A staging table (`orders.order_outbox`) where domain events are written atomically with the domain state change in the same database transaction. A background worker reads pending rows and dispatches them to external systems. This guarantees events are never lost even if the dispatcher crashes.

**Out for Delivery**
An order status. The TMS driver has collected the package and is travelling to the customer's address.

**OutForDeliverySentToGW**
An outbox event dispatched to GW when the package is dispatched by TMS (Out for Delivery transition). Notifies the customer-facing gateway so the customer receives an in-transit update.

---

## P

**Package**
A physical box or parcel containing one or more **Order Lines**, assigned a **Tracking ID** by WMS when packed. An order may be split into multiple packages.

**Packed**
An order status. WMS has confirmed all items are packed into packages with tracking IDs assigned.

**Paid**
An order status. POS has confirmed payment was received from the customer. Terminal status — no further transitions occur.

**Payment Method**
How the customer pays for an order: `Prepaid` (paid before delivery), `PostPaid` (paid after delivery), `CreditCard`.

**Partial Item Return**
A post-delivery return scenario where the customer rejects a subset of order line items — for example, the customer keeps the beef but rejects the chicken because it was not fresh. Only the rejected items are returned. Triggers a partial refund for those items and a POS Recalculation to remove their value from the order total. See also **Credit Note**.

**Partial Pick**
A fulfillment scenario where WMS picks fewer units than ordered for one or more lines, due to stock unavailability or quality issues. The actual quantity is recorded as `picked_quantity` on `orders.order_pick_lines`, and a `shortfall_reason` is captured. After partial pick, a **POS Recalculation** is triggered to adjust the order total.

**Pick Confirmed**
An order status. WMS has reported the actual quantities picked for each order line. If any quantity differs from what was ordered, or if a **Substitution** was proposed, a **POS Recalculation** is triggered.

**PickConfirmedSentToGW**
An outbox event dispatched to GW after **Pick Confirmed**. Notifies the Gateway so downstream customer-facing systems can reflect the picking result.

**PickConfirmedSentToTMS**
An outbox event dispatched to TMS after **Pick Confirmed**. Used in the prepaid flow so TMS can prepare the driver dispatch and create the shipment.

**Pick Started**
An order status. WMS has confirmed that a warehouse picker has begun collecting items from the shelves.

**Picker**
The warehouse staff member physically collecting items from shelves during the **Picking** process.

**Picking**
The warehouse activity of locating and collecting products from shelves to fulfill an order. Begins when WMS sends the **Pick Started** webhook and ends with **Pick Confirmed**.

**Post-Paid Order**
An order where payment is collected after delivery. The standard flow: Pending → Booking Confirmed → Pick Started → … → Delivered → Invoiced → Paid.

**POS (Point of Sale)**
The external system responsible for price calculation, promotion application, and invoice issuance. OMS sends `PickConfirmedEvent` to POS via the **Outbox** to trigger recalculation. POS responds with the adjusted total via the `recalculation-result` webhook.

**POS Recalculation**
The process where POS recalculates the order total based on actual picked quantities and applies promotions. Triggered automatically after **Pick Confirmed** if quantities changed or a **Substitution** was made. For **Prepaid Orders**, WMS can trigger this multiple times mid-pick. Blocked by `pos_recalc_pending = true` flag — packing cannot proceed until POS confirms the final price.

**pos_recalc_pending**
A flag on the **Order** (`true`/`false`). Set to `true` when a recalculation is requested. Set back to `false` when POS confirms the result. The order cannot transition to **Packed** while this is `true`.

**pre_hold_status**
The order status saved immediately before transitioning to **On Hold**. Restored exactly when the hold is released. Allows the order to resume at the correct point in the lifecycle.

**Prepaid Flow**
The order lifecycle for **Prepaid Orders**. Key differences from the standard flow: (1) the **Delivery Slot** is booked via TMS/CBE before order creation, so the **Booking Confirmed** step is skipped; (2) an **ABB Invoice** is issued to WMS before **Dispatch**; (3) an official **Tax Invoice** is sent by STS after **Delivered**.

**Prepaid Order**
An order where the customer pays before delivery. The delivery slot is pre-booked via TMS/CBE. An **ABB Invoice** is issued before dispatch. STS issues the official tax invoice post-delivery.

**Promotion**
A discount applied to an order or specific order line by POS. Types include `PercentageDiscount`, `FixedDiscount`, `BuyXGetY`, `FreeGift`. Stored in `payment.order_promotions` and recorded per **POS Recalculation** round.

**Proof of Delivery (PoD)**
A URL or reference to evidence (e.g. a photo) that a package was delivered. Sent by TMS in the **Package Delivered** webhook and stored in the webhook log as an audit record.

**PS (Proxy Service)**
An internal routing proxy that sits between **GW** and backend systems. Acts as a pass-through for slot requests and booking calls. Does not have a direct role in OMS order processing after the booking is made.

**Purchase Order (PO)**
A record in OMS tracking expected goods from a supplier. Created by ERP/JDA or an operator. Progresses through `Created → PartiallyReceived → FullyReceived → Closed` as goods arrive and are shelved.

**Put Away**
The warehouse activity of placing inspected items on a shelf, repair bay, or disposal location after they are received. For **Returns**, put-away triggers atomic creation of a **Refund** and **Credit Note**. For inbound POs, put-away closes the PO and signals stock availability.

---

## R

**Ready for Collection**
An order status. POS has confirmed a **Click & Collect** order is packed and waiting at the store counter for the customer.

**Recalculation Round**
A numbered iteration of **POS Recalculation**. Stored as `recalc_round` on `order_line_amounts`. Round 1 is triggered by **Pick Confirmed**. Subsequent rounds are triggered by additional substitutions or quantity changes (common in **Prepaid Orders**).

**Refund**
The financial record of money being returned to a customer after a **Return** is completed. Created atomically when **Put Away** is confirmed. Refund methods: `CreditCard`, `BankTransfer`, `StoreCredit`.

**Rejected Substitution**
A **Substitution** that the customer has declined. The original **Order Line** is voided. A **POS Recalculation** is triggered to remove the line from the total.

**Rescheduler**
The capability for a customer or operator to change a **Delivery Slot** after the order is created but before it reaches **Out for Delivery**. A reschedule call updates `delivery_slots.scheduled_start` and `scheduled_end` and notifies TMS via outbox.

**Return**
A customer request to send purchased items back to the warehouse. Initiated after **Delivered**, **Invoiced**, or **Paid**. Return lifecycle: `Requested → Pickup Scheduled → Picked Up → Received → Inspected → Put Away → Refunded`.

**Return Order Number**
The human-readable reference for a return — e.g. `RET-001`. Shown to staff and customers.

**Rollout Policy**
A configuration record controlling whether OMS is live at a given store (`Full`) or the store still uses the legacy system (`LegacyFallback`).

---

## S

**Sale Order**
The outbound message sent from OMS to CBE and WMS after an **Order** is created. Registers the order for payment processing (CBE) and warehouse picking (WMS).

**SaleOrderSentToTMS**
An outbox event dispatched to TMS at order creation, used in the prepaid flow. Notifies TMS early so a driver booking can be prepared in parallel with warehouse picking.

**SC (Sprint Connect)**
See **OMS**.

**Sloc**
See **Assigned Sloc**.

**Source Order ID**
The order reference from the originating external system (app, POS, e-commerce platform). Used for idempotent order creation — submitting the same `source_order_id` twice returns the existing order rather than creating a duplicate.

**STS (Settlement Tax System)**
The external system that issues official ABB/Tax invoices post-delivery for **Prepaid Orders**. STS sends the invoice to OMS via webhook; OMS forwards it to the customer through **GW**.

**Status History**
The append-only log of every status transition an order has gone through. Used to build the **Timeline** and for audit purposes.

**Stock Ledger**
A read-only view of stock movement events OMS has recorded for a SKU across locations. Sourced from Purchase Order put-away, Transfer Order movements, and order pick confirmations. OMS does not own inventory counts — this reflects events, not live WMS stock levels.

**Substitution**
When a warehouse picker cannot find the original SKU and proposes an alternative product. WMS sends a `substitution-offered` webhook. The customer must approve or reject the substitution. Sets `substitution_flag = true` and `pos_recalc_pending = true` on the order.

**Substitution Flag**
A boolean on the **Order** (`substitution_flag`). Set to `true` when any substitution record is created. Never reset. Signals to downstream systems that at least one line was substituted.

---

## T

**Tax Invoice**
See **Invoice**. The `TaxInvoice` type is specifically issued by **STS** post-delivery for **Prepaid Orders** and forwarded to the customer via **GW**.

**Timeline**
The chronological list of events for an order, combining **Status History** (domain transitions), **Webhook Logs** (callbacks from external systems), and **Outbox** entries (dispatched domain events). Returned by `GET /orders/{id}/timeline`.

**TMS (Transport Management System)**
The external system responsible for driver dispatch and delivery tracking. OMS sends `PickConfirmedEvent` to TMS after picking (and pre-delivery invoicing for **Prepaid Orders**). TMS sends back `PackageDispatched` and `PackageDelivered` webhooks.

**Tracking ID**
The TMS identifier assigned to a **Package** — e.g. `TRK-2024-001`. Used to match incoming TMS webhooks back to the correct order. Also used on **Transfer Orders** to track in-transit stock.

**Transfer Order (TO)**
A record in OMS tracking stock movement from one store or DC to another. Progresses through `Created → Pick Confirmed → In Transit → Received → Completed`.

---

## U

**Unit of Measure**
How a product is counted or weighed: `Each`, `Kg`, `Litre`. Determines how quantities are reported and picked.

---

## V

**Voided Line**
An **Order Line** whose status has been set to `Voided` because the customer rejected a **Substitution**. The line is excluded from the final total. Triggers a **POS Recalculation**.

---

## W

**WaveStarted**
A domain event raised when WMS initiates an internal picking wave for an order. A wave groups one or more orders for a single picking run. OMS records this event in `orders.order_wave_events` and stages a **WaveStartedSentToGW** outbox event for opted-in Gateways.

**WaveStartedSentToGW**
An outbox event dispatched to Gateway when WMS starts a picking wave. Only dispatched for Gateways that have a matching row in `config.outbox_routing_rules` (i.e. opted-in to this event). Gateways without a routing rule receive no notification.

**Webhook**
An inbound HTTP callback from an external system (WMS, TMS, or POS) notifying OMS of an event. All webhook endpoints return `202 Accepted` immediately. Each webhook is logged in `order_webhook_logs` with an **Idempotency Key** to prevent duplicate processing.

**Webhook Log**
The record of an inbound **Webhook** call. Stored in `orders.order_webhook_logs`. Includes the raw payload, idempotency key, and a human-readable detail string. Used for debugging and the order **Timeline**.

**WMS (Warehouse Management System)**
The external system that physically manages stock at the warehouse. WMS sends webhooks to OMS for every key picking event (Pick Started, Pick Confirmed, Packed, Substitution Offered) and receives domain events via the **Outbox** (Sale Order, Pick Started notification, etc.).

---

## Bounded Context Glossary Map

Some terms appear in multiple bounded contexts with related but distinct meanings:

| Term | In Order context | In Payment context | In Returns context |
|---|---|---|---|
| **Amount** | Requested or picked quantity on an Order Line | Recalculated unit price from POS | Refund amount back to customer |
| **Status** | Order lifecycle stage (Pending → Paid) | Invoice status (Generated → Issued) | Return stage (Requested → Refunded) |
| **Invoice** | Referenced by `invoice_id` FK | The authoritative invoice record | The invoice being reversed by a credit note |
| **Line** | An order line (one SKU) | A line amount record per recalc round | A return item (one returned SKU) |
| **Condition** | Not used | Not used | Physical state of a returned item (Resellable / Repairable / Dispose) |
| **Credit Note** | Not used | STS-issued document reducing customer's balance; stored in `payment.credit_notes` | Triggers a partial refund when a return is put away |
| **Wave** | Not used | Not used | Not used (picking wave is an Order context concept) |
