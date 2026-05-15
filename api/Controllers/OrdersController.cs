using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/orders")]
public class OrdersController(
    GetOrdersHandler getOrders,
    GetOrderHandler getOrder,
    GetOrderLinesHandler getOrderLines,
    GetOrderPackagesHandler getOrderPackages,
    GetOrderWebhooksHandler getOrderWebhooks,
    GetOrderSubstitutionsHandler getOrderSubstitutions,
    GetOrderCreditNoteHandler getOrderCreditNote,
    ApproveSubstitutionHandler approveSubstitution,
    RejectSubstitutionHandler rejectSubstitution,
    GetOrderTimelineHandler getOrderTimeline,
    CreateOrderHandler createOrder,
    HoldOrderHandler holdOrder,
    ReleaseHoldHandler releaseHold,
    CancelOrderHandler cancelOrder,
    TriggerRecalculateHandler triggerRecalculate,
    GetDeliverySlotHandler getDeliverySlot,
    UpdateDeliverySlotHandler updateDeliverySlot,
    PrepaidInvoiceHandler prepaidInvoice,
    PartialPickHandler partialPick) : ControllerBase
{
    [HttpGet]
    public IResult GetAll(
        [FromQuery] string? status, [FromQuery] string? channelType,
        [FromQuery] string? fulfillmentType, [FromQuery] string? storeId,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        => getOrders.Handle(status, channelType, fulfillmentType, storeId, search, page, limit);

    [HttpGet("{id}")]
    public IResult Get(string id) => getOrder.Handle(id);

    [HttpGet("{id}/lines")]
    public IResult GetLines(string id) => getOrderLines.Handle(id);

    [HttpGet("{id}/packages")]
    public IResult GetPackages(string id) => getOrderPackages.Handle(id);

    [HttpGet("{id}/webhooks")]
    public IResult GetWebhooks(string id) => getOrderWebhooks.Handle(id);

    [HttpGet("{id}/substitutions")]
    public IResult GetSubstitutions(string id) => getOrderSubstitutions.Handle(id);

    [HttpGet("{id}/credit-note")]
    public IResult GetCreditNote(string id) => getOrderCreditNote.Handle(id);

    [HttpPost("{id}/substitutions/{subId}/approve")]
    public IResult ApproveSubstitution(string id, string subId) => approveSubstitution.Handle(id, subId);

    [HttpPost("{id}/substitutions/{subId}/reject")]
    public IResult RejectSubstitution(string id, string subId) => rejectSubstitution.Handle(id, subId);

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

    [HttpPost("{id}/recalculate")]
    public IResult Recalculate(string id) => triggerRecalculate.Handle(id);

    [HttpGet("{id}/delivery-slot")]
    public IResult GetDeliverySlot(string id) => getDeliverySlot.Handle(id);

    [HttpPatch("{id}/delivery-slot")]
    public IResult UpdateDeliverySlot(string id, [FromBody] UpdateDeliverySlotRequest req) => updateDeliverySlot.Handle(id, req);

    [HttpPost("{id}/invoice/prepaid")]
    public IResult PrepaidInvoice(string id) => prepaidInvoice.Handle(id);

    [HttpPatch("{id}/partial-pick")]
    public IResult PartialPick(string id, [FromBody] PartialPickRequest req) => partialPick.Handle(id, req);
}
