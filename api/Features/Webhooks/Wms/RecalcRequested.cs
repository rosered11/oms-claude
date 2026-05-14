namespace OmsApi;

public record RecalcRequestedRequest(string OrderId, string Reason, DateTime RequestedAt);

public class RecalcRequestedHandler(InMemoryStore store)
{
    public IResult Handle(RecalcRequestedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);

        if (o.PosRecalcPending)
            return Results.Conflict(new { error = "pos_recalc_already_pending",
                detail = $"Order {req.OrderId} already has a POS recalculation in progress." });

        o.PosRecalcPending = true;
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
        store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("POS", "RecalculationForwarded",
            $"SC → POS: Recalculation forwarded. Current basket: {o.Amount} THB."));

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, posRecalcPending = true });
    }
}
