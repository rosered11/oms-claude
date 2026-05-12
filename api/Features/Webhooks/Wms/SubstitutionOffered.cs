namespace OmsApi;

public record SubstitutionOfferedRequest(
    string OrderId, string OrderLineId,
    string SubstituteSku, string SubstituteProductName,
    decimal SubstituteUnitPrice, decimal SubstitutedAmount,
    DateTime OfferedAt);

public class SubstitutionOfferedHandler(InMemoryStore store)
{
    public IResult Handle(SubstitutionOfferedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);

        var line = o.Lines.FirstOrDefault(l => l.Id == req.OrderLineId);
        var subId = $"sub-{store.GetSubstitutions(req.OrderId).Count + 1:D3}";

        var sub = store.AddSubstitution(new SubstitutionDto
        {
            SubstitutionId = subId,
            OrderId = req.OrderId,
            OrderLineId = req.OrderLineId,
            OriginalSku = line?.Sku ?? req.OrderLineId,
            OriginalProductName = line?.ProductName ?? req.OrderLineId,
            SubstituteSku = req.SubstituteSku,
            SubstituteProductName = req.SubstituteProductName,
            SubstituteUnitPrice = req.SubstituteUnitPrice,
            SubstitutedAmount = req.SubstitutedAmount,
            CreatedAt = req.OfferedAt
        });

        o.SubstitutionFlag = true;
        o.PosRecalcPending = true;
        o.UpdatedAt = DateTime.UtcNow;

        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "WMS",
            EventType = "SubstitutionOffered",
            Detail = $"Substitute {req.SubstituteSku} offered for line {req.OrderLineId}.",
            ReceivedAt = DateTime.UtcNow
        });

        return Results.Accepted(null, new
        {
            accepted = true,
            substitutionId = subId,
            orderId = req.OrderId,
            customerNotified = true,
            posRecalcPending = true
        });
    }
}
