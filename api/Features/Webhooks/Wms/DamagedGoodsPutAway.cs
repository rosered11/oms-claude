namespace OmsApi;

public record DamagedItemDto(string Sku, string Condition, string Sloc, int Qty);
public record DamagedGoodsPutAwayRequest(string DamagedReceiptId, List<DamagedItemDto> Items, DateTime PutAwayAt);

public class DamagedGoodsPutAwayHandler(InMemoryStore store)
{
    public IResult Handle(DamagedGoodsPutAwayRequest req)
    {
        var receipt = store.FindDamagedReceipt(req.DamagedReceiptId);
        if (receipt is null) return ApiResult.NotFound("damaged receipt", req.DamagedReceiptId);

        receipt.Status = "PutAway";
        return Results.Accepted(null, new { accepted = true, damagedReceiptId = req.DamagedReceiptId, newStatus = "PutAway" });
    }
}
