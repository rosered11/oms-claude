namespace OmsApi;

public record GoodsReceiptLineRequest(string Sku, decimal ReceivedQty);
public record GoodsReceiptConfirmedRequest(string PurchaseOrderId, string GoodsReceiveNo,
    List<GoodsReceiptLineRequest> Lines, DateTime ReceivedAt);

public class GoodsReceiptConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(GoodsReceiptConfirmedRequest req)
    {
        var po = store.FindPO(req.PurchaseOrderId);
        if (po is null) return ApiResult.NotFound("purchase order", req.PurchaseOrderId);

        po.GoodsReceiveNo = req.GoodsReceiveNo;
        po.Status = "FullyReceived";
        po.UpdatedAt = DateTime.UtcNow;

        store.AddGoodsReceipt(req.PurchaseOrderId, new GoodsReceiptDto
        {
            GoodsReceiveNo = req.GoodsReceiveNo,
            Status = "Received",
            ReceivedAt = req.ReceivedAt,
            Lines = req.Lines.Select(l => new GoodsReceiptLineDto
            {
                Sku = l.Sku,
                ReceivedQty = l.ReceivedQty,
                Condition = "Resellable"
            }).ToList()
        });

        return Results.Accepted(null, new { accepted = true, purchaseOrderId = req.PurchaseOrderId, newStatus = "FullyReceived" });
    }
}
