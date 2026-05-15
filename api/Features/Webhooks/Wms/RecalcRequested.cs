namespace OmsApi;

public record RecalcRequestedRequest(string OrderId, string Reason, DateTime RequestedAt);

public class RecalcRequestedHandler(InMemoryStore store)
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
        store.AppendEvent(req.OrderId, ApiResult.DomainEvent("PosRecalcCalled", o.Status,
            $"SC → POS [outbound]: recalculation completed synchronously. Adjusted basket: {o.Amount} THB."));
        store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("GW", "RecalcRequestedEvent",
            $"SC → GW: recalc triggered by RecalcRequested for order {req.OrderId} → gw.recalc-requested"));

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, adjustedAmount = o.Amount });
    }
}
