using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/webhooks/tms")]
public class TmsWebhooksController(
    PackageDispatchedHandler packageDispatched,
    PackageDeliveredHandler packageDelivered,
    PackageDamageReportedHandler packageDamageReported,
    TmsRecalcRequestedHandler tmsRecalcRequested) : ControllerBase
{
    [HttpPost("package-dispatched")]
    public IResult PackageDispatched([FromBody] PackageDispatchedRequest req) => packageDispatched.Handle(req);

    [HttpPost("package-delivered")]
    public IResult PackageDelivered([FromBody] PackageDeliveredRequest req) => packageDelivered.Handle(req);

    [HttpPost("package-damage-reported")]
    public IResult PackageDamageReported([FromBody] PackageDamageReportedRequest req) => packageDamageReported.Handle(req);

    [HttpPost("recalculation-requested")]
    public IResult RecalcRequested([FromBody] TmsRecalcRequestedRequest req) => tmsRecalcRequested.Handle(req);
}
