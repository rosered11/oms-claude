namespace OmsApi;

public class GetDeliverySlotHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);
        if (o.DeliverySlot is null)
            return Results.NotFound(new { error = "not_found", detail = $"Order {id} has no delivery slot assigned." });

        return Results.Ok(new
        {
            orderId = id,
            deliverySlot = new
            {
                slotId         = o.DeliverySlot.SlotId,
                scheduledStart = o.DeliverySlot.ScheduledStart,
                scheduledEnd   = o.DeliverySlot.ScheduledEnd,
                storeId        = o.DeliverySlot.StoreId,
                bookedVia      = o.DeliverySlot.BookedVia,
                bookingRef     = o.DeliverySlot.BookingRef,
                history        = o.DeliverySlot.History
            }
        });
    }
}
