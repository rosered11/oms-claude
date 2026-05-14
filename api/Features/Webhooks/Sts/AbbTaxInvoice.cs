namespace OmsApi;

public record AbbTaxInvoiceRequest(
    string OrderId,
    string InvoiceNumber,
    string InvoiceLink,
    decimal Amount,
    string Currency,
    DateTime IssuedAt);

public class AbbTaxInvoiceHandler(InMemoryStore store)
{
    public IResult Handle(AbbTaxInvoiceRequest req)
    {
        var order = store.FindOrder(req.OrderId);
        if (order is null) return ApiResult.NotFound("order", req.OrderId);

        var existing = store.GetInvoice(req.OrderId);
        if (existing?.SourceStsRef == req.InvoiceNumber)
            return Results.Conflict(new
            {
                error = "conflict",
                detail = $"ABB/Tax Invoice {req.InvoiceNumber} already received for {req.OrderId}."
            });

        var invoiceId = $"inv-{Guid.NewGuid():N}"[..10];
        store.SetInvoice(req.OrderId, new InvoiceDto
        {
            InvoiceId = invoiceId,
            OrderId = req.OrderId,
            InvoiceNumber = req.InvoiceNumber,
            InvoiceType = "ABBTax",
            TotalAmount = req.Amount,
            Currency = req.Currency,
            Status = "Issued",
            InvoiceLink = req.InvoiceLink,
            SourceStsRef = req.InvoiceNumber,
            IssuedAt = req.IssuedAt,
            GeneratedAt = DateTime.UtcNow
        });

        var forwardedTo = order.IsPrepaid
            ? new[] { "WMS", "Gateway" }
            : new[] { "TMS", "Gateway" };

        store.AppendEvent(req.OrderId, ApiResult.OutboxEvent(string.Join("+", forwardedTo),
            order.IsPrepaid ? "ABBInvoiceSentToWMS" : "ABBTaxInvoiceSentToTMS",
            $"Invoice {req.InvoiceNumber} forwarded to {string.Join(", ", forwardedTo)}."));

        return Results.Accepted(null, new
        {
            accepted = true,
            orderId = req.OrderId,
            invoiceNumber = req.InvoiceNumber,
            invoiceId,
            forwardedTo
        });
    }
}
