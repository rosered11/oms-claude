namespace OmsApi;

public record PartialPickLineRequest(string OrderLineId, int PickedQuantity, int OrderedQuantity, string Reason);
public record PartialPickRequest(List<PartialPickLineRequest> Lines, string? IdempotencyKey);

public class PartialPickHandler(InMemoryStore store)
{
    private static readonly HashSet<string> AllowedStatuses =
        [OrderStatus.BookingConfirmed, OrderStatus.PickStarted];

    public IResult Handle(string id, PartialPickRequest req)
    {
        var order = store.FindOrder(id);
        if (order is null) return ApiResult.NotFound("order", id);

        if (!AllowedStatuses.Contains(order.Status))
            return Results.Conflict(new
            {
                error = "invalid_transition",
                detail = $"Order {id} is in status {order.Status}. Partial pick is not allowed from this state."
            });

        if (order.PosRecalcPending)
            return Results.Conflict(new
            {
                error = "pos_recalc_already_pending",
                detail = "POS recalculation already in progress."
            });

        if (req.Lines.All(l => l.PickedQuantity == 0))
            return Results.UnprocessableEntity(new
            {
                error = "zero_pick_not_allowed",
                detail = "All lines reduced to zero. Use Cancel Order instead."
            });

        var now = DateTime.UtcNow;
        var partialLines = new List<object>();

        foreach (var lineReq in req.Lines)
        {
            var line = order.Lines.FirstOrDefault(l =>
                l.Id.Equals(lineReq.OrderLineId, StringComparison.OrdinalIgnoreCase));
            if (line is null) continue;

            line.PickedAmount = lineReq.PickedQuantity;
            line.UpdatedAt = now;

            partialLines.Add(new
            {
                orderLineId = lineReq.OrderLineId,
                pickedQuantity = lineReq.PickedQuantity,
                orderedQuantity = lineReq.OrderedQuantity,
                shortfallQuantity = lineReq.OrderedQuantity - lineReq.PickedQuantity,
                reason = lineReq.Reason
            });
        }

        order.PosRecalcPending = true;
        order.UpdatedAt = now;

        store.AppendEvent(id, ApiResult.DomainEvent("PartialPickRecorded", order.Status,
            $"{req.Lines.Count} line(s) partially picked. POS recalc triggered."));

        return Results.Ok(new
        {
            orderId = id,
            status = order.Status,
            pos_recalc_pending = true,
            partialLines
        });
    }
}
