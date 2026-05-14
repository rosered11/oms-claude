namespace OmsApi;

public class TriggerRecalculateHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);

        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(id, ApiResult.DomainEvent("RecalcRequested", o.Status, "POS recalculation triggered manually."));
        store.AppendEvent(id, ApiResult.DomainEvent("PosRecalcCalled", o.Status,
            $"SC → POS [outbound]: recalculation triggered. Current basket: {o.Amount} THB. Promo engine and tax calculation applied."));
        return Results.Accepted(null, new { orderId = id, adjustedAmount = o.Amount, recalcTriggeredAt = DateTime.UtcNow });
    }
}
