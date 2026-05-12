namespace OmsApi;

public class TriggerRecalculateHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);

        o.PosRecalcPending = true;
        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(id, ApiResult.DomainEvent("RecalcRequested", o.Status, "Manual POS recalculation triggered."));
        return Results.Accepted(null, new { orderId = id, posRecalcPending = true, recalcTriggeredAt = DateTime.UtcNow });
    }
}
