namespace OmsApi;

public class GetStockLedgerHandler(InMemoryStore store)
{
    public IResult Handle(string sku)
    {
        var ledger = store.GetStockLedger(sku);
        if (ledger is null) return Results.NotFound(new { error_code = "STOCK_NOT_FOUND", message = $"No stock movements found for SKU {sku}" });
        return Results.Ok(ledger);
    }
}
