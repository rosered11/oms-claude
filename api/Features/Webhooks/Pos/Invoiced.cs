namespace OmsApi;

public record InvoicedRequest(string OrderId, string InvoiceNumber, decimal TotalAmount,
    string Currency, DateTime InvoicedAt);

public class InvoicedHandler(InMemoryStore store)
{
    public IResult Handle(InvoicedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status is not (OrderStatus.Delivered or OrderStatus.Collected))
            return ApiResult.InvalidTransition(o.Status, OrderStatus.Invoiced);

        o.Status = OrderStatus.Invoiced;
        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("POS", "Invoiced", OrderStatus.Invoiced,
            $"Invoice {req.InvoiceNumber} issued for {req.TotalAmount} {req.Currency}."));
        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "POS",
            EventType = "Invoiced",
            Detail = $"Invoice {req.InvoiceNumber} — {req.TotalAmount} {req.Currency}.",
            ReceivedAt = DateTime.UtcNow
        });
        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, newStatus = OrderStatus.Invoiced, invoiceId = $"inv-{req.OrderId}" });
    }
}
