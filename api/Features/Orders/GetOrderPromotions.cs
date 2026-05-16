namespace OmsApi;

public class GetOrderPromotionsHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var order = store.FindOrder(id);
        if (order is null) return ApiResult.NotFound("order", id);

        var promotions = store.GetOrderPromotions(id);
        var totalDiscount = promotions.Sum(p => p.DiscountAmount);
        return Results.Ok(new { promotions, totalDiscount, currency = "THB" });
    }
}
