namespace OmsApi;

public class GetOrderTimelineHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var order = store.FindOrder(id);
        if (order is null) return ApiResult.NotFound("order", id);
        var events = store.GetTimeline(id);
        var inboundCount  = events.Count(e => string.Equals(e.Phase, "inbound",  StringComparison.OrdinalIgnoreCase));
        var outboundCount = events.Count(e => string.Equals(e.Phase, "outbound", StringComparison.OrdinalIgnoreCase));
        var firstEvent = events.OrderBy(e => e.OccurredAt).FirstOrDefault();
        var lastEvent  = events.OrderBy(e => e.OccurredAt).LastOrDefault();
        var spanMinutes = (lastEvent is not null && firstEvent is not null)
            ? (int)(lastEvent.OccurredAt - firstEvent.OccurredAt).TotalMinutes : 0;
        return Results.Ok(new
        {
            orderId = id,
            order = new
            {
                orderNumber = order.OrderNumber,
                status = order.Status,
                fulfillmentType = order.FulfillmentType,
                store = order.StoreId
            },
            events,
            summary = new
            {
                totalEvents = events.Count,
                inboundPhaseEvents = inboundCount,
                outboundPhaseEvents = outboundCount,
                totalEndToEndMinutes = spanMinutes
            }
        });
    }
}
