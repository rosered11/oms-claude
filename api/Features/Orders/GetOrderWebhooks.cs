namespace OmsApi;

public class GetOrderWebhooksHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);
        return Results.Ok(new { orderId = id, webhooks = store.GetWebhookLogs(id) });
    }
}
