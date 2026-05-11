namespace OmsApi;

public class GetStockLedgerHandler(InMemoryStore store)
{
    public IResult Handle(string sku) => Results.Ok(store.GetStockLedger(sku));
}
