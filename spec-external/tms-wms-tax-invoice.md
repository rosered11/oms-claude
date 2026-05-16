## Request

| Name                                  | Param Type     | Data Type    | Mandatory | Description                  | Comments                                                                                               |
| ------------------------------------- | -------------- | ------------ | --------- | ---------------------------- | ------------------------------------------------------------------------------------------------------ |
| x-api-key                             | Request header | string       | M         | x-api-key                    |                                                                                                        |
| x-channel                             | Request header | string       | M         | TWD                          |                                                                                                        |
| order_id                              | Request Body   | string       | M         | เลขที่คำสั่งซื้อ             |                                                                                                        |
| sale_channel                          | Request Body   | string       | M         | ช่องทางการขายสำหรับออกบิล    |                                                                                                        |
| sale_source                           | Request Body   | string       | M         | ช่องทางการขาย                | - CF = ChefYim<br>- CO = ChoYim<br>- WA<br>- XB                                                        |
| document_type                         | Request Body   | string       | M         | ประเภทเอกสาร                 | - ABB = ใบเสร็จอย่างย่อ<br>- INV = ใบกำกับภาษีเต็มรูป<br>- CN = ใบลดหนี้<br>- CL_DEPOSIT = ใบล้างมัดจำ |
| documents                             | Request Body   | array object | M         | ข้อมูลเอกสาร                 |                                                                                                        |
| documents[].abb_id                    | Request Body   | string       | M         | เลขที่ใบเสร็จ ABB            |                                                                                                        |
| documents[].tax_invoice_id            | Request Body   | string       | M         | เลขที่ใบกำกับภาษี            |                                                                                                        |
| documents[].cn_abb_id                 | Request Body   | string       | O         | เลขที่ใบลดหนี้ - ใบเสร็จ ABB |                                                                                                        |
| documents[].cn_tax_id                 | Request Body   | string       | O         | เลขที่ใบลดหนี้ - ใบกำกับภาษี |                                                                                                        |
| documents[].url                       | Request Body   | string       | M         | url สำหรับ download เอกสาร   | value = blank (if is_success = false)                                                                  |
| documents[].is_success                | Request Body   | boolean      | M         | สถานะการสร้างใบกำกับภาษี     | - true = สร้างเอกสารไม่สำเร็จ<br>- false = สร้างเอกสารสำเร็จ                                           |
| documents[].document_created_datetime | Request Body   | datetime     | M         | วันที่จัดส่งเอกสาร           |                                                                                                        |


## Response - 204