@@ -1,40 +0,0 @@
# Order Management System

## Context

This is the OMS that your team is managing. Details on how the OMS works can be found in the path docs.

And all incoming requirements should be learned and incorporated into your team's knowledge base, according to each person's role.

## Requirement

I want you to apply new business logic to the document OMS. You must change or improve the docs that support business logic. path documents is the docs

## Business Logic

- Remove context about CBE CHG Backend of the OMS
- This is sequence flow of pre-paid lated update Steps:
    - Time Slot Request
    Customer → Gateway → PS → TMS
    Available delivery windows are queried before any order is created.

    - Booking Created
    Customer → Gateway → PS → TMS
    Delivery slot is locked.

    - Order Created → Pending
    Customer → Gateway → PS → SC (OMS)
    POST /orders is called. OMS creates the order in Pending status.

    - OMS notifies TMS and WMS (Outbox)
    SC → WMS: SaleOrderSentToWMS
    SC → TMS: SaleOrderSentToTMS
    Both dispatched atomically via the outbox worker.

    - WMS starts picking → PickStarted
    WMS → SC (webhook: /webhooks/wms/pick-started)

    - WMS starts wave → WaveStarted
    WMS → SC (webhook)

    - OMS notifies (Outbox)
    SC → Gateway: WaveStartedSentToGateway

    - POS Recalculation
    WMS → SC → POS → SC → WMS
    Repeats as needed during picking (e.g., substitutions, price adjustments).
    pos_recalc_pending = true blocks packing until POS confirms final price.

    - Pick Confirmed
    WMS → SC (webhook: /webhooks/wms/pick-confirmed)
    Status → PickConfirmed. SC notifies POS.
    
    - OMS notifies (Outbox)
    SC → TMS: PickConfirmedSentToTMS
    SC → Gateway: PickConfirmedSentToGateway

    - ABB/Tax Invoice receive webhook from STS
    STS → SC (webhook)

    - OMS notifies (Outbox)
    SC → WMS: ABBInvoiceSentToWMS (outbox)
    SC → Gateway: ABBInvoiceSentToGateway (outbox)

    - [Option] if have Credit Note: receive webhook from STS
    STS → SC (webhook)

    - [Option] if have Credit Note: OMS notifies (Outbox)
    SC → WMS: CreditNoteSentToWMS (outbox)
    SC → Gateway: CreditNoteSentToGateway (outbox)

    - POS Recalculation
    TMS → SC → POS → SC → TMS
    Repeats as needed during picking (e.g., substitutions, price adjustments).
    pos_recalc_pending = true blocks packing until POS confirms final price.

    - Package Dispatched → OutForDelivery
    TMS → SC (webhook: /webhooks/tms/package-dispatched)
    Status → OutForDelivery.

    - OMS notifies (Outbox)
    SC → Gateway: OutForDeliverySentToGateway (outbox)

    - Package Delivered → Delivered
    TMS → SC (webhook: /webhooks/tms/package-delivered)
    Status → Delivered.

    - OMS notifies (Outbox)
    SC → Gateway: DeliveredSentToGateway (outbox)

- This is sequence flow of pod lated update Steps:
    - Time Slot Request
    Customer → Gateway → PS → TMS
    Available delivery windows are queried before any order is created.

    - Booking Created
    Customer → Gateway → PS → TMS
    Delivery slot is locked.

    - Order Created → Pending
    Customer → Gateway → PS → SC (OMS)
    POST /orders is called. OMS creates the order in Pending status.

    - OMS notifies TMS and WMS (Outbox)
    SC → WMS: SaleOrderSentToWMS
    SC → TMS: SaleOrderSentToTMS
    Both dispatched atomically via the outbox worker.

    - WMS starts picking → PickStarted
    WMS → SC (webhook: /webhooks/wms/pick-started)

    - WMS starts wave → WaveStarted
    WMS → SC (webhook)

    - OMS notifies (Outbox)
    SC → Gateway: WaveStartedSentToGateway

    - POS Recalculation
    WMS → SC → POS → SC → WMS
    Repeats as needed during picking (e.g., substitutions, price adjustments).
    pos_recalc_pending = true blocks packing until POS confirms final price.

    - Pick Confirmed
    WMS → SC (webhook: /webhooks/wms/pick-confirmed)
    Status → PickConfirmed. SC notifies POS.
    
    - OMS notifies (Outbox)
    SC → TMS: PickConfirmedSentToTMS
    SC → Gateway: PickConfirmedSentToGateway

    - POS Recalculation
    TMS → SC → POS → SC → TMS
    Repeats as needed during picking (e.g., substitutions, price adjustments).
    pos_recalc_pending = true blocks packing until POS confirms final price.

    - Package Dispatched → OutForDelivery
    TMS → SC (webhook: /webhooks/tms/package-dispatched)
    Status → OutForDelivery.

    - OMS notifies (Outbox)
    SC → Gateway: OutForDeliverySentToGateway (outbox)

    - Package Delivered → Delivered
    TMS → SC (webhook: /webhooks/tms/package-delivered)
    Status → Delivered.

    - OMS notifies (Outbox)
    SC → Gateway: DeliveredSentToGateway (outbox)

    - ABB/Tax Invoice receive webhook from STS
    STS → SC (webhook)

    - OMS notifies (Outbox)
    SC → TMS: ABBInvoiceSentToWMS (outbox)
    SC → Gateway: ABBInvoiceSentToGateway (outbox)

    - [Option] if have Credit Note: receive webhook from STS
    STS → SC (webhook)

    - [Option] if have Credit Note: OMS notifies (Outbox)
    SC → TMS: CreditNoteSentToWMS (outbox)
    SC → Gateway: CreditNoteSentToGateway (outbox)