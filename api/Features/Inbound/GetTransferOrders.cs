namespace OmsApi;

public class GetTransferOrdersHandler(InMemoryStore store)
{
    public IResult Handle()
    {
        var items = store.TransferOrders;
        return Results.Ok(new { items, total = items.Count, page = 1, limit = 200 });
    }
}
