using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/orders")]
public class OrdersController(
    GetOrdersHandler getOrders,
    GetOrderHandler getOrder,
    GetOrderLinesHandler getOrderLines,
    GetOrderPackagesHandler getOrderPackages,
    GetOrderTimelineHandler getOrderTimeline,
    CreateOrderHandler createOrder,
    HoldOrderHandler holdOrder,
    ReleaseHoldHandler releaseHold,
    CancelOrderHandler cancelOrder) : ControllerBase
{
    [HttpGet]
    public IResult GetAll(
        [FromQuery] string? status, [FromQuery] string? channelType,
        [FromQuery] string? fulfillmentType, [FromQuery] string? storeId,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => getOrders.Handle(status, channelType, fulfillmentType, storeId, search, page, pageSize);

    [HttpGet("{id}")]
    public IResult Get(string id) => getOrder.Handle(id);

    [HttpGet("{id}/lines")]
    public IResult GetLines(string id) => getOrderLines.Handle(id);

    [HttpGet("{id}/packages")]
    public IResult GetPackages(string id) => getOrderPackages.Handle(id);

    [HttpGet("{id}/timeline")]
    public IResult GetTimeline(string id) => getOrderTimeline.Handle(id);

    [HttpPost]
    public IResult Create([FromBody] CreateOrderRequest req) => createOrder.Handle(req);

    [HttpPatch("{id}/hold")]
    public IResult Hold(string id, [FromBody] HoldOrderRequest req) => holdOrder.Handle(id, req);

    [HttpPatch("{id}/release-hold")]
    public IResult ReleaseHold(string id, [FromBody] ReleaseHoldRequest req) => releaseHold.Handle(id, req);

    [HttpPatch("{id}/cancel")]
    public IResult Cancel(string id, [FromBody] CancelOrderRequest req) => cancelOrder.Handle(id, req);
}
