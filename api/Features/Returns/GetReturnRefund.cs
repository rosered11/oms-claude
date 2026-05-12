namespace OmsApi;

public class GetReturnRefundHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var ret = store.FindReturn(id);
        if (ret is null) return ApiResult.NotFound("return", id);

        var refund = store.GetRefund(id);
        if (refund is null)
            return Results.NotFound(new { error = "not_found", detail = $"Return {id} has no refund record. Refund is created after put-away completes." });

        var creditNote = store.GetCreditNote(id);
        return Results.Ok(new { returnId = id, refund, creditNote });
    }
}
