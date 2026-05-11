namespace OmsApi;

public record BookingConfirmedRequest(string OrderId, string WmsBookingRef, DateTime ConfirmedAt);

public class BookingConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(BookingConfirmedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status != OrderStatus.Pending) return ApiResult.InvalidTransition(o.Status, OrderStatus.BookingConfirmed);

        o.Status = OrderStatus.BookingConfirmed;
        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("WMS", "BookingConfirmed", OrderStatus.BookingConfirmed,
            $"WMS booking ref {req.WmsBookingRef} confirmed at {req.ConfirmedAt:o}."));
        return Results.Ok(o);
    }
}
