namespace OmsApi;

public record PickStartedRequest(string OrderId, string PickerId, DateTime StartedAt);

public class PickStartedHandler(InMemoryStore store)
{
    public IResult Handle(PickStartedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status is not (OrderStatus.BookingConfirmed or OrderStatus.Pending))
            return ApiResult.InvalidTransition(o.Status, OrderStatus.PickStarted);

        o.Status = OrderStatus.PickStarted;
        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("WMS", "PickStarted", OrderStatus.PickStarted,
            $"Picker {req.PickerId} started at {req.StartedAt:o}."));
        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "WMS",
            EventType = "PickStarted",
            Detail = $"Picker {req.PickerId} started at {req.StartedAt:o}.",
            ReceivedAt = DateTime.UtcNow
        });
        store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("TMS", "PickStartedEvent",
            $"SC → TMS: PickStartedEvent dispatched. TMS notified for driver slot scheduling."));
        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, newStatus = OrderStatus.PickStarted });
    }
}
