namespace OmsApi;

public record PickStartedRequest(string OrderId, string PickerId, DateTime StartedAt);

public class PickStartedHandler(InMemoryStore store)
{
    public IResult Handle(PickStartedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status != OrderStatus.BookingConfirmed) return ApiResult.InvalidTransition(o.Status, OrderStatus.PickStarted);

        o.Status = OrderStatus.PickStarted;
        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("WMS", "PickStarted", OrderStatus.PickStarted,
            $"Picker {req.PickerId} started at {req.StartedAt:o}."));
        return Results.Ok(o);
    }
}
