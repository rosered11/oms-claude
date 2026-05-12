namespace OmsApi;

public record CollectionReadyRequest(string OrderId, string StoreId, DateTime NotifiedAt);

public class CollectionReadyHandler(InMemoryStore store)
{
    public IResult Handle(CollectionReadyRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status != OrderStatus.PickConfirmed)
            return ApiResult.InvalidTransition(o.Status, OrderStatus.ReadyForCollection);

        o.Status = OrderStatus.ReadyForCollection;
        o.UpdatedAt = DateTime.UtcNow;

        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("POS", "CollectionReady", OrderStatus.ReadyForCollection,
            $"Order ready for collection at store {req.StoreId}."));
        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "POS",
            EventType = "CollectionReady",
            Detail = $"Ready at {req.StoreId}.",
            ReceivedAt = DateTime.UtcNow
        });

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, newStatus = OrderStatus.ReadyForCollection });
    }
}
