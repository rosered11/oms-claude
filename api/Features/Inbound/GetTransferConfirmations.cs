namespace OmsApi;

public class GetTransferConfirmationsHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var to = store.FindTO(id);
        if (to is null) return ApiResult.NotFound("transfer order", id);
        return Results.Ok(new { transferOrderId = id, confirmations = store.GetTransferConfirmations(id) });
    }
}
