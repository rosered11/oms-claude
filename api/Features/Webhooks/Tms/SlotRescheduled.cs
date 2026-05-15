namespace OmsApi;

public record TmsSlotRescheduledRequest(
    string OrderId,
    DateTime NewScheduledStart,
    DateTime NewScheduledEnd,
    string? BookingRef,
    string? Reason,
    DateTime RescheduledAt);

public class TmsSlotRescheduledHandler(InMemoryStore store)
{
    public IResult Handle(TmsSlotRescheduledRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (!OrderStatus.CanUpdateSlot(o.Status))
            return Results.Conflict(new { error = "slot_change_not_allowed", detail = $"Order {req.OrderId} is already {o.Status}. Slot cannot be changed." });

        var now = DateTime.UtcNow;
        o.DeliverySlot ??= new DeliverySlotDto
        {
            SlotId    = $"SLOT-{Guid.NewGuid():N}"[..12],
            StoreId   = o.StoreId,
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

        o.DeliverySlot.ScheduledStart = req.NewScheduledStart;
        o.DeliverySlot.ScheduledEnd   = req.NewScheduledEnd;
        o.DeliverySlot.BookedVia      = "TMS";
        o.DeliverySlot.BookingRef     = req.BookingRef ?? o.DeliverySlot.BookingRef;
        o.DeliverySlot.UpdatedAt      = now;
        o.UpdatedAt = now;

        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "TMS",
            EventType    = "SlotRescheduled",
            Detail       = $"TMS rescheduled slot to {req.NewScheduledStart:o}–{req.NewScheduledEnd:o}. Reason: {req.Reason ?? "n/a"}.",
            ReceivedAt   = now
        });

        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("TMS", "SlotRescheduled", o.Status,
            $"TMS rescheduled delivery to {req.NewScheduledStart:o}–{req.NewScheduledEnd:o}. Reason: {req.Reason ?? "n/a"}."));
        store.AppendEvent(req.OrderId, ApiResult.DomainEvent("DeliverySlotRescheduled", o.Status,
            $"Slot updated from TMS notification. New window: {req.NewScheduledStart:o}–{req.NewScheduledEnd:o}."));
        store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("WMS", "DeliverySlotRescheduledEvent",
            $"SC → WMS: Delivery slot updated for order {req.OrderId}. New window: {req.NewScheduledStart:o}–{req.NewScheduledEnd:o} → wms.slot-update"));
        return Results.Accepted(null, new
        {
            accepted    = true,
            orderId     = req.OrderId,
            deliverySlot = new
            {
                scheduledStart = o.DeliverySlot.ScheduledStart,
                scheduledEnd   = o.DeliverySlot.ScheduledEnd
            }
        });
    }
}
