namespace OmsApi;

public class GetTransferOrderHandler(InMemoryStore store)
{
    public IResult Handle(string id) =>
        store.FindTO(id) is { } to ? Results.Ok(to) : ApiResult.NotFound("transfer-order", id);
}
