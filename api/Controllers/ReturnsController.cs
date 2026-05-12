using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/returns")]
public class ReturnsController(
    GetReturnsHandler getReturns,
    GetReturnHandler getReturn,
    GetReturnItemsHandler getReturnItems,
    CreateReturnHandler createReturn,
    CancelReturnHandler cancelReturn,
    GetReturnRefundHandler getReturnRefund) : ControllerBase
{
    [HttpGet]
    public IResult GetAll() => getReturns.Handle();

    [HttpGet("{id}")]
    public IResult Get(string id) => getReturn.Handle(id);

    [HttpGet("{id}/items")]
    public IResult GetItems(string id) => getReturnItems.Handle(id);

    [HttpPost]
    public IResult Create([FromBody] CreateReturnRequest req) => createReturn.Handle(req);

    [HttpPatch("{id}/cancel")]
    public IResult Cancel(string id, [FromBody] CancelReturnRequest req) => cancelReturn.Handle(id, req);

    [HttpGet("{id}/refund")]
    public IResult GetRefund(string id) => getReturnRefund.Handle(id);
}
