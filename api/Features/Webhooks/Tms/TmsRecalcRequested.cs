namespace OmsApi;

public record TmsRecalcRequestedRequest(string TrackingId, string Reason, decimal ActualWeight, DateTime RequestedAt);

public class TmsRecalcRequestedHandler(InMemoryStore store)
{
    public IResult Handle(TmsRecalcRequestedRequest req)
    {
        var o = store.FindOrderByTracking(req.TrackingId);
        if (o is null) return ApiResult.NotFound("tracking", req.TrackingId);

        if (o.Status != OrderStatus.OutForDelivery)
            return ApiResult.InvalidTransition(o.Status, "PosRecalc");

        o.UpdatedAt = DateTime.UtcNow;

        store.AddWebhookLog(o.Id, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "TMS",
            EventType = "TmsRecalcRequested",
            Detail = $"TMS requested POS recalculation at door. Actual weight: {req.ActualWeight} kg. Reason: {req.Reason}.",
            ReceivedAt = DateTime.UtcNow
        });
        store.AppendEvent(o.Id, ApiResult.WebhookEvent("TMS", "TmsRecalcRequested", o.Status,
            $"TMS requested POS recalculation. Actual weight: {req.ActualWeight} kg. Reason: {req.Reason}."));
        store.AppendEvent(o.Id, ApiResult.DomainEvent("PosRecalcCalled", o.Status,
            $"SC → POS [outbound]: recalculation requested by driver at door. Actual weight: {req.ActualWeight} kg."));
        store.AppendEvent(o.Id, ApiResult.OutboxEvent("GW", "RecalcRequestedEvent",
            $"SC → GW: recalc triggered by TmsRecalcRequested for order {o.Id} → gw.recalc-requested"));

        return Results.Accepted(null, new { accepted = true, orderId = o.Id, adjustedAmount = o.Amount });
    }
}
