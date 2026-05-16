using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/outbox")]
public class OutboxController(
    GetDispatchLogsHandler getDispatchLogs,
    RetryDispatchHandler retryDispatch) : ControllerBase
{
    [HttpGet("dispatch-logs")]
    public IResult GetAll([FromQuery] string? orderId, [FromQuery] string? status)
        => getDispatchLogs.Handle(orderId, status);

    [HttpGet("dispatch-logs/{logId:long}")]
    public IResult GetOne(long logId) => getDispatchLogs.HandleOne(logId);

    [HttpPost("dispatch-logs/{logId:long}/retry")]
    public IResult Retry(long logId) => retryDispatch.Handle(logId);
}
