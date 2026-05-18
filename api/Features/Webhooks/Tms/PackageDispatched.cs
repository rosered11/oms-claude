namespace OmsApi;

public record PackageDispatchedRequest(string TrackingId, DateTime DispatchedAt);

public class PackageDispatchedHandler(InMemoryStore store, OutboxAdapterService adapterService)
{
    public IResult Handle(PackageDispatchedRequest req)
    {
        var o = store.FindOrderByTracking(req.TrackingId);
        if (o is null) return ApiResult.NotFound("tracking", req.TrackingId);

        var pkg = o.Packages.FirstOrDefault(p => p.TrackingId == req.TrackingId);
        if (pkg is not null) pkg.Status = OrderStatus.OutForDelivery;

        if (o.Packages.All(p => p.Status == OrderStatus.OutForDelivery))
        {
            o.Status = OrderStatus.OutForDelivery;
            store.AppendEvent(o.Id, ApiResult.WebhookEvent("TMS", "PackageDispatched", OrderStatus.OutForDelivery,
                $"Package {req.TrackingId} dispatched at {req.DispatchedAt:o}."));

            var payment = store.GetOrderPayment(o.Id);
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(
                GatewayUpdateStatusPayload.Build(o, payment));

            foreach (var evt in adapterService.Dispatch(o.Id, o.ChannelType, o.SubChannel, o.BusinessUnit,
                "OutForDeliveryEvent", payloadJson))
                store.AppendEvent(o.Id, evt);
        }

        store.AddWebhookLog(o.Id, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "TMS",
            EventType = "PackageDispatched",
            Detail = $"Package {req.TrackingId} dispatched.",
            ReceivedAt = DateTime.UtcNow
        });
        o.UpdatedAt = DateTime.UtcNow;
        return Results.Accepted(null, new { accepted = true, orderId = o.Id, newOrderStatus = OrderStatus.OutForDelivery, newPackageStatus = OrderStatus.OutForDelivery });
    }
}
