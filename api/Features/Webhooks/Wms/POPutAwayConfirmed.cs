namespace OmsApi;

public record POPutAwayItemDto(string Sku, string Condition, string Sloc, int Qty);
public record POPutAwayConfirmedRequest(string PurchaseOrderId, List<POPutAwayItemDto> Items, DateTime PutAwayAt,
    string? UpdatedBy = null);

public class POPutAwayConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(POPutAwayConfirmedRequest req)
    {
        var po = store.FindPO(req.PurchaseOrderId);
        if (po is null) return ApiResult.NotFound("purchase order", req.PurchaseOrderId);

        var now = DateTime.UtcNow;
        po.Status = "Closed";
        po.UpdatedAt = now;
        po.UpdatedBy = req.UpdatedBy;

        foreach (var item in req.Items)
        {
            var poLine = po.Lines.FirstOrDefault(l =>
                l.Sku.Equals(item.Sku, StringComparison.OrdinalIgnoreCase));
            if (poLine is not null)
            {
                poLine.Condition = item.Condition;
                poLine.Sloc = item.Sloc;
                poLine.PutAwayAt = req.PutAwayAt;
            }
        }

        var receipts = store.GetGoodsReceipts(req.PurchaseOrderId);
        foreach (var receipt in receipts)
        {
            receipt.Status = "PutAway";
            receipt.PutAwayAt = req.PutAwayAt;
            foreach (var item in req.Items)
            {
                var line = receipt.Lines.FirstOrDefault(l =>
                    l.Sku.Equals(item.Sku, StringComparison.OrdinalIgnoreCase));
                if (line is not null) { line.Condition = item.Condition; line.Sloc = item.Sloc; }
            }
        }

        return Results.Accepted(null, new { accepted = true, purchaseOrderId = req.PurchaseOrderId, newStatus = "Closed" });
    }
}
