namespace OmsApi;

public record PackageDeliveredRequest(string TrackingId, DateTime DeliveredAt, string RecipientName);

public class PackageDeliveredHandler(InMemoryStore store)
{
    public IResult Handle(PackageDeliveredRequest req)
    {
        var o = store.FindOrderByTracking(req.TrackingId);
        if (o is null) return ApiResult.NotFound("tracking", req.TrackingId);

        var pkg = o.Packages.FirstOrDefault(p => p.TrackingId == req.TrackingId);
        if (pkg is not null) pkg.Status = OrderStatus.Delivered;

        if (o.Packages.All(p => p.Status == OrderStatus.Delivered))
        {
            o.Status = OrderStatus.Delivered;
            store.AppendEvent(o.Id, ApiResult.WebhookEvent("TMS", "PackageDelivered", OrderStatus.Delivered,
                $"Delivered to {req.RecipientName} at {req.DeliveredAt:o}."));
        }

        o.UpdatedAt = DateTime.UtcNow;
        return Results.Ok(o);
    }
}
