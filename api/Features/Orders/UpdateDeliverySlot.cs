namespace OmsApi;

public record UpdateDeliverySlotRequest(DateTime ScheduledStart, DateTime ScheduledEnd,
    string? BookedVia = null, string? BookingRef = null, string? Reason = null);

public class UpdateDeliverySlotHandler(InMemoryStore store)
{
    public IResult Handle(string id, UpdateDeliverySlotRequest req)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);
        if (!OrderStatus.CanUpdateSlot(o.Status))
            return Results.Conflict(new { error = "slot_change_not_allowed", detail = $"Order {id} is already {o.Status}. Slot cannot be changed." });

        var now = DateTime.UtcNow;
        o.DeliverySlot ??= new DeliverySlotDto
        {
            SlotId = $"SLOT-{Guid.NewGuid():N}"[..12],
            StoreId = o.StoreId,
            CreatedAt = now
        };

        o.DeliverySlot.History.Add(new SlotHistoryEntryDto
        {
            ScheduledStart = o.DeliverySlot.ScheduledStart,
            ScheduledEnd   = o.DeliverySlot.ScheduledEnd,
            BookedVia      = o.DeliverySlot.BookedVia,
            BookingRef     = o.DeliverySlot.BookingRef,
            Reason         = req.Reason,
            ChangedAt      = now
        });

        o.DeliverySlot.ScheduledStart = req.ScheduledStart;
        o.DeliverySlot.ScheduledEnd   = req.ScheduledEnd;
        o.DeliverySlot.BookedVia      = req.BookedVia ?? o.DeliverySlot.BookedVia;
        o.DeliverySlot.BookingRef     = req.BookingRef ?? o.DeliverySlot.BookingRef;
        o.DeliverySlot.UpdatedAt      = now;
        o.UpdatedAt = now;

        store.AppendEvent(id, ApiResult.DomainEvent("DeliverySlotRescheduled", o.Status,
            $"Slot rescheduled to {req.ScheduledStart:o}–{req.ScheduledEnd:o}. Reason: {req.Reason ?? "n/a"}"));
        store.AppendEvent(id, ApiResult.OutboxEvent("TMS", "DeliverySlotRescheduledEvent",
            $"SC → TMS: New slot {req.ScheduledStart:o}–{req.ScheduledEnd:o} for order {id} → tms.reschedule-slot"));

        return Results.Ok(new
        {
            orderId = id,
            deliverySlot = new
            {
                scheduledStart = o.DeliverySlot.ScheduledStart,
                scheduledEnd   = o.DeliverySlot.ScheduledEnd
            }
        });
    }
}
