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

        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("POS", "RecalcCompleted", o.Status,
            $"Recalculation closed. Final amount: {req.FinalAmount} {req.Currency}."));

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, posRecalcPending = false });
    }
}
