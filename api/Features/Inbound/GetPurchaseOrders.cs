namespace OmsApi;

public class GetPurchaseOrdersHandler(InMemoryStore store)
{
    public IResult Handle() => Results.Ok(store.PurchaseOrders);
}
