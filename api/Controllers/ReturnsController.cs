using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/returns")]
public class ReturnsController(
    GetReturnsHandler getReturns,
    GetReturnHandler getReturn,
    GetReturnItemsHandler getReturnItems,
    CreateReturnHandler createReturn) : ControllerBase
{
    [HttpGet]
    public IResult GetAll() => getReturns.Handle();

    [HttpGet("{id}")]
    public IResult Get(string id) => getReturn.Handle(id);

    [HttpGet("{id}/items")]
    public IResult GetItems(string id) => getReturnItems.Handle(id);

    [HttpPost]
    public IResult Create([FromBody] CreateReturnRequest req) => createReturn.Handle(req);
}
