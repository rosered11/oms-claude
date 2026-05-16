@@ -1,40 +0,0 @@
# OMS

## Context

This is the OMS that your teams is managing. Details on how the OMS works can be found in the path docs.

And all incoming requirements should be learned and incorporated into your team's knowledge base, according to each person's role.

## Requirement

I want you to implement adapter for outbox, following the api spec external. by I must be able to configure the URL, header and if have call authorize before calling the service external, I must be able to can config http for calling authorize, and then it will call the target outbox.

After this, when OMS creates an event outbox, following the external spec, it must call api to the target is complete. And then it will create an event timeline. But if it isn't complete. I  must be able to monitor the case.

And I need you to create a document that maps fields to all external specifications. For example, what fields and tables are mapped to that field and sent to the external system.

## Outbox relation to Spec

- In case OMS call POS to Recalculation use spec on path spec-external/pos-recalc.md
- In case OMS update status to GW like: ware start, out for delivery and etc.. use spec on path the spec-external/gw-update-status.md
- In case when receive webhook in from STS, OMS must to send Tax Invoice to WMS or TMS. use spec on path the spec-external/tms-wms-tax-invoice.md
- In case when receive webhook in from STS, OMS must to send Credit Note to WMS or TMS. use spec on path the spec-external/tws-wms-credit-note.md
