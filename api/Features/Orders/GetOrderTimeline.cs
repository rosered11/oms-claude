namespace OmsApi;

public class GetOrderTimelineHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        if (store.FindOrder(id) is null) return ApiResult.NotFound("order", id);
        return Results.Ok(store.GetTimeline(id));
    }
}
