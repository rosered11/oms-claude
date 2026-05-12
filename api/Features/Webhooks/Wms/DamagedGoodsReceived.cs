namespace OmsApi;

public record DamagedGoodsReceivedRequest(string OrderId, string TrackingId, DateTime ReceivedAt);

public class DamagedGoodsReceivedHandler(InMemoryStore store)
{
    public IResult Handle(DamagedGoodsReceivedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);

        var receiptId = store.NextDamagedReceiptId();
        store.AddDamagedReceipt(new DamagedReceiptDto
        {
            DamagedReceiptId = receiptId,
            OrderId = req.OrderId,
            TrackingId = req.TrackingId,
            ReceivedAt = req.ReceivedAt
        });

        o.PreHoldStatus = o.Status;
        o.Status = OrderStatus.OnHold;
        o.HoldReason = "PackageDamaged";
        o.UpdatedAt = DateTime.UtcNow;

        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("WMS", "DamagedGoodsReceived", OrderStatus.OnHold,
            $"Damaged package {req.TrackingId} checked in at warehouse dock."));

        return Results.Accepted(null, new
        {
            accepted = true,
            orderId = req.OrderId,
            damagedReceiptId = receiptId,
            newOrderStatus = OrderStatus.OnHold,
            holdReason = "PackageDamaged"
        });
    }
}
