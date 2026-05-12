namespace OmsApi;

public record CollectedRequest(string OrderId, DateTime CollectedAt, string CollectedBy);

public class CollectedHandler(InMemoryStore store)
{
    public IResult Handle(CollectedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status != OrderStatus.ReadyForCollection)
            return ApiResult.InvalidTransition(o.Status, OrderStatus.Collected);

        o.Status = OrderStatus.Collected;
        o.UpdatedAt = DateTime.UtcNow;

        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("POS", "Collected", OrderStatus.Collected,
            $"Order collected by {req.CollectedBy} at {req.CollectedAt:o}."));
        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "POS",
            EventType = "Collected",
            Detail = $"Collected by {req.CollectedBy}.",
            ReceivedAt = DateTime.UtcNow
        });

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, newStatus = OrderStatus.Collected, invoiceTriggered = true });
    }
}
