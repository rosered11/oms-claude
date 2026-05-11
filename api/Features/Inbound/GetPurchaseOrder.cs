namespace OmsApi;

public class GetPurchaseOrderHandler(InMemoryStore store)
{
    public IResult Handle(string id) =>
        store.FindPO(id) is { } po ? Results.Ok(po) : ApiResult.NotFound("purchase-order", id);
}
