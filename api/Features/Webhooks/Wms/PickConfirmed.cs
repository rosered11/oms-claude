namespace OmsApi;

public record PickLineDto(string OrderLineId, string Sku, decimal PickedQty, bool Substituted);
public record PickConfirmedRequest(string OrderId, List<PickLineDto> Lines, DateTime PickedAt);

public class PickConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(PickConfirmedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status != OrderStatus.PickStarted) return ApiResult.InvalidTransition(o.Status, OrderStatus.PickConfirmed);

        foreach (var pick in req.Lines)
        {
            var line = o.Lines.FirstOrDefault(l => l.Id == pick.OrderLineId);
            if (line is not null)
            {
                line.PickedAmount = pick.PickedQty;
                line.IsSubstitute = pick.Substituted;
            }
        }

        o.Status = OrderStatus.PickConfirmed;
        o.SubstitutionFlag = req.Lines.Any(l => l.Substituted);
        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("WMS", "PickConfirmed", OrderStatus.PickConfirmed,
            $"{req.Lines.Count} lines confirmed picked at {req.PickedAt:o}."));
        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "WMS",
            EventType = "PickConfirmed",
            Detail = $"{req.Lines.Count} lines picked.",
            ReceivedAt = DateTime.UtcNow
        });
        foreach (var evt in ApiResult.DispatchOutbox(store, o.ChannelType, o.SubChannel, o.BusinessUnit,
            "PickConfirmedEvent", $"SC → {{target}}: Pick Confirmed for {req.OrderId}"))
            store.AppendEvent(req.OrderId, evt);
        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, newStatus = OrderStatus.PickConfirmed });
    }
}
