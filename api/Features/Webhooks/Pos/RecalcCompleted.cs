namespace OmsApi;

public record RecalcCompletedRequest(string OrderId, decimal FinalAmount, string Currency, DateTime CompletedAt);

public class RecalcCompletedHandler(InMemoryStore store)
{
    public IResult Handle(RecalcCompletedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);

        o.Amount = req.FinalAmount;
        o.PosRecalcPending = false;
        o.UpdatedAt = DateTime.UtcNow;

        store.AppendEvent(req.OrderId, ApiResult.DomainEvent("RecalcCompleted", o.Status,
            $"POS response received. Final amount: {req.FinalAmount} {req.Currency}. posRecalcPending cleared."));
        store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("WMS", "AmountSentToWMS",
            $"SC → WMS: Updated amount {req.FinalAmount} {req.Currency} confirmed back to warehouse."));

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, posRecalcPending = false });
    }
}
