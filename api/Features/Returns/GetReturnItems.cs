namespace OmsApi;

public class GetReturnItemsHandler(InMemoryStore store)
{
    public IResult Handle(string id) =>
        store.FindReturn(id) is { } r ? Results.Ok(r.Items) : ApiResult.NotFound("return", id);
}
