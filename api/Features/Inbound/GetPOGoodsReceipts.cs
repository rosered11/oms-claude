namespace OmsApi;

public class GetPOGoodsReceiptsHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var po = store.FindPO(id);
        if (po is null) return ApiResult.NotFound("purchase order", id);
        return Results.Ok(new { purchaseOrderId = id, goodsReceipts = store.GetGoodsReceipts(id) });
    }
}
