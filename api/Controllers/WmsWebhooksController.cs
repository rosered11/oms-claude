using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/webhooks/wms")]
public class WmsWebhooksController(
    BookingConfirmedHandler bookingConfirmed,
    PickStartedHandler pickStarted,
    PickConfirmedHandler pickConfirmed,
    PackedHandler packed) : ControllerBase
{
    [HttpPost("booking-confirmed")]
    public IResult BookingConfirmed([FromBody] BookingConfirmedRequest req) => bookingConfirmed.Handle(req);

    [HttpPost("pick-started")]
    public IResult PickStarted([FromBody] PickStartedRequest req) => pickStarted.Handle(req);

    [HttpPost("pick-confirmed")]
    public IResult PickConfirmed([FromBody] PickConfirmedRequest req) => pickConfirmed.Handle(req);

    [HttpPost("packed")]
    public IResult Packed([FromBody] PackedRequest req) => packed.Handle(req);
}
