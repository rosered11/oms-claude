namespace OmsApi;

public record AbbTaxInvoiceReceivedRequest(
    string OrderId,
    string InvoiceNumber,
    decimal InvoiceAmount,
    string Currency,
    string? InvoiceLink,
    DateTime IssuedAt);

public class AbbTaxInvoiceReceivedHandler(InMemoryStore store)
{
    public IResult Handle(AbbTaxInvoiceReceivedRequest req)
    {
        var order = store.FindOrder(req.OrderId);
        if (order is null) return ApiResult.NotFound("order", req.OrderId);

        var existing = store.GetInvoice(req.OrderId);
        if (existing?.SourceStsRef == req.InvoiceNumber)
            return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, duplicate = true });

        var invoiceId = $"inv-{Guid.NewGuid():N}"[..10];
        store.SetInvoice(req.OrderId, new InvoiceDto
        {
            InvoiceId = invoiceId,
            OrderId = req.OrderId,
            InvoiceNumber = req.InvoiceNumber,
            InvoiceType = "ABBTax",
            TotalAmount = req.InvoiceAmount,
            Currency = req.Currency,
            Status = "Issued",
            InvoiceLink = req.InvoiceLink,
            SourceStsRef = req.InvoiceNumber,
            IssuedAt = req.IssuedAt,
            GeneratedAt = DateTime.UtcNow
        });

        if (order.IsPrepaid)
        {
            store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("WMS", "ABBInvoiceSentToWMS",
                $"Invoice {req.InvoiceNumber} dispatched to WMS (prepaid)."));
        }
        else
        {
            store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("TMS", "ABBTaxInvoiceSentToTMS",
                $"Invoice {req.InvoiceNumber} dispatched to TMS (POD)."));
            store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("Gateway", "ABBTaxInvoiceSentToGW",
                $"Invoice {req.InvoiceNumber} dispatched to Gateway (POD)."));
        }

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId });
    }
}
