namespace OmsApi;

public class GetPurchaseOrdersHandler(InMemoryStore store)
{
    public IResult Handle()
    {
        var items = store.PurchaseOrders;
        return Results.Ok(new { items, total = items.Count, page = 1, limit = 200 });
    }
}
