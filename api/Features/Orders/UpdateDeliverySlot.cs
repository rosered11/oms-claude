namespace OmsApi;

public record UpdateDeliverySlotRequest(DateTime ScheduledStart, DateTime ScheduledEnd,
    string? BookedVia = null, string? BookingRef = null);

public class UpdateDeliverySlotHandler(InMemoryStore store)
{
    public IResult Handle(string id, UpdateDeliverySlotRequest req)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);
        if (!OrderStatus.CanUpdateSlot(o.Status))
            return Results.Conflict(new { error = "invalid_transition", detail = $"Order {id} is already {o.Status}. Slot cannot be changed." });

        var now = DateTime.UtcNow;
        o.DeliverySlot ??= new DeliverySlotDto
        {
            SlotId = $"SLOT-{Guid.NewGuid():N}"[..12],
            StoreId = o.StoreId,
            CreatedAt = now
        };
        o.DeliverySlot.ScheduledStart = req.ScheduledStart;
        o.DeliverySlot.ScheduledEnd = req.ScheduledEnd;
        o.DeliverySlot.BookedVia = req.BookedVia ?? o.DeliverySlot.BookedVia;
        o.DeliverySlot.BookingRef = req.BookingRef ?? o.DeliverySlot.BookingRef;
        o.DeliverySlot.UpdatedAt = now;
        o.UpdatedAt = now;

        return Results.Ok(new
        {
            orderId = id,
            slotId = o.DeliverySlot.SlotId,
            scheduledStart = o.DeliverySlot.ScheduledStart,
            scheduledEnd = o.DeliverySlot.ScheduledEnd,
            bookedVia = o.DeliverySlot.BookedVia,
            bookingRef = o.DeliverySlot.BookingRef
        });
    }
}
