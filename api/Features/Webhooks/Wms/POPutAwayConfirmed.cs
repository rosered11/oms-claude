namespace OmsApi;

public record POPutAwayItemDto(string Sku, string Condition, string Sloc, int Qty);
public record POPutAwayConfirmedRequest(string PurchaseOrderId, List<POPutAwayItemDto> Items, DateTime PutAwayAt);

public class POPutAwayConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(POPutAwayConfirmedRequest req)
    {
        var po = store.FindPO(req.PurchaseOrderId);
        if (po is null) return ApiResult.NotFound("purchase order", req.PurchaseOrderId);

        po.Status = "Closed";
        po.UpdatedAt = DateTime.UtcNow;

        var receipts = store.GetGoodsReceipts(req.PurchaseOrderId);
        foreach (var receipt in receipts)
        {
            receipt.Status = "PutAway";
            receipt.PutAwayAt = req.PutAwayAt;
            foreach (var item in req.Items)
            {
                var line = receipt.Lines.FirstOrDefault(l => l.Sku.Equals(item.Sku, StringComparison.OrdinalIgnoreCase));
                if (line is not null) { line.Condition = item.Condition; line.Sloc = item.Sloc; }
            }
        }

        return Results.Accepted(null, new { accepted = true, purchaseOrderId = req.PurchaseOrderId, newStatus = "Closed" });
    }
}
