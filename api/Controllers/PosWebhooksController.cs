using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/webhooks/pos")]
public class PosWebhooksController(
    RecalcResultHandler recalcResult,
    InvoicedHandler invoiced,
    PaymentConfirmedHandler paymentConfirmed) : ControllerBase
{
    [HttpPost("recalculation-result")]
    public IResult RecalcResult([FromBody] RecalcResultRequest req) => recalcResult.Handle(req);

    [HttpPost("invoiced")]
    public IResult Invoiced([FromBody] InvoicedRequest req) => invoiced.Handle(req);

    [HttpPost("payment-confirmed")]
    public IResult PaymentConfirmed([FromBody] PaymentConfirmedRequest req) => paymentConfirmed.Handle(req);
}
