namespace OmsApi;

public class GetReturnsHandler(InMemoryStore store)
{
    public IResult Handle() => Results.Ok(store.Returns);
}
