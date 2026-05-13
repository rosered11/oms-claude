namespace OmsApi;

public record GoodsReceiptLineRequest(string Sku, decimal ReceivedQty, string Condition = "Resellable",
    string? Sloc = null);
public record GoodsReceiptConfirmedRequest(string PurchaseOrderId, string GoodsReceiveNo,
    List<GoodsReceiptLineRequest> Lines, DateTime ReceivedAt, string? UpdatedBy = null);

public class GoodsReceiptConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(GoodsReceiptConfirmedRequest req)
    {
        var po = store.FindPO(req.PurchaseOrderId);
        if (po is null) return ApiResult.NotFound("purchase order", req.PurchaseOrderId);

        var now = DateTime.UtcNow;
        po.GoodsReceiveNo = req.GoodsReceiveNo;
        po.Status = "FullyReceived";
        po.UpdatedAt = now;
        po.UpdatedBy = req.UpdatedBy;

        foreach (var line in req.Lines)
        {
            var poLine = po.Lines.FirstOrDefault(l =>
                l.Sku.Equals(line.Sku, StringComparison.OrdinalIgnoreCase));
            if (poLine is not null)
            {
                poLine.ReceivedQty = (int)line.ReceivedQty;
                poLine.Condition = line.Condition;
                poLine.Sloc = line.Sloc ?? poLine.Sloc;
                poLine.ReceivedAt = req.ReceivedAt;
            }
        }

        store.AddGoodsReceipt(req.PurchaseOrderId, new GoodsReceiptDto
        {
            GoodsReceiveNo = req.GoodsReceiveNo,
            Status = "Received",
            ReceivedAt = req.ReceivedAt,
            Lines = req.Lines.Select(l => new GoodsReceiptLineDto
            {
                Sku = l.Sku,
                ReceivedQty = l.ReceivedQty,
                Condition = l.Condition,
                Sloc = l.Sloc ?? ""
            }).ToList()
        });

        return Results.Accepted(null, new { accepted = true, purchaseOrderId = req.PurchaseOrderId, newStatus = "FullyReceived" });
    }
}
