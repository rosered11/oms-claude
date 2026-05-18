namespace OmsApi;

public record RecalcRequestedRequest(string OrderId, string Reason, DateTime RequestedAt);

public class RecalcRequestedHandler(InMemoryStore store, OutboxAdapterService adapterService)
{
    public IResult Handle(RecalcRequestedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);

        o.UpdatedAt = DateTime.UtcNow;

        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "WMS",
            EventType = "RecalcRequested",
            Detail = $"WMS requested POS recalculation. Reason: {req.Reason}.",
            ReceivedAt = DateTime.UtcNow
        });
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("WMS", "RecalcRequested", o.Status,
            $"WMS requested POS recalculation. Reason: {req.Reason}."));

        var promotions = store.GetOrderPromotions(req.OrderId);
        var payload = System.Text.Json.JsonSerializer.Serialize(PosRecalcPayload.Build(o, promotions));

        foreach (var evt in adapterService.Dispatch(req.OrderId, o.ChannelType, o.SubChannel,
            o.BusinessUnit, "PosRecalculateEvent", payload))
            store.AppendEvent(req.OrderId, evt);

        store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("Gateway", "RecalcRequestedEvent",
            $"SC → Gateway: recalc triggered by RecalcRequested for order {req.OrderId} → Gateway.recalc-requested"));

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, adjustedAmount = o.Amount });
    }
}
