using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/inbound")]
public class InboundController(
    GetPurchaseOrdersHandler getPurchaseOrders,
    GetPurchaseOrderHandler getPurchaseOrder,
    CreatePurchaseOrderHandler createPurchaseOrder,
    GetPOGoodsReceiptsHandler getPOGoodsReceipts,
    GetTransferOrdersHandler getTransferOrders,
    GetTransferOrderHandler getTransferOrder,
    CreateTransferOrderHandler createTransferOrder,
    GetTransferConfirmationsHandler getTransferConfirmations) : ControllerBase
{
    [HttpGet("purchase-orders")]
    public IResult GetPurchaseOrders() => getPurchaseOrders.Handle();

    [HttpGet("purchase-orders/{id}")]
    public IResult GetPurchaseOrder(string id) => getPurchaseOrder.Handle(id);

    [HttpPost("purchase-orders")]
    public IResult CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest req) => createPurchaseOrder.Handle(req);

    [HttpGet("purchase-orders/{id}/goods-receipts")]
    public IResult GetGoodsReceipts(string id) => getPOGoodsReceipts.Handle(id);

    [HttpGet("transfer-orders")]
    public IResult GetTransferOrders() => getTransferOrders.Handle();

    [HttpGet("transfer-orders/{id}")]
    public IResult GetTransferOrder(string id) => getTransferOrder.Handle(id);

    [HttpPost("transfer-orders")]
    public IResult CreateTransferOrder([FromBody] CreateTransferOrderRequest req) => createTransferOrder.Handle(req);

    [HttpGet("transfer-orders/{id}/confirmations")]
    public IResult GetTransferConfirmations(string id) => getTransferConfirmations.Handle(id);
}
