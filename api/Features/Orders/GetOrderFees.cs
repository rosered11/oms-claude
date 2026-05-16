namespace OmsApi;

public class GetOrderFeesHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var order = store.FindOrder(id);
        if (order is null) return ApiResult.NotFound("order", id);

        var fees = store.GetOrderFees(id);
        var totalFees = fees.Sum(f => f.Amount);
        return Results.Ok(new { fees, totalFees, currency = "THB" });
    }
}
