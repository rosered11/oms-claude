namespace OmsApi;

public record PartialPickLineRequest(string OrderLineId, int PickedQuantity, int OrderedQuantity, string Reason);
public record PartialPickRequest(List<PartialPickLineRequest> Lines, string? IdempotencyKey);

public class PartialPickHandler(InMemoryStore store)
{
    private static readonly HashSet<string> AllowedStatuses =
        [OrderStatus.PickStarted];

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

        order.UpdatedAt = now;

        store.AppendEvent(id, ApiResult.DomainEvent("PartialPickRecorded", order.Status,
            $"{req.Lines.Count} line(s) partially picked."));
        store.AppendEvent(id, ApiResult.DomainEvent("PosRecalcCalled", order.Status,
            $"SC → POS [outbound]: recalculation for partial pick. {req.Lines.Count} line(s) adjusted."));

        return Results.Ok(new { orderId = id, status = order.Status, partialLines });
    }
}
