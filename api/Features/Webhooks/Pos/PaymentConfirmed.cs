namespace OmsApi;

public record PaymentConfirmedRequest(string OrderId, string InvoiceNumber, string PaymentMethod,
    decimal PaidAmount, string Currency, DateTime PaidAt);

public class PaymentConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(PaymentConfirmedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status != OrderStatus.Invoiced) return ApiResult.InvalidTransition(o.Status, OrderStatus.Paid);

        o.Status = OrderStatus.Paid;
        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("POS", "PaymentConfirmed", OrderStatus.Paid,
            $"Payment {req.PaidAmount} {req.Currency} confirmed via {req.PaymentMethod}."));
        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "POS",
            EventType = "PaymentConfirmed",
            Detail = $"Paid {req.PaidAmount} {req.Currency} via {req.PaymentMethod}.",
            ReceivedAt = DateTime.UtcNow
        });
        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, newStatus = OrderStatus.Paid });
    }
}
