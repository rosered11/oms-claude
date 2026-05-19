@@ -1,40 +0,0 @@
# Order Management System

## Context

This is the OMS that your team is managing. Details on how the OMS works can be found in the path docs.

And all incoming requirements should be learned and incorporated into your team's knowledge base, according to each person's role.

## Requirement

I want you to follow this requirement:
- Remove these processes from the OMS, because the OMS isn't handling them.
    - BookingConfirmed
    - Invoiced
    - Paid
- I want you to review the name of `ApiResult.DispatchOutbox`. I should use this name for the handler about the TimelineEvent, or I should use another name. if must use another name you please recommend one for me.
- I want you to review the `Stock Ledger`.Should it be in the OMS System like?
- I want you remove branches/nearby from the OMS. because the OMS isn't handle this feature.
- I want you change field of is_prepaid to another type like: string. because in the future it maybe case more then 2 flows.
- I want you change all monetary value in OMS are stored from **satang** to **bath**