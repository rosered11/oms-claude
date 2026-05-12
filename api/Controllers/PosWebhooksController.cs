using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/webhooks/pos")]
public class PosWebhooksController(
    RecalcResultHandler recalcResult,
    RecalcCompletedHandler recalcCompleted,
    CollectionReadyHandler collectionReady,
    CollectedHandler collected,
    InvoicedHandler invoiced,
    PaymentConfirmedHandler paymentConfirmed) : ControllerBase
{
    [HttpPost("recalculation-result")]
    public IResult RecalcResult([FromBody] RecalcResultRequest req) => recalcResult.Handle(req);

    [HttpPost("pos-recalc-completed")]
    public IResult RecalcCompleted([FromBody] RecalcCompletedRequest req) => recalcCompleted.Handle(req);

    [HttpPost("pos-collection-ready")]
    public IResult CollectionReady([FromBody] CollectionReadyRequest req) => collectionReady.Handle(req);

    [HttpPost("collected")]
    public IResult Collected([FromBody] CollectedRequest req) => collected.Handle(req);

    [HttpPost("invoiced")]
    public IResult Invoiced([FromBody] InvoicedRequest req) => invoiced.Handle(req);

    [HttpPost("payment-confirmed")]
    public IResult PaymentConfirmed([FromBody] PaymentConfirmedRequest req) => paymentConfirmed.Handle(req);
}
