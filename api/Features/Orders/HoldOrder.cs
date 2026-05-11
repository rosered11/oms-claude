namespace OmsApi;

public record HoldOrderRequest(string HoldReason, string HeldBy);

public class HoldOrderHandler(InMemoryStore store)
{
    public IResult Handle(string id, HoldOrderRequest req)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);
        if (o.Status is OrderStatus.Cancelled or OrderStatus.Delivered or OrderStatus.Paid)
            return ApiResult.InvalidTransition(o.Status, OrderStatus.OnHold);

        o.PreHoldStatus = o.Status;
        o.Status = OrderStatus.OnHold;
        o.HoldReason = req.HoldReason;
        o.UpdatedAt = DateTime.UtcNow;
        o.UpdatedBy = req.HeldBy;
        store.AppendEvent(id, ApiResult.DomainEvent("OrderOnHold", OrderStatus.OnHold,
            $"Held: {req.HoldReason}"));
        return Results.Ok(o);
    }
}
