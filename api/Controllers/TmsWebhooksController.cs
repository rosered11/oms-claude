using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/webhooks/tms")]
public class TmsWebhooksController(
    PackageDispatchedHandler packageDispatched,
    PackageDeliveredHandler packageDelivered) : ControllerBase
{
    [HttpPost("package-dispatched")]
    public IResult PackageDispatched([FromBody] PackageDispatchedRequest req) => packageDispatched.Handle(req);

    [HttpPost("package-delivered")]
    public IResult PackageDelivered([FromBody] PackageDeliveredRequest req) => packageDelivered.Handle(req);
}
