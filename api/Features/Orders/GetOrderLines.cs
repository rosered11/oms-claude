namespace OmsApi;

public class GetOrderLinesHandler(InMemoryStore store)
{
    public IResult Handle(string id) =>
        store.FindOrder(id) is { } o ? Results.Ok(o.Lines) : ApiResult.NotFound("order", id);
}
