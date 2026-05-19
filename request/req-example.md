@@ -1,40 +0,0 @@
# Order Management System

## Context

This is the OMS that your team is managing. Details on how the OMS works can be found in the path docs.

And all incoming requirements should be learned and incorporated into your team's knowledge base, according to each person's role.

## Requirement

I want you to apply new business logic to the document OMS. You must change or improve the docs that support business logic. path documents is the docs

## Business Logic

- This is sequence flow of pre-paid Steps:
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
    SC → WMS: SaleOrderSentToTMS
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
    SC → WMS: ABBTaxInvoiceSentToWMS (outbox)

    - [Option] if have Credit Note: receive webhook from STS
    STS → SC (webhook)

    - [Option] if have Credit Note: OMS notifies (Outbox)
    SC → WMS: CreditNoteSentToWMS (outbox)

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

- Feature OMS must to support
    - Create Order
    - Cancel Order
    - Partial Pick
    - Return Order
    - Rescheduler
    - in case create order ในกรณี สั่งเนื้อกับไก่ แต่เอาแค่ไก่ เนื้อไม่สด ลูกค้าเลยไม่เอา สามารถคืนเงินได้
    - OMS must to support mutiple bu like:
        - ในกรณี Multiple MarketPlace:
            - ในกรณี Merket Place เป็น Tiktok ในจังหวะ Pickconfirm จะต้องส่งข้อมูลไปหา tiktok บางส่วนผ่าน api tiktok
            - Market Place เป็น Lazada ขา packconfirm ต้องส่งข้อมูลบางส่วนไปหา lazada
        - ในกรณี Multiple Gateway:
            - Gateway A จะต้องส่ง update status run wave ไปหา Gateway ด้วย
            - Gateway B ไม่ต้องส่ง update status run wave ไปหา Gateway ด้วย
        - ระบบขายของกลุ่ม CMG ต้องสามารถ action ได้แค่ process data ของ CMG เท่านั้น ไม่สามารถไปจัดการข้อมูลของ CFR ได้ภายในระบบ OMS