# OMS E2E Use Case Sequence Diagrams

This document contains one Mermaid sequence diagram per end-to-end use case defined in `cypress/e2e/`. Each diagram title matches the Cypress `describe()` label exactly. Actors are consistent across all diagrams: `Customer` (end customer), `GW` (CFW Gateway), `OMS` (Sprint Connect OMS), `WMS` (Warehouse System), `TMS` (Transport System), `POS` (Point of Sale), `STS` (Settlement Tax System). All monetary values are in satang (smallest THB unit).

---

## UC1 — Web / CMG / Prepaid full order flow

```mermaid
sequenceDiagram
  title: UC1 — Web / CMG / Prepaid full order flow

  participant Customer
  participant GW
  participant OMS
  participant WMS
  participant TMS
  participant POS
  participant STS

  Customer->>GW: Place Prepaid order (channelType=Web, BU=CMG)
  GW->>OMS: POST /orders
  OMS-->>GW: 201 { orderId, status: "Pending", paymentMethod: "Prepaid", businessUnit: "CMG" }
  Note over OMS: State: Pending (Prepaid skips BookingConfirmed)

  WMS->>OMS: POST /webhooks/wms/pick-started { orderId, pickerId }
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickStarted" }
  Note over OMS: State: PickStarted

  WMS->>OMS: POST /webhooks/wms/wave-started { orderId, waveId }
  OMS-->>WMS: 202 { accepted: true, orderId }
  Note over OMS,GW: Outbox: WaveStartedSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/recalculation-requested { orderId, reason: "PickQuantityDiffers" }
  OMS->>POS: Outbound recalculation call (synchronous)
  POS-->>OMS: adjustedAmount
  OMS-->>WMS: 202 { accepted: true, adjustedAmount }

  WMS->>OMS: POST /webhooks/wms/pick-confirmed { orderId, lines[{orderLineId, sku, pickedQty}] }
  OMS->>POS: Internal POS call
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickConfirmed" }
  Note over OMS: State: PickConfirmed

  STS->>OMS: POST /webhooks/sts/abb-tax-invoice-received { orderId, invoiceNumber, invoiceAmount, invoiceLink }
  OMS-->>STS: 202 { accepted: true }
  Note over OMS: State remains PickConfirmed — invoice does not advance state machine
  Note over OMS,WMS: Outbox: ABBInvoiceSentToWMS → WMS
  Note over OMS,GW: Outbox: ABBInvoiceSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/packed { orderId, packages[{trackingId, vehicleType, weight, lineIds}] }
  OMS-->>WMS: 202 { accepted: true, newStatus: "Packed" }
  Note over OMS: State: Packed

  TMS->>OMS: POST /webhooks/tms/package-dispatched { trackingId, dispatchedAt }
  OMS-->>TMS: 202 { newOrderStatus: "OutForDelivery" }
  Note over OMS: State: OutForDelivery

  TMS->>OMS: POST /webhooks/tms/package-delivered { trackingId, deliveredAt, recipientName }
  OMS-->>TMS: 202 { newStatus: "Delivered", invoiceTriggered: true }
  Note over OMS: State: Delivered (terminal)

  GW->>OMS: GET /orders/{orderId}
  OMS-->>GW: 200 { status: "Delivered", businessUnit: "CMG" }
```

---

## UC2 — Web / CFR / Prepaid full order flow

```mermaid
sequenceDiagram
  title: UC2 — Web / CFR / Prepaid full order flow

  participant Customer
  participant GW
  participant OMS
  participant WMS
  participant TMS
  participant POS
  participant STS

  Customer->>GW: Place Prepaid order (channelType=Web, BU=CFR)
  GW->>OMS: POST /orders
  OMS-->>GW: 201 { orderId, status: "Pending", paymentMethod: "Prepaid", businessUnit: "CFR" }
  Note over OMS: State: Pending (Prepaid skips BookingConfirmed)

  WMS->>OMS: POST /webhooks/wms/pick-started { orderId, pickerId }
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickStarted" }
  Note over OMS: State: PickStarted

  WMS->>OMS: POST /webhooks/wms/wave-started { orderId, waveId }
  OMS-->>WMS: 202 { accepted: true, orderId }
  Note over OMS,GW: Outbox: WaveStartedSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/recalculation-requested { orderId, reason: "PickQuantityDiffers" }
  OMS->>POS: Outbound recalculation call (synchronous)
  POS-->>OMS: adjustedAmount
  OMS-->>WMS: 202 { accepted: true, adjustedAmount }

  WMS->>OMS: POST /webhooks/wms/pick-confirmed { orderId, lines[{orderLineId, sku, pickedQty}] }
  OMS->>POS: Internal POS call
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickConfirmed" }
  Note over OMS: State: PickConfirmed

  STS->>OMS: POST /webhooks/sts/abb-tax-invoice-received { orderId, invoiceNumber, invoiceAmount, invoiceLink }
  OMS-->>STS: 202 { accepted: true }
  Note over OMS: State remains PickConfirmed
  Note over OMS,WMS: Outbox: ABBInvoiceSentToWMS → WMS
  Note over OMS,GW: Outbox: ABBInvoiceSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/packed { orderId, packages[{trackingId, vehicleType, weight, lineIds}] }
  OMS-->>WMS: 202 { accepted: true, newStatus: "Packed" }
  Note over OMS: State: Packed

  TMS->>OMS: POST /webhooks/tms/package-dispatched { trackingId, dispatchedAt }
  OMS-->>TMS: 202 { newOrderStatus: "OutForDelivery" }
  Note over OMS: State: OutForDelivery

  TMS->>OMS: POST /webhooks/tms/package-delivered { trackingId, deliveredAt, recipientName }
  OMS-->>TMS: 202 { newStatus: "Delivered", invoiceTriggered: true }
  Note over OMS: State: Delivered (terminal)

  GW->>OMS: GET /orders/{orderId}
  OMS-->>GW: 200 { status: "Delivered", businessUnit: "CFR" }
  Note over OMS,GW: BU isolation invariant: CMG operators cannot access CFR orders (403)
```

---

## UC3 — TikTok Marketplace / CMG / Prepaid — AWB retrieval after OutForDelivery

```mermaid
sequenceDiagram
  title: UC3 — TikTok Marketplace / CMG / Prepaid — AWB retrieval after OutForDelivery

  participant Customer
  participant GW
  participant OMS
  participant WMS
  participant TMS
  participant POS
  participant STS

  Customer->>GW: Place Prepaid order (channelType=Marketplace, subChannel=TikTok, BU=CMG)
  GW->>OMS: POST /orders
  OMS-->>GW: 201 { orderId, status: "Pending", channelType: "Marketplace", businessUnit: "CMG", paymentMethod: "Prepaid" }
  Note over OMS: State: Pending

  WMS->>OMS: POST /webhooks/wms/pick-started { orderId, pickerId }
  OMS-->>WMS: 202 { newStatus: "PickStarted" }
  Note over OMS: State: PickStarted

  WMS->>OMS: POST /webhooks/wms/wave-started { orderId, waveId }
  OMS-->>WMS: 202 { accepted: true, orderId }
  Note over OMS,GW: Outbox: WaveStartedSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/recalculation-requested { orderId, reason: "PickQuantityDiffers" }
  OMS->>POS: Outbound recalculation call (synchronous)
  POS-->>OMS: adjustedAmount
  OMS-->>WMS: 202 { accepted: true, adjustedAmount }

  WMS->>OMS: POST /webhooks/wms/pick-confirmed { orderId, lines[{orderLineId, sku, pickedQty}] }
  OMS->>POS: Internal POS call
  OMS-->>WMS: 202 { newStatus: "PickConfirmed" }
  Note over OMS: State: PickConfirmed

  STS->>OMS: POST /webhooks/sts/abb-tax-invoice-received { orderId, invoiceNumber, invoiceAmount, invoiceLink }
  OMS-->>STS: 202 { accepted: true }
  Note over OMS,WMS: Outbox: ABBInvoiceSentToWMS → WMS
  Note over OMS,GW: Outbox: ABBInvoiceSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/packed { orderId, packages[{trackingId, vehicleType, weight, lineIds}] }
  OMS-->>WMS: 202 { newStatus: "Packed" }
  Note over OMS: State: Packed (package carries AWB trackingId)

  TMS->>OMS: POST /webhooks/tms/package-dispatched { trackingId, dispatchedAt }
  OMS-->>TMS: 202 { newOrderStatus: "OutForDelivery" }
  Note over OMS: State: OutForDelivery
  Note over OMS,GW: Outbox: OutForDeliveryEvent → Marketplace (TikTok AWB-notify)

  rect rgb(220, 235, 255)
    Note over GW,OMS: TikTok-specific: marketplace fetches AWB for parcel tracking
    GW->>OMS: GET /orders/{orderId}/packages
    OMS-->>GW: 200 [ { trackingId, vehicleType, weight, lineIds } ]
    Note over GW: AWB = packages[0].trackingId
  end

  TMS->>OMS: POST /webhooks/tms/package-delivered { trackingId, deliveredAt, recipientName }
  OMS-->>TMS: 202 { newStatus: "Delivered" }
  Note over OMS: State: Delivered (terminal)

  GW->>OMS: GET /orders/{orderId}
  OMS-->>GW: 200 { status: "Delivered", channelType: "Marketplace", businessUnit: "CMG" }
```

---

## UC4 — Web / CFR / POD full order flow

```mermaid
sequenceDiagram
  title: UC4 — Web / CFR / POD full order flow

  participant Customer
  participant GW
  participant OMS
  participant WMS
  participant TMS
  participant POS
  participant STS

  Customer->>GW: Place POD order (channelType=Web, BU=CFR, paymentMethod=POD)
  GW->>OMS: POST /orders { isPrepaid: false, paymentMethod: "POD" }
  OMS-->>GW: 201 { orderId, status: "Pending", paymentMethod: "POD", businessUnit: "CFR", isPrepaid: false }
  Note over OMS: State: Pending

  WMS->>OMS: POST /webhooks/wms/pick-started { orderId, pickerId }
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickStarted" }
  Note over OMS: State: PickStarted

  WMS->>OMS: POST /webhooks/wms/wave-started { orderId, waveId }
  OMS-->>WMS: 202 { accepted: true, orderId }
  Note over OMS,GW: Outbox: WaveStartedSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/recalculation-requested { orderId, reason: "PickQuantityDiffers" }
  OMS->>POS: Outbound recalculation call (synchronous)
  POS-->>OMS: adjustedAmount
  OMS-->>WMS: 202 { accepted: true, adjustedAmount }

  WMS->>OMS: POST /webhooks/wms/pick-confirmed { orderId, lines[{orderLineId, sku, pickedQty}] }
  OMS->>POS: Internal POS call
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickConfirmed" }
  Note over OMS: State: PickConfirmed (no STS invoice yet — POD invoices after Delivered)

  WMS->>OMS: POST /webhooks/wms/packed { orderId, packages[{trackingId, vehicleType, weight, lineIds}] }
  OMS-->>WMS: 202 { accepted: true, newStatus: "Packed" }
  Note over OMS: State: Packed

  TMS->>OMS: POST /webhooks/tms/package-dispatched { trackingId, dispatchedAt }
  OMS-->>TMS: 202 { newOrderStatus: "OutForDelivery" }
  Note over OMS: State: OutForDelivery

  rect rgb(255, 240, 210)
    Note over TMS,OMS: POD-specific Step 6a — pre-delivery recalculation before driver collects payment
    TMS->>OMS: POST /webhooks/tms/recalculation-requested { trackingId, reason: "PickQuantityDiffers" }
    OMS->>POS: Outbound recalculation call (synchronous)
    POS-->>OMS: adjustedAmount
    OMS-->>TMS: 202 { accepted: true, adjustedAmount }
    Note over OMS: Timeline event: PosRecalcCalled recorded
  end

  TMS->>OMS: POST /webhooks/tms/package-delivered { trackingId, deliveredAt, recipientName }
  OMS-->>TMS: 202 { accepted: true, newStatus: "Delivered", invoiceTriggered: true }
  Note over OMS: State: Delivered (customer pays driver at door)

  STS->>OMS: POST /webhooks/sts/abb-tax-invoice-received { orderId, invoiceNumber, invoiceAmount, invoiceLink }
  OMS-->>STS: 202 { accepted: true }
  Note over OMS: State remains Delivered — invoice does not advance state machine
  Note over OMS,TMS: Outbox: ABBTaxInvoiceSentToTMS → TMS (POD routing: TMS+GW, not WMS+GW)
  Note over OMS,GW: Outbox: ABBTaxInvoiceSentToGW → GW

  GW->>OMS: GET /orders/{orderId}
  OMS-->>GW: 200 { status: "Delivered", businessUnit: "CFR", paymentMethod: "POD" }
```

---

## UC5 — Web / CFR / POD — weight-based fresh products (pork 841.23 g + duck 1.23 kg)

```mermaid
sequenceDiagram
  title: UC5 — Web / CFR / POD — weight-based fresh products (pork 841.23 g + duck 1.23 kg)

  participant Customer
  participant GW
  participant OMS
  participant WMS
  participant TMS
  participant POS
  participant STS

  Customer->>GW: Place POD order with weight-based lines (BU=CFR)
  GW->>OMS: POST /orders { paymentMethod: "POD", lines: [PORK-KG @ 12700 sat/kg × 0.84123 kg, DUCK-KG @ 3960 sat/kg × 1.23 kg] }
  OMS-->>GW: 201 { orderId, status: "Pending", paymentMethod: "POD", businessUnit: "CFR", lines: [porkLine, duckLine] }
  Note over OMS: State: Pending — pork: 10684 sat, duck: 4871 sat, total: 15555 sat

  WMS->>OMS: POST /webhooks/wms/pick-started { orderId, pickerId }
  OMS-->>WMS: 202 { newStatus: "PickStarted" }
  Note over OMS: State: PickStarted

  WMS->>OMS: POST /webhooks/wms/wave-started { orderId, waveId }
  OMS-->>WMS: 202 { accepted: true, orderId }
  Note over OMS,GW: Outbox: WaveStartedSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/recalculation-requested { orderId, reason: "ActualWeightDiffers" }
  OMS->>POS: Outbound recalculation call (synchronous) — actual weights confirmed
  POS-->>OMS: adjustedAmount (POS-rounded satang)
  OMS-->>WMS: 202 { accepted: true, adjustedAmount }

  WMS->>OMS: POST /webhooks/wms/pick-confirmed { orderId, lines: [porkLine pickedQty=0.84123, duckLine pickedQty=1.23] }
  OMS->>POS: Internal POS call
  OMS-->>WMS: 202 { newStatus: "PickConfirmed" }
  Note over OMS: State: PickConfirmed

  WMS->>OMS: POST /webhooks/wms/packed { orderId, packages[{trackingId, weight: 2.07123, lineIds: [porkLine, duckLine]}] }
  OMS-->>WMS: 202 { newStatus: "Packed" }
  Note over OMS: State: Packed

  TMS->>OMS: POST /webhooks/tms/package-dispatched { trackingId, dispatchedAt }
  OMS-->>TMS: 202 { newOrderStatus: "OutForDelivery" }
  Note over OMS: State: OutForDelivery

  rect rgb(255, 240, 210)
    Note over TMS,OMS: POD-specific Step 6a — driver confirms final weight before collecting payment
    TMS->>OMS: POST /webhooks/tms/recalculation-requested { trackingId, reason: "ActualWeightDiffers", actualWeight: 2.07123 }
    OMS->>POS: Outbound recalculation call (synchronous)
    POS-->>OMS: adjustedAmount
    OMS-->>TMS: 202 { accepted: true, adjustedAmount }
    Note over OMS: Timeline event: PosRecalcCalled recorded
  end

  TMS->>OMS: POST /webhooks/tms/package-delivered { trackingId, deliveredAt, recipientName }
  OMS-->>TMS: 202 { newStatus: "Delivered", invoiceTriggered: true }
  Note over OMS: State: Delivered (customer pays driver at door)

  STS->>OMS: POST /webhooks/sts/abb-tax-invoice-received { orderId, invoiceAmount: 15555, currency: "THB" }
  OMS-->>STS: 202 { accepted: true }
  Note over OMS: State remains Delivered
  Note over OMS,TMS: Outbox: ABBTaxInvoiceSentToTMS → TMS (POD routing: TMS+GW)
  Note over OMS,GW: Outbox: ABBTaxInvoiceSentToGW → GW

  GW->>OMS: GET /orders/{orderId}
  OMS-->>GW: 200 { status: "Delivered", businessUnit: "CFR", paymentMethod: "POD" }
```

---

## UC6 — Web / CFR / POD — beef + chicken order, beef not fresh → partial return

```mermaid
sequenceDiagram
  title: UC6 — Web / CFR / POD — beef + chicken order, beef not fresh → partial return

  participant Customer
  participant GW
  participant OMS
  participant WMS
  participant TMS
  participant POS
  participant STS

  Customer->>GW: Place POD order (BU=CFR, lines: BEEF-KG 0.5 kg @ 35000 sat/kg, CHICKEN-KG 0.5 kg @ 15000 sat/kg)
  GW->>OMS: POST /orders { paymentMethod: "POD", lines: [beefLine, chickenLine] }
  OMS-->>GW: 201 { orderId, status: "Pending", lines: [beefLine, chickenLine] }
  Note over OMS: State: Pending — beef: 17500 sat, chicken: 7500 sat, total: 25000 sat

  WMS->>OMS: POST /webhooks/wms/pick-started { orderId, pickerId }
  OMS-->>WMS: 202 { newStatus: "PickStarted" }
  Note over OMS: State: PickStarted

  WMS->>OMS: POST /webhooks/wms/wave-started { orderId, waveId }
  OMS-->>WMS: 202 { accepted: true, orderId }
  Note over OMS,GW: Outbox: WaveStartedSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/recalculation-requested { orderId, reason: "PickQuantityDiffers" }
  OMS->>POS: Outbound recalculation call (synchronous)
  POS-->>OMS: adjustedAmount
  OMS-->>WMS: 202 { accepted: true, adjustedAmount }

  WMS->>OMS: POST /webhooks/wms/pick-confirmed { orderId, lines: [beefLine pickedQty=0.5, chickenLine pickedQty=0.5] }
  OMS->>POS: Internal POS call
  OMS-->>WMS: 202 { newStatus: "PickConfirmed" }
  Note over OMS: State: PickConfirmed

  WMS->>OMS: POST /webhooks/wms/packed { orderId, packages[{trackingId, weight: 1.0, lineIds: [beefLine, chickenLine]}] }
  OMS-->>WMS: 202 { newStatus: "Packed" }
  Note over OMS: State: Packed

  TMS->>OMS: POST /webhooks/tms/package-dispatched { trackingId, dispatchedAt }
  OMS-->>TMS: 202 { newOrderStatus: "OutForDelivery" }
  Note over OMS: State: OutForDelivery

  rect rgb(255, 240, 210)
    Note over TMS,OMS: POD-specific Step 6a — pre-delivery recalculation before driver collects payment
    TMS->>OMS: POST /webhooks/tms/recalculation-requested { trackingId, reason: "PickQuantityDiffers" }
    OMS->>POS: Outbound recalculation call (synchronous)
    POS-->>OMS: adjustedAmount
    OMS-->>TMS: 202 { accepted: true, adjustedAmount }
    Note over OMS: Timeline event: PosRecalcCalled recorded
  end

  TMS->>OMS: POST /webhooks/tms/package-delivered { trackingId, deliveredAt, recipientName }
  OMS-->>TMS: 202 { newStatus: "Delivered", invoiceTriggered: true }
  Note over OMS: State: Delivered

  STS->>OMS: POST /webhooks/sts/abb-tax-invoice-received { orderId, invoiceAmount: 25000, currency: "THB" }
  OMS-->>STS: 202 { accepted: true }
  Note over OMS: State remains Delivered
  Note over OMS,TMS: Outbox: ABBTaxInvoiceSentToTMS → TMS
  Note over OMS,GW: Outbox: ABBTaxInvoiceSentToGW → GW

  rect rgb(255, 220, 220)
    Note over Customer,OMS: Customer rejects beef at the door (not fresh) — keeps chicken
    Customer->>GW: Initiate partial return for beef only
    GW->>OMS: POST /returns { orderId, returnType: "PartialItem", returnReason: "ItemNotFresh", items: [{ orderLineId: beefLineId, sku: "BEEF-KG", quantity: 0.5 }] }
    OMS-->>GW: 201 { returnId, orderId, status: "Requested", items: [beefLine] }
    Note over OMS: Return status: Requested — only beefLine, chickenLine not returned

    GW->>OMS: GET /returns/{returnId}
    OMS-->>GW: 200 { orderId, returnReason: "ItemNotFresh", status: "Requested" }

    GW->>OMS: GET /returns/{returnId}/items
    OMS-->>GW: 200 [ { sku: "BEEF-KG" } ] (CHICKEN-KG absent)
  end
```

---

## UC7 — Stock transfer from Store A to Store B

```mermaid
sequenceDiagram
  title: UC7 — Stock transfer from Store A to Store B

  participant GW
  participant OMS
  participant WMS

  GW->>OMS: POST /inbound/transfer-orders { sourceStoreId: "STORE-A", destStoreId: "STORE-B", lines: [{ sku: "WATER-1L", requestedQty: 6 }] }
  OMS-->>GW: 201 { transferOrderId, status: "Created" }
  Note over OMS: Transfer state: Created

  GW->>OMS: GET /inbound/transfer-orders/{transferOrderId}
  OMS-->>GW: 200 { status: "Created", lines: [{ sku: "WATER-1L", requestedQty: 6 }] }

  WMS->>OMS: POST /webhooks/wms/transfer-pick-confirmed { transferOrderId, lines: [{ sku: "WATER-1L", transferredQty: 6 }], confirmedAt }
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickConfirmed" }
  Note over OMS: Transfer state: PickConfirmed

  WMS->>OMS: POST /webhooks/wms/transfer-received { transferOrderId, receivedAt }
  OMS-->>WMS: 202 { accepted: true, newStatus: "Completed" }
  Note over OMS: Transfer state: Completed (terminal)

  GW->>OMS: GET /inbound/transfer-orders/{transferOrderId}
  OMS-->>GW: 200 { status: "Completed" }
```

---

## UC8 — Customer postpones delivery date

```mermaid
sequenceDiagram
  title: UC8 — Customer postpones delivery date

  participant Customer
  participant GW
  participant OMS
  participant WMS
  participant TMS
  participant POS
  participant STS

  Customer->>GW: Place Prepaid order (BU=CFR, channelType=Web)
  GW->>OMS: POST /orders
  OMS-->>GW: 201 { orderId, status: "Pending" }
  Note over OMS: State: Pending

  GW->>OMS: GET /orders/{orderId}/delivery-slot
  OMS-->>GW: 200 { deliverySlot: { scheduledStart, scheduledEnd } }

  rect rgb(220, 255, 220)
    Note over Customer,OMS: Slot reschedule allowed while order is Pending
    Customer->>GW: Reschedule delivery to +5 hours
    GW->>TMS: Reschedule request
    TMS->>OMS: POST /webhooks/tms/slot-rescheduled { orderId, newScheduledStart: +5h, newScheduledEnd: +6h, bookingRef, reason: "CustomerRequest" }
    OMS-->>TMS: 202 { accepted: true, orderId, deliverySlot: { scheduledStart } }
    Note over OMS,WMS: Outbox: DeliverySlotRescheduledEvent → WMS

    GW->>OMS: GET /orders/{orderId}/delivery-slot
    OMS-->>GW: 200 { deliverySlot: { scheduledStart } }
  end

  WMS->>OMS: POST /webhooks/wms/pick-started { orderId, pickerId }
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickStarted" }
  Note over OMS: State: PickStarted

  WMS->>OMS: POST /webhooks/wms/wave-started { orderId, waveId }
  OMS-->>WMS: 202 { accepted: true, orderId }
  Note over OMS,GW: Outbox: WaveStartedSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/recalculation-requested { orderId, reason: "PickQuantityDiffers" }
  OMS->>POS: Outbound recalculation call (synchronous)
  POS-->>OMS: adjustedAmount
  OMS-->>WMS: 202 { accepted: true, adjustedAmount }

  WMS->>OMS: POST /webhooks/wms/pick-confirmed { orderId, lines[{orderLineId, sku, pickedQty}] }
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickConfirmed" }
  Note over OMS: State: PickConfirmed

  STS->>OMS: POST /webhooks/sts/abb-tax-invoice-received { orderId, invoiceNumber, invoiceAmount }
  OMS-->>STS: 202 { accepted: true }
  Note over OMS,WMS: Outbox: ABBInvoiceSentToWMS → WMS
  Note over OMS,GW: Outbox: ABBInvoiceSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/packed { orderId, packages[{trackingId, vehicleType, weight, lineIds}] }
  OMS-->>WMS: 202 { accepted: true, newStatus: "Packed" }
  Note over OMS: State: Packed

  TMS->>OMS: POST /webhooks/tms/package-dispatched { trackingId, dispatchedAt }
  OMS-->>TMS: 202 { newOrderStatus: "OutForDelivery" }
  Note over OMS: State: OutForDelivery

  rect rgb(255, 210, 210)
    Note over Customer,OMS: Reschedule attempt rejected once OutForDelivery
    Customer->>GW: Attempt to reschedule slot again (+7 hours)
    GW->>TMS: Reschedule request
    TMS->>OMS: POST /webhooks/tms/slot-rescheduled { orderId, newScheduledStart: +7h, newScheduledEnd: +8h, reason: "CustomerRequest" }
    OMS-->>TMS: 409 { error: "slot_change_not_allowed" }
  end
```

---

## UC9 — Customer cancels order

```mermaid
sequenceDiagram
  title: UC9 — Customer cancels order

  participant Customer
  participant GW
  participant OMS
  participant WMS

  rect rgb(220, 255, 220)
    Note over Customer,OMS: Scenario A — Cancel from Pending (allowed)
    Customer->>GW: Place order
    GW->>OMS: POST /orders
    OMS-->>GW: 201 { orderId1, status: "Pending" }
    Note over OMS: State: Pending

    Customer->>GW: Cancel order
    GW->>OMS: PATCH /orders/{orderId1}/cancel { reason: "CustomerRequest", cancelledBy }
    OMS-->>GW: 200 { id: orderId1, newStatus: "Cancelled" }
    Note over OMS: State: Cancelled (terminal)

    GW->>OMS: GET /orders/{orderId1}
    OMS-->>GW: 200 { status: "Cancelled" }
  end

  rect rgb(255, 210, 210)
    Note over Customer,OMS: Scenario B — Cancel from PickStarted (rejected)
    Customer->>GW: Place second order
    GW->>OMS: POST /orders
    OMS-->>GW: 201 { orderId2, status: "Pending" }
    Note over OMS: State: Pending

    WMS->>OMS: POST /webhooks/wms/pick-started { orderId2, pickerId }
    OMS-->>WMS: 202 { accepted: true, newStatus: "PickStarted" }
    Note over OMS: State: PickStarted

    Customer->>GW: Attempt to cancel order in PickStarted
    GW->>OMS: PATCH /orders/{orderId2}/cancel { reason: "CustomerRequest", cancelledBy }
    OMS-->>GW: 409 { error: "invalid_transition" }
    Note over OMS: State unchanged: PickStarted — cancel invariant enforced
  end
```

---

## UC10 — Short-pick: dish soap out of stock, only water delivered

```mermaid
sequenceDiagram
  title: UC10 — Short-pick: dish soap out of stock, only water delivered

  participant Customer
  participant GW
  participant OMS
  participant WMS
  participant TMS
  participant POS
  participant STS

  Customer->>GW: Place Prepaid order (BU=CFR, lines: WATER-500ML ×2 @ 1500 sat, DISH-SOAP-500ML ×1 @ 4500 sat)
  GW->>OMS: POST /orders { paymentMethod: "Prepaid", lines: [waterLine, soapLine] }
  OMS-->>GW: 201 { orderId, status: "Pending", lines: [waterLine, soapLine] }
  Note over OMS: State: Pending

  WMS->>OMS: POST /webhooks/wms/pick-started { orderId, pickerId }
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickStarted" }
  Note over OMS: State: PickStarted

  WMS->>OMS: POST /webhooks/wms/wave-started { orderId, waveId }
  OMS-->>WMS: 202 { accepted: true, orderId }
  Note over OMS,GW: Outbox: WaveStartedSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/recalculation-requested { orderId, reason: "OutOfStock" }
  OMS->>POS: Outbound recalculation call (synchronous) — soap excluded
  POS-->>OMS: adjustedAmount (water only)
  OMS-->>WMS: 202 { accepted: true, adjustedAmount }

  rect rgb(255, 240, 210)
    Note over WMS,OMS: Short-pick recorded — soap line pickedQty = 0
    WMS->>OMS: PATCH /orders/{orderId}/partial-pick { lines: [{ orderLineId: soapLineId, pickedQuantity: 0, orderedQuantity: 1, reason: "OutOfStock" }], idempotencyKey }
    OMS-->>WMS: 200 { orderId, partialLines: [{ orderLineId: soapLineId, shortfallQuantity: 1 }] }
  end

  WMS->>OMS: POST /webhooks/wms/pick-confirmed { orderId, lines: [waterLine pickedQty=2, soapLine pickedQty=0] }
  OMS->>POS: Internal POS call
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickConfirmed" }
  Note over OMS: State: PickConfirmed — soap line shortfall recorded

  STS->>OMS: POST /webhooks/sts/abb-tax-invoice-received { orderId, invoiceAmount: 3000 (water only), currency: "THB" }
  OMS-->>STS: 202 { accepted: true }
  Note over OMS,WMS: Outbox: ABBInvoiceSentToWMS → WMS
  Note over OMS,GW: Outbox: ABBInvoiceSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/packed { orderId, packages[{ trackingId, lineIds: [waterLine only] }] }
  OMS-->>WMS: 202 { accepted: true, newStatus: "Packed" }
  Note over OMS: State: Packed (soap line absent from package)

  TMS->>OMS: POST /webhooks/tms/package-dispatched { trackingId, dispatchedAt }
  OMS-->>TMS: 202 { newOrderStatus: "OutForDelivery" }
  Note over OMS: State: OutForDelivery

  TMS->>OMS: POST /webhooks/tms/package-delivered { trackingId, deliveredAt, recipientName }
  OMS-->>TMS: 202 { newStatus: "Delivered", invoiceTriggered: true }
  Note over OMS: State: Delivered (terminal)

  GW->>OMS: GET /orders/{orderId}
  OMS-->>GW: 200 { status: "Delivered", lines: [waterLine pickedQty=2, soapLine pickedQty=0] }
```

---

## UC11 — Substitution: fabric softener → dish soap, credit note for price difference

```mermaid
sequenceDiagram
  title: UC11 — Substitution: fabric softener → dish soap, credit note for price difference

  participant Customer
  participant GW
  participant OMS
  participant WMS
  participant TMS
  participant POS
  participant STS

  Customer->>GW: Place Prepaid order (BU=CFR, lines: WATER-500ML ×2 @ 1500 sat, FABRIC-SOFTENER-500ML ×1 @ 8900 sat)
  GW->>OMS: POST /orders { paymentMethod: "Prepaid", lines: [waterLine, softenerLine] }
  OMS-->>GW: 201 { orderId, status: "Pending", lines: [waterLine, softenerLine] }
  Note over OMS: State: Pending

  WMS->>OMS: POST /webhooks/wms/pick-started { orderId, pickerId }
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickStarted" }
  Note over OMS: State: PickStarted

  WMS->>OMS: POST /webhooks/wms/wave-started { orderId, waveId }
  OMS-->>WMS: 202 { accepted: true, orderId }
  Note over OMS,GW: Outbox: WaveStartedSentToGW → GW

  rect rgb(255, 240, 210)
    Note over WMS,OMS: Fabric softener unavailable — substitution offered
    WMS->>OMS: POST /webhooks/wms/substitution-offered { orderId, orderLineId: softenerLineId, substituteSku: "DISH-SOAP-500ML", substituteUnitPrice: 4500, substitutedAmount: 1 }
    OMS-->>WMS: 202 { accepted: true, substitutionId, customerNotified: true }
    Note over OMS: Substitution record created — customerApproved: null

    GW->>OMS: GET /orders/{orderId}/substitutions
    OMS-->>GW: 200 [ { substitutionId, originalSku: "FABRIC-SOFTENER-500ML", substituteSku: "DISH-SOAP-500ML", customerApproved: null } ]

    Customer->>GW: Approve substitution
    GW->>OMS: POST /orders/{orderId}/substitutions/{substitutionId}/approve
    OMS-->>GW: 200 { substitutionId, customerApproved: true }
    Note over OMS: customerApproved: true
  end

  WMS->>OMS: POST /webhooks/wms/recalculation-requested { orderId, reason: "SubstitutionApproved" }
  OMS->>POS: Outbound recalculation call (synchronous)
  POS-->>OMS: adjustedAmount (dish soap price applied)
  OMS-->>WMS: 202 { accepted: true, adjustedAmount }

  WMS->>OMS: POST /webhooks/wms/pick-confirmed { orderId, lines: [waterLine pickedQty=2 substituted=false, softenerLine pickedQty=1 substituted=true] }
  OMS->>POS: Internal POS call
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickConfirmed" }
  Note over OMS: State: PickConfirmed

  STS->>OMS: POST /webhooks/sts/abb-tax-invoice-received { orderId, invoiceAmount: 7500 (2×1500 water + 4500 dish soap) }
  OMS-->>STS: 202 { accepted: true }
  Note over OMS,WMS: Outbox: ABBInvoiceSentToWMS → WMS
  Note over OMS,GW: Outbox: ABBInvoiceSentToGW → GW

  rect rgb(220, 235, 255)
    Note over STS,OMS: Credit note issued — substitute cheaper than original (8900 − 4500 = 4400 sat refund)
    STS->>OMS: POST /webhooks/sts/credit-note-received { orderId, creditNoteNumber, amount: 4400, currency: "THB" }
    OMS-->>STS: 202 { accepted: true }
  end

  WMS->>OMS: POST /webhooks/wms/packed { orderId, packages[{trackingId, lineIds: [waterLine, softenerLine]}] }
  OMS-->>WMS: 202 { accepted: true, newStatus: "Packed" }
  Note over OMS: State: Packed

  TMS->>OMS: POST /webhooks/tms/package-dispatched { trackingId, dispatchedAt }
  OMS-->>TMS: 202 { newOrderStatus: "OutForDelivery" }
  Note over OMS: State: OutForDelivery

  TMS->>OMS: POST /webhooks/tms/package-delivered { trackingId, deliveredAt, recipientName }
  OMS-->>TMS: 202 { newStatus: "Delivered", invoiceTriggered: true }
  Note over OMS: State: Delivered (terminal)

  GW->>OMS: GET /orders/{orderId}/substitutions
  OMS-->>GW: 200 [ { substitutionId, customerApproved: true } ]
```

---

## UC12 — Full return after delivery (CustomerRequest)

```mermaid
sequenceDiagram
  title: UC12 — Full return after delivery (CustomerRequest)

  participant Customer
  participant GW
  participant OMS
  participant WMS
  participant TMS
  participant POS
  participant STS

  Customer->>GW: Place Prepaid order (BU=CFR)
  GW->>OMS: POST /orders
  OMS-->>GW: 201 { orderId, status: "Pending" }
  Note over OMS: State: Pending

  WMS->>OMS: POST /webhooks/wms/pick-started { orderId, pickerId }
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickStarted" }
  Note over OMS: State: PickStarted

  WMS->>OMS: POST /webhooks/wms/wave-started { orderId, waveId }
  OMS-->>WMS: 202 { accepted: true, orderId }
  Note over OMS,GW: Outbox: WaveStartedSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/recalculation-requested { orderId, reason: "PickQuantityDiffers" }
  OMS->>POS: Outbound recalculation call (synchronous)
  POS-->>OMS: adjustedAmount
  OMS-->>WMS: 202 { accepted: true, adjustedAmount }

  WMS->>OMS: POST /webhooks/wms/pick-confirmed { orderId, lines: [{ orderLineId, sku: "APPLE-1KG", pickedQty: 2 }] }
  OMS->>POS: Internal POS call
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickConfirmed" }
  Note over OMS: State: PickConfirmed

  STS->>OMS: POST /webhooks/sts/abb-tax-invoice-received { orderId, invoiceNumber, invoiceAmount }
  OMS-->>STS: 202 { accepted: true }
  Note over OMS,WMS: Outbox: ABBInvoiceSentToWMS → WMS
  Note over OMS,GW: Outbox: ABBInvoiceSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/packed { orderId, packages[{trackingId, vehicleType, weight, lineIds}] }
  OMS-->>WMS: 202 { accepted: true, newStatus: "Packed" }
  Note over OMS: State: Packed

  TMS->>OMS: POST /webhooks/tms/package-dispatched { trackingId, dispatchedAt }
  OMS-->>TMS: 202 { newOrderStatus: "OutForDelivery" }
  Note over OMS: State: OutForDelivery

  TMS->>OMS: POST /webhooks/tms/package-delivered { trackingId, deliveredAt, recipientName }
  OMS-->>TMS: 202 { newStatus: "Delivered", invoiceTriggered: true }
  Note over OMS: State: Delivered

  GW->>OMS: GET /orders/{orderId}
  OMS-->>GW: 200 { status: "Delivered" }

  rect rgb(255, 220, 220)
    Note over Customer,OMS: Full return initiated after delivery
    Customer->>GW: Initiate full return (CustomerRequest)
    GW->>OMS: POST /returns { orderId, returnReason: "CustomerRequest", items: [{ orderLineId, sku: "APPLE-1KG", quantity: 2, itemReason: "CustomerRequest" }] }
    OMS-->>GW: 201 { returnId, orderId, status: "Requested" }
    Note over OMS: Return status: Requested

    GW->>OMS: GET /returns/{returnId}
    OMS-->>GW: 200 { orderId, status: "Requested" }

    GW->>OMS: GET /returns/{returnId}/items
    OMS-->>GW: 200 [ { sku: "APPLE-1KG", quantity: 2 } ]

    WMS->>OMS: POST /webhooks/wms/put-away-confirmed { returnId, items: [{ sku: "APPLE-1KG", condition: "Resellable", sloc: "B-05", quantity: 2, performedBy }], putAwayAt }
    OMS-->>WMS: 202 { accepted: true, returnId, newReturnStatus: "PutAway", refundInitiated: true }
    Note over OMS: Return status: PutAway — refund initiated automatically

    GW->>OMS: GET /returns/{returnId}
    OMS-->>GW: 200 { status: "PutAway" }
  end
```

---

## UC13 — Web / CFR / Prepaid order with coupon FRESH10 (10% discount)

```mermaid
sequenceDiagram
  title: UC13 — Web / CFR / Prepaid order with coupon FRESH10 (10% discount)

  participant Customer
  participant GW
  participant OMS
  participant WMS
  participant TMS
  participant POS
  participant STS

  Customer->>GW: Place Prepaid order with coupon FRESH10 (channelType=Web, BU=CFR)
  GW->>OMS: POST /orders { paymentMethod: "Prepaid", promotions: [{ promoCode: "FRESH10", promoType: "PercentageDiscount", discountPercentage: 0.10 }] }
  OMS-->>GW: 201 { orderId, status: "Pending", businessUnit: "CFR" }
  Note over OMS: State: Pending — coupon FRESH10 attached (10% off)

  WMS->>OMS: POST /webhooks/wms/pick-started { orderId, pickerId }
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickStarted" }
  Note over OMS: State: PickStarted

  WMS->>OMS: POST /webhooks/wms/wave-started { orderId, waveId }
  OMS-->>WMS: 202 { accepted: true, orderId }
  Note over OMS,GW: Outbox: WaveStartedSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/recalculation-requested { orderId, reason: "PickQuantityDiffers", promoCode: "FRESH10" }
  OMS->>POS: Outbound recalculation call (synchronous) — POS applies FRESH10 coupon
  POS-->>OMS: adjustedAmount (10% discount applied, e.g. 19800 × 0.90 = 17820 sat)
  OMS-->>WMS: 202 { accepted: true, adjustedAmount }
  Note over OMS: Timeline event: recalculation event recorded

  WMS->>OMS: POST /webhooks/wms/pick-confirmed { orderId, lines: [{ orderLineId, sku: "APPLE-1KG", pickedQty: 2 }] }
  OMS->>POS: Internal POS call
  OMS-->>WMS: 202 { accepted: true, newStatus: "PickConfirmed" }
  Note over OMS: State: PickConfirmed

  STS->>OMS: POST /webhooks/sts/abb-tax-invoice-received { orderId, invoiceAmount: 17820 (discounted), currency: "THB" }
  OMS-->>STS: 202 { accepted: true }
  Note over OMS,WMS: Outbox: ABBInvoiceSentToWMS → WMS
  Note over OMS,GW: Outbox: ABBInvoiceSentToGW → GW

  WMS->>OMS: POST /webhooks/wms/packed { orderId, packages[{trackingId, vehicleType, weight, lineIds}] }
  OMS-->>WMS: 202 { accepted: true, newStatus: "Packed" }
  Note over OMS: State: Packed

  TMS->>OMS: POST /webhooks/tms/package-dispatched { trackingId, dispatchedAt }
  OMS-->>TMS: 202 { newOrderStatus: "OutForDelivery" }
  Note over OMS: State: OutForDelivery

  TMS->>OMS: POST /webhooks/tms/package-delivered { trackingId, deliveredAt, recipientName }
  OMS-->>TMS: 202 { newStatus: "Delivered", invoiceTriggered: true }
  Note over OMS: State: Delivered (terminal)

  GW->>OMS: GET /orders/{orderId}
  OMS-->>GW: 200 { status: "Delivered", businessUnit: "CFR" }
```

---

## Summary Table

| UC | Title | Channel | BU | Payment | Terminal State |
|----|-------|---------|-----|---------|----------------|
| UC1 | Web / CMG / Prepaid full order flow | Web | CMG | Prepaid | Delivered |
| UC2 | Web / CFR / Prepaid full order flow | Web | CFR | Prepaid | Delivered |
| UC3 | TikTok Marketplace / CMG / Prepaid — AWB retrieval after OutForDelivery | Marketplace (TikTok) | CMG | Prepaid | Delivered |
| UC4 | Web / CFR / POD full order flow | Web | CFR | POD | Delivered |
| UC5 | Web / CFR / POD — weight-based fresh products (pork 841.23 g + duck 1.23 kg) | Web | CFR | POD | Delivered |
| UC6 | Web / CFR / POD — beef + chicken order, beef not fresh → partial return | Web | CFR | POD | Delivered + ReturnRequested |
| UC7 | Stock transfer from Store A to Store B | — (Inbound) | — | — | Completed |
| UC8 | Customer postpones delivery date | Web | CFR | Prepaid | OutForDelivery (slot-reschedule 409 enforced) |
| UC9 | Customer cancels order | Web | — | — | Cancelled (Scenario A) / PickStarted—unchanged (Scenario B) |
| UC10 | Short-pick: dish soap out of stock, only water delivered | Web | CFR | Prepaid | Delivered |
| UC11 | Substitution: fabric softener → dish soap, credit note for price difference | Web | CFR | Prepaid | Delivered |
| UC12 | Full return after delivery (CustomerRequest) | Web | CFR | Prepaid | Delivered + PutAway (return) |
| UC13 | Web / CFR / Prepaid order with coupon FRESH10 (10% discount) | Web | CFR | Prepaid | Delivered |
