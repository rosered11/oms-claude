namespace OmsApi;

public class GetReturnHandler(InMemoryStore store)
{
    public IResult Handle(string id) =>
        store.FindReturn(id) is { } r ? Results.Ok(r) : ApiResult.NotFound("return", id);
}
