@@ -1,40 +0,0 @@
# OMS flow POD

## Context

This is the OMS that your teams is managing. Details on how the OMS works can be found in the path docs.

And all incoming requirements should be learned and incorporated into your team's knowledge base, according to each person's role.

## Requirement

I want you to create or update e2e follow use cases, with the details of OMS in these documents. By the path of e2e is 'cypress'

and then I want to every cancel order from UI. It must to send outbox to TMS, WMS and Gateway.

## Path documents

- docs

## Use Case

- Customer place order via Web of bu CMG. payment method is prepaid
- Customer place order via Web of bu CFR. payment method is prepaid
- Customer place order via Tiktok of bu CMG. payment method is prepaid but TikTok needs OMS call api to get the AWB after Out For Delivery
- Customer place order via Web of bu CFR. payment method is POD
- Customer place order via Web of bu CFR. payment method is POD.
    - ราคาหมู กิโลกรัมละ 127 บาท, ลูกค้าซื้อ 841.23 กรัม
    - ราคาเป็ด 2.5 kg รวมราคา 99 บาท, ลูกค้าซื้อ 1.23 kg
- Customer place order via Web of bu CFR 
    - ในกรณี สั่งเนื้อกับไก่ แต่เอาแค่ไก่ เนื้อไม่สด ลูกค้าเลยไม่เอา สามารถคืนเงินได้
- Stock A transfer order to Stock B
- Customer postpone delivery date
- Customer cancel order
- Customer place order สั่งน้ำ 2 packs, กับ น้ำยาล้างจาน แต่น้ำยาล้างจานไม่มี เลยเอาแค่น้ำ
- Customer place order สั่งน้ำ 2 packs, กับ น้ำยาปรับผ้านุ่ม แต่น้ำยาปรับผ้านุ่มไม่มี เลย เอาน้ำยาล้างจานแทน แต่ต้องคืนเงินส่วนต่างให้ลูกค้า เนื่องจากน้ำยาปรับผ้านุ่มราคาแพงกว่า
- Customer สั่งสินค้า ได้รับสินค้าแล้ว แต่ต้องการขอคืนสินค้า
- Customer place order แต่มีใช้ส่วนลดคูปองในการจ่ายเงินด้วย
- Customer place order ใน payment flow Prepaid 2 ชิ้น น้ำยาล้างจาน กับ น้ำเปล่า หลังจากลูกค้าได้รับสินค้าแล้ว ลูกค้าทำเรื่องขอคืนน้ำยาล้างจาน