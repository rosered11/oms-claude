namespace OmsApi;

public class TriggerRecalculateHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);

        o.PosRecalcPending = true;
        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(id, ApiResult.DomainEvent("RecalcRequested", o.Status, "POS recalculation triggered by WMS mid-pick."));
        store.AppendEvent(id, ApiResult.OutboxEvent("POS", "RecalculationForwarded",
            $"SC → POS: Recalculation forwarded. Current basket: {o.Amount} THB. Promo engine and tax calculation applied."));
        return Results.Accepted(null, new { orderId = id, posRecalcPending = true, recalcTriggeredAt = DateTime.UtcNow });
    }
}
