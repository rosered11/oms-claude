namespace OmsApi;

public class GetDispatchLogsHandler(InMemoryStore store)
{
    public IResult Handle(string? orderId, string? status)
    {
        var logs = orderId is not null
            ? store.GetDispatchLogs(orderId)
            : status?.Equals("Failed", StringComparison.OrdinalIgnoreCase) == true
                ? store.GetFailedDispatchLogs()
                : store.GetDispatchLogs();

        return Results.Ok(new { data = logs.Select(ToDto) });
    }

    public IResult HandleOne(long logId)
    {
        var log = store.GetDispatchLog(logId);
        if (log is null) return ApiResult.NotFound("dispatch_log", logId.ToString());
        return Results.Ok(new { data = ToDto(log) });
    }

    private static OutboxDispatchLogDto ToDto(OutboxDispatchLog log) => new()
    {
        LogId = log.LogId,
        OrderId = log.OrderId,
        EndpointKey = log.EndpointKey,
        TriggerEvent = log.TriggerEvent,
        TargetSystem = log.TargetSystem,
        AuthType = log.AuthType,
        Status = log.Status,
        TokenUrl = log.TokenUrl,
        TokenRequestHeaders = log.TokenRequestHeaders,
        TokenRequestBody = log.TokenRequestBody,
        TokenResponsePayload = log.TokenResponsePayload,
        BaseUrl = log.BaseUrl,
        ApiRequestHeaders = log.ApiRequestHeaders,
        RequestPayload = log.RequestPayload,
        ResponsePayload = log.ResponsePayload,
        HttpStatusCode = log.HttpStatusCode,
        AttemptCount = log.AttemptCount,
        CreatedAt = log.CreatedAt,
        CompletedAt = log.CompletedAt,
        ErrorMessage = log.ErrorMessage
    };
}

public class RetryDispatchHandler(InMemoryStore store, OutboxAdapterService adapterService)
{
    public IResult Handle(long logId)
    {
        var log = store.GetDispatchLog(logId);
        if (log is null) return ApiResult.NotFound("dispatch_log", logId.ToString());

        if (log.Status != "Failed")
            return Results.Conflict(new
            {
                error_code = "INVALID_STATE",
                message = $"Dispatch log '{logId}' is not in Failed status (current: {log.Status}).",
                trace_id = Guid.NewGuid()
            });

        var evt = adapterService.Retry(logId);
        return Results.Accepted(null, new { retried = true, logId, detail = evt?.Detail });
    }
}
