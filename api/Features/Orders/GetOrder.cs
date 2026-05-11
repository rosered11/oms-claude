namespace OmsApi;

public class GetOrderHandler(InMemoryStore store)
{
    public IResult Handle(string id) =>
        store.FindOrder(id) is { } o ? Results.Ok(o) : ApiResult.NotFound("order", id);
}
