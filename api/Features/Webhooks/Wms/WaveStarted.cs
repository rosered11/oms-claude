namespace OmsApi;

public record WaveStartedRequest(string OrderId, string WaveId, DateTime StartedAt);

public class WaveStartedHandler(InMemoryStore store, OutboxAdapterService adapterService)
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

        var payment = store.GetOrderPayment(req.OrderId);
        var payload = System.Text.Json.JsonSerializer.Serialize(
            GwUpdateStatusPayload.Build(order, payment, "WAVE_STARTED"));

        foreach (var evt in adapterService.Dispatch(req.OrderId, order.ChannelType, order.SubChannel,
            order.BusinessUnit, "WaveStartedSentToGW", payload))
            store.AppendEvent(req.OrderId, evt);

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, waveId = req.WaveId });
    }
}
