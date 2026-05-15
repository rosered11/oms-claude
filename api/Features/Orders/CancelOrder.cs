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
            $"Cancelled by {req.CancelledBy}: {req.Reason}. Status → Cancelled."));
        store.AppendEvent(id, ApiResult.OutboxEvent("WMS", "OrderCancelledSentToWMS",
            $"SC → WMS: OrderCancelled. Reverse stock reservation for order {id}."));
        store.AppendEvent(id, ApiResult.OutboxEvent("TMS", "OrderCancelledSentToTMS",
            $"SC → TMS: OrderCancelled. Cancel delivery booking for order {id}."));
        store.AppendEvent(id, ApiResult.OutboxEvent("GW", "OrderCancelledSentToGW",
            $"SC → GW: OrderCancelled. Notify customer of cancellation for order {id}."));

        return Results.Ok(new { id, newStatus = OrderStatus.Cancelled });
    }
}
