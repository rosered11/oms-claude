namespace OmsApi;

public record DamagedGoodsItemRequest(string Sku, string Condition, string? Sloc, decimal Quantity);
public record DamagedGoodsReceivedRequest(string OrderId, string TrackingId, DateTime ReceivedAt,
    List<DamagedGoodsItemRequest>? Items = null);

public class DamagedGoodsReceivedHandler(InMemoryStore store)
{
    public IResult Handle(DamagedGoodsReceivedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);

        var receiptId = store.NextDamagedReceiptId();
        var receipt = new DamagedReceiptDto
        {
            DamagedReceiptId = receiptId,
            OrderId = req.OrderId,
            TrackingId = req.TrackingId,
            Status = "Received",
            ReceivedAt = req.ReceivedAt,
            Items = (req.Items ?? []).Select((item, i) => new DamagedGoodsItemDto
            {
                ItemId = $"{receiptId}-I{i + 1:D3}",
                DamagedReceiptId = receiptId,
                Sku = item.Sku,
                Condition = item.Condition,
                Sloc = item.Sloc,
                Quantity = item.Quantity
            }).ToList()
        };
        store.AddDamagedReceipt(receipt);

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
