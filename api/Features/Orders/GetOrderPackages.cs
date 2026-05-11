namespace OmsApi;

public class GetOrderPackagesHandler(InMemoryStore store)
{
    public IResult Handle(string id) =>
        store.FindOrder(id) is { } o ? Results.Ok(o.Packages) : ApiResult.NotFound("order", id);
}
