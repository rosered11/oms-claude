@@ -1,40 +0,0 @@
# OMS

## Context

This is the OMS that your teams is managing. Details on how the OMS works can be found in the path docs.

And all incoming requirements should be learned and incorporated into your team's knowledge base, according to each person's role.

## Requirement

I want you to remove all authen from the OMS. and please review file docs/oms-api-blueprint.md. after i run aglio convert to html i receive log follow bellow.

After run aglio, this is output file docs/output.html i think it isn't complete.

## Example log Aglio

```
>> Line 10: action is missing a response (warning code 6)
>> Context
       ...
        **Auth:** Bearer JWT on all endpoints (except `POST /auth/token`)

       ---

>>>>   ## Authentication

       ### POST /auth/token

       Obtain a Bearer JWT for API access.

       ...
>> Line 36: action is missing a response (warning code 6)
>> Context
       ...
        ```

       ---

>>>>   ## Group: Orders
```