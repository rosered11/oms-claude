namespace OmsApi;

public record RecalcPromoDto(string PromoCode, decimal DiscountAmount, string Description);
public record RecalcResultRequest(string OrderId, decimal OriginalAmount, decimal AdjustedAmount,
    string Currency, List<RecalcPromoDto> PromotionsApplied, DateTime RecalculatedAt);

public class RecalcResultHandler(InMemoryStore store)
{
    public IResult Handle(RecalcResultRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);

        o.Amount = req.AdjustedAmount;
        o.PosRecalcPending = false;
        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("POS", "RecalcResult", o.Status,
            $"Amount adjusted {req.OriginalAmount} → {req.AdjustedAmount} {req.Currency}."));
        return Results.Ok(o);
    }
}
