using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/inbound")]
public class InboundController(
    GetPurchaseOrdersHandler getPurchaseOrders,
    GetPurchaseOrderHandler getPurchaseOrder,
    CreatePurchaseOrderHandler createPurchaseOrder,
    GetTransferOrdersHandler getTransferOrders,
    GetTransferOrderHandler getTransferOrder,
    CreateTransferOrderHandler createTransferOrder) : ControllerBase
{
    [HttpGet("purchase-orders")]
    public IResult GetPurchaseOrders() => getPurchaseOrders.Handle();

    [HttpGet("purchase-orders/{id}")]
    public IResult GetPurchaseOrder(string id) => getPurchaseOrder.Handle(id);

    [HttpPost("purchase-orders")]
    public IResult CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest req) => createPurchaseOrder.Handle(req);

    [HttpGet("transfer-orders")]
    public IResult GetTransferOrders() => getTransferOrders.Handle();

    [HttpGet("transfer-orders/{id}")]
    public IResult GetTransferOrder(string id) => getTransferOrder.Handle(id);

    [HttpPost("transfer-orders")]
    public IResult CreateTransferOrder([FromBody] CreateTransferOrderRequest req) => createTransferOrder.Handle(req);
}
