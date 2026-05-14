namespace OmsApi;

public record InvoicedRequest(string OrderId, string InvoiceNumber, decimal TotalAmount,
    string Currency, string InvoiceType, DateTime InvoicedAt, string? IdempotencyKey = null);

public class InvoicedHandler(InMemoryStore store)
{
    public IResult Handle(InvoicedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status is not (OrderStatus.Delivered or OrderStatus.Collected))
            return ApiResult.InvalidTransition(o.Status, OrderStatus.Invoiced);

        var now = DateTime.UtcNow;
        var invoiceId = $"inv-{req.OrderId}-{req.InvoiceNumber}";

        o.Status = OrderStatus.Invoiced;
        o.UpdatedAt = now;

        store.SetInvoice(req.OrderId, new InvoiceDto
        {
            InvoiceId = invoiceId,
            OrderId = req.OrderId,
            InvoiceNumber = req.InvoiceNumber,
            InvoiceType = req.InvoiceType,
            TotalAmount = req.TotalAmount,
            Currency = req.Currency,
            Status = "Issued",
            GeneratedAt = now,
            IssuedAt = req.InvoicedAt
        });

        store.AppendEvent(req.OrderId, ApiResult.DomainEvent("Invoiced", OrderStatus.Invoiced,
            $"Invoice {req.InvoiceNumber} issued for {req.TotalAmount} {req.Currency}."));

        return Results.Accepted(null, new
        {
            accepted = true,
            orderId = req.OrderId,
            newStatus = OrderStatus.Invoiced,
            invoiceId
        });
    }
}
