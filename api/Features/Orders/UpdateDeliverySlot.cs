namespace OmsApi;

public record UpdateDeliverySlotRequest(DateTime ScheduledStart, DateTime ScheduledEnd);

public class UpdateDeliverySlotHandler(InMemoryStore store)
{
    public IResult Handle(string id, UpdateDeliverySlotRequest req)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);
        if (!OrderStatus.CanUpdateSlot(o.Status))
            return Results.Conflict(new { error = "invalid_transition", detail = $"Order {id} is already {o.Status}. Slot cannot be changed." });

        o.DeliverySlot ??= new DeliverySlotDto { SlotId = $"SLOT-{Guid.NewGuid():N}"[..12], StoreId = o.StoreId };
        o.DeliverySlot.ScheduledStart = req.ScheduledStart;
        o.DeliverySlot.ScheduledEnd = req.ScheduledEnd;
        o.UpdatedAt = DateTime.UtcNow;

        return Results.Ok(new
        {
            orderId = id,
            slotId = o.DeliverySlot.SlotId,
            scheduledStart = o.DeliverySlot.ScheduledStart,
            scheduledEnd = o.DeliverySlot.ScheduledEnd
        });
    }
}
