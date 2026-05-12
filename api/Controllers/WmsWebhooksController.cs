using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/webhooks/wms")]
public class WmsWebhooksController(
    BookingConfirmedHandler bookingConfirmed,
    PickStartedHandler pickStarted,
    PickConfirmedHandler pickConfirmed,
    PackedHandler packed,
    SubstitutionOfferedHandler substitutionOffered,
    PutAwayConfirmedHandler putAwayConfirmed,
    GoodsReceiptConfirmedHandler goodsReceiptConfirmed,
    POPutAwayConfirmedHandler poPutAwayConfirmed,
    TransferPickConfirmedHandler transferPickConfirmed,
    TransferReceivedHandler transferReceived,
    DamagedGoodsReceivedHandler damagedGoodsReceived,
    DamagedGoodsPutAwayHandler damagedGoodsPutAway) : ControllerBase
{
    [HttpPost("booking-confirmed")]
    public IResult BookingConfirmed([FromBody] BookingConfirmedRequest req) => bookingConfirmed.Handle(req);

    [HttpPost("pick-started")]
    public IResult PickStarted([FromBody] PickStartedRequest req) => pickStarted.Handle(req);

    [HttpPost("pick-confirmed")]
    public IResult PickConfirmed([FromBody] PickConfirmedRequest req) => pickConfirmed.Handle(req);

    [HttpPost("packed")]
    public IResult Packed([FromBody] PackedRequest req) => packed.Handle(req);

    [HttpPost("substitution-offered")]
    public IResult SubstitutionOffered([FromBody] SubstitutionOfferedRequest req) => substitutionOffered.Handle(req);

    [HttpPost("put-away-confirmed")]
    public IResult PutAwayConfirmed([FromBody] PutAwayConfirmedRequest req) => putAwayConfirmed.Handle(req);

    [HttpPost("goods-receipt-confirmed")]
    public IResult GoodsReceiptConfirmed([FromBody] GoodsReceiptConfirmedRequest req) => goodsReceiptConfirmed.Handle(req);

    [HttpPost("purchase-order-put-away-confirmed")]
    public IResult POPutAwayConfirmed([FromBody] POPutAwayConfirmedRequest req) => poPutAwayConfirmed.Handle(req);

    [HttpPost("transfer-pick-confirmed")]
    public IResult TransferPickConfirmed([FromBody] TransferPickConfirmedRequest req) => transferPickConfirmed.Handle(req);

    [HttpPost("transfer-received")]
    public IResult TransferReceived([FromBody] TransferReceivedRequest req) => transferReceived.Handle(req);

    [HttpPost("damaged-goods-received")]
    public IResult DamagedGoodsReceived([FromBody] DamagedGoodsReceivedRequest req) => damagedGoodsReceived.Handle(req);

    [HttpPost("damaged-goods-put-away")]
    public IResult DamagedGoodsPutAway([FromBody] DamagedGoodsPutAwayRequest req) => damagedGoodsPutAway.Handle(req);
}
