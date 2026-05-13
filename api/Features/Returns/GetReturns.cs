namespace OmsApi;

public class GetReturnsHandler(InMemoryStore store)
{
    public IResult Handle()
    {
        var items = store.Returns;
        return Results.Ok(new { items, total = items.Count, page = 1, limit = 200 });
    }
}
