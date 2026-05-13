namespace OmsApi;

public record DamagedItemDto(string Sku, string Condition, string Sloc, decimal Quantity);
public record DamagedGoodsPutAwayRequest(string DamagedReceiptId, List<DamagedItemDto> Items, DateTime PutAwayAt,
    string? UpdatedBy = null);

public class DamagedGoodsPutAwayHandler(InMemoryStore store)
{
    public IResult Handle(DamagedGoodsPutAwayRequest req)
    {
        var receipt = store.FindDamagedReceipt(req.DamagedReceiptId);
        if (receipt is null) return ApiResult.NotFound("damaged receipt", req.DamagedReceiptId);

        var now = DateTime.UtcNow;
        receipt.Status = "PutAway";
        receipt.PutAwayAt = req.PutAwayAt;
        receipt.UpdatedBy = req.UpdatedBy;

        foreach (var item in req.Items)
        {
            var existing = receipt.Items.FirstOrDefault(i =>
                i.Sku.Equals(item.Sku, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                existing.Condition = item.Condition;
                existing.Sloc = item.Sloc;
                existing.Quantity = item.Quantity;
                existing.ConfirmedAt = req.PutAwayAt;
            }
            else
            {
                receipt.Items.Add(new DamagedGoodsItemDto
                {
                    ItemId = $"{req.DamagedReceiptId}-I{receipt.Items.Count + 1:D3}",
                    DamagedReceiptId = req.DamagedReceiptId,
                    Sku = item.Sku,
                    Condition = item.Condition,
                    Sloc = item.Sloc,
                    Quantity = item.Quantity,
                    ConfirmedAt = req.PutAwayAt
                });
            }
        }

        return Results.Accepted(null, new { accepted = true, damagedReceiptId = req.DamagedReceiptId, newStatus = "PutAway" });
    }
}
