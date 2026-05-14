namespace OmsApi;

public record PackageDto(string TrackingId, string VehicleType, decimal Weight, List<string> LineIds);
public record PackedRequest(string OrderId, List<PackageDto> Packages, DateTime PackedAt);

public class PackedHandler(InMemoryStore store)
{
    public IResult Handle(PackedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status != OrderStatus.PickConfirmed) return ApiResult.InvalidTransition(o.Status, OrderStatus.Packed);

        var now = DateTime.UtcNow;
        o.Packages = req.Packages.Select((p, i) => new OrderPackageDto
        {
            Id = $"PKG-{i + 1:D3}",
            TrackingId = p.TrackingId,
            VehicleType = p.VehicleType,
            Weight = p.Weight,
            Status = OrderStatus.Packed,
            LineIds = p.LineIds,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        o.Status = OrderStatus.Packed;
        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("WMS", "Packed", OrderStatus.Packed,
            $"{req.Packages.Count} package(s) packed at {req.PackedAt:o}."));
        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "WMS",
            EventType = "Packed",
            Detail = $"{req.Packages.Count} package(s) packed.",
            ReceivedAt = DateTime.UtcNow
        });
        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, newStatus = OrderStatus.Packed, packagesCreated = req.Packages.Count });
    }
}
