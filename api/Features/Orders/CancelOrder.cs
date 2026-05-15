namespace OmsApi;

public record CancelOrderRequest(string Reason, string CancelledBy);

public class CancelOrderHandler(InMemoryStore store)
{
    public IResult Handle(string id, CancelOrderRequest req)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);
        if (!OrderStatus.CanCancel(o.Status))
            return ApiResult.InvalidTransition(o.Status, OrderStatus.Cancelled);

        o.Status = OrderStatus.Cancelled;
        o.UpdatedAt = DateTime.UtcNow;
        o.UpdatedBy = req.CancelledBy;
        store.AppendEvent(id, ApiResult.DomainEvent("OrderCancelled", OrderStatus.Cancelled,
            $"Cancelled: {req.Reason}"));
        return Results.Ok(new { id, newStatus = OrderStatus.Cancelled });
    }
}
