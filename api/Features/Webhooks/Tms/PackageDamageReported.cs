namespace OmsApi;

public record PackageDamageReportedRequest(string TrackingId, string Reason, string DriverNote, DateTime ReportedAt);

public class PackageDamageReportedHandler(InMemoryStore store)
{
    public IResult Handle(PackageDamageReportedRequest req)
    {
        var o = store.FindOrderByTracking(req.TrackingId);
        if (o is null) return ApiResult.NotFound("tracking", req.TrackingId);

        var preHold = o.Status;
        o.PreHoldStatus = preHold;
        o.Status = OrderStatus.OnHold;
        o.HoldReason = "PackageDamaged";
        o.UpdatedAt = DateTime.UtcNow;

        store.AppendEvent(o.Id, ApiResult.WebhookEvent("TMS", "PackageDamageReported", OrderStatus.OnHold,
            $"Driver reported damage on {req.TrackingId}: {req.DriverNote}"));
        return Results.Accepted(null, new
        {
            accepted = true,
            orderId = o.Id,
            newOrderStatus = OrderStatus.OnHold,
            holdReason = "PackageDamaged",
            preHoldStatus = preHold
        });
    }
}
