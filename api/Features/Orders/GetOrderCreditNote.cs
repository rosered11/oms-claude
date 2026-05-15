namespace OmsApi;

public class GetOrderCreditNoteHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var order = store.FindOrder(id);
        if (order is null) return ApiResult.NotFound("order", id);
        var cn = store.GetCreditNote(id);
        if (cn is null) return Results.NotFound(new
        {
            error_code = "NOT_FOUND",
            message = $"No credit note found for order '{id}'.",
            trace_id = Guid.NewGuid()
        });
        return Results.Ok(cn);
    }
}
