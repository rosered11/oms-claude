namespace OmsApi;

public class TriggerRecalculateHandler(InMemoryStore store, OutboxAdapterService adapterService)
{
    public IResult Handle(string id)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);

        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(id, ApiResult.DomainEvent("RecalcRequested", o.Status, "POS recalculation triggered manually."));

        var promotions = store.GetOrderPromotions(id);
        var payload = PosRecalcPayload.Build(o, promotions);
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

        foreach (var evt in adapterService.Dispatch(id, o.ChannelType, o.SubChannel, o.BusinessUnit,
            "PosRecalculateEvent", payloadJson))
            store.AppendEvent(id, evt);

        return Results.Accepted(null, new { orderId = id, adjustedAmount = o.Amount, recalcTriggeredAt = DateTime.UtcNow });
    }
}
