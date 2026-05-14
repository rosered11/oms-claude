namespace OmsApi;

public record WaveStartedRequest(string OrderId, string WaveId, DateTime StartedAt);

public class WaveStartedHandler(InMemoryStore store)
{
    public IResult Handle(WaveStartedRequest req)
    {
        var order = store.FindOrder(req.OrderId);
        if (order is null) return ApiResult.NotFound("order", req.OrderId);

        if (order.Status != OrderStatus.PickStarted)
            return ApiResult.InvalidTransition(order.Status, "WaveStarted");

        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"WHL-{Guid.NewGuid():N}"[..12],
            SourceSystem = "WMS",
            EventType = "WaveStarted",
            Detail = $"Wave {req.WaveId} started at {req.StartedAt:O}",
            ReceivedAt = DateTime.UtcNow
        });

        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("WMS", "WaveStarted", order.Status,
            $"Wave {req.WaveId} started."));

        store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("Gateway", "WaveStartedSentToGW",
            "Dispatched to Gateway."));

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, waveId = req.WaveId });
    }
}
