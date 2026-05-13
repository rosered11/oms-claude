namespace OmsApi;

public record HoldOrderRequest(string HoldReason, string HeldBy);

public class HoldOrderHandler(InMemoryStore store)
{
    public IResult Handle(string id, HoldOrderRequest req)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);
        if (!OrderStatus.CanHold(o.Status))
            return ApiResult.InvalidTransition(o.Status, OrderStatus.OnHold);

        var now = DateTime.UtcNow;
        o.PreHoldStatus = o.Status;
        o.Status = OrderStatus.OnHold;
        o.HoldReason = req.HoldReason;
        o.UpdatedAt = now;
        o.UpdatedBy = req.HeldBy;

        store.AddOrderHold(new OrderHoldDto
        {
            HoldId = $"HOLD-{Guid.NewGuid():N}"[..12],
            OrderId = id,
            HoldReason = req.HoldReason,
            HeldAt = now,
            HeldBy = req.HeldBy
        });

        store.AppendEvent(id, ApiResult.DomainEvent("OrderOnHold", OrderStatus.OnHold,
            $"Held: {req.HoldReason}"));
        return Results.Ok(o);
    }
}
