using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/stock")]
public class StockController(GetStockLedgerHandler getStockLedger) : ControllerBase
{
    [HttpGet("{sku}/ledger")]
    public IResult GetLedger(string sku) => getStockLedger.Handle(sku);
}
