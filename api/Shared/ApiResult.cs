namespace OmsApi;

public static class ApiResult
{
    public static IResult NotFound(string resource, string id) =>
        Results.NotFound(new { error_code = "NOT_FOUND", message = $"{resource} '{id}' not found.", trace_id = Guid.NewGuid() });

    public static IResult InvalidTransition(string current, string target) =>
        Results.UnprocessableEntity(new { error_code = "INVALID_TRANSITION",
            message = $"Cannot transition from '{current}' to '{target}'.", trace_id = Guid.NewGuid() });

    public static TimelineEventDto DomainEvent(string evt, string outStatus, string detail) => new()
    {
        Time = DateTime.UtcNow.ToString("o"),
        Phase = "Domain",
        Type = "Domain",
        System = "OMS",
        Event = evt,
        OutStatus = outStatus,
        Detail = detail,
        OccurredAt = DateTime.UtcNow
    };

    public static TimelineEventDto WebhookEvent(string system, string evt, string outStatus, string detail) => new()
    {
        Time = DateTime.UtcNow.ToString("o"),
        Phase = "Webhook",
        Type = "Inbound",
        System = system,
        Event = evt,
        OutStatus = outStatus,
        Detail = detail,
        OccurredAt = DateTime.UtcNow
    };

    public static TimelineEventDto OutboxEvent(string targetSystem, string evt, string detail) => new()
    {
        Time = DateTime.UtcNow.ToString("o"),
        Phase = "outbound",
        Type = "outbox",
        System = targetSystem,
        Event = evt,
        OutStatus = "Published",
        Detail = detail,
        OccurredAt = DateTime.UtcNow
    };
}
