namespace OmsApi;

public record AbbTaxInvoiceReceivedRequest(
    string OrderId,
    string InvoiceNumber,
    decimal InvoiceAmount,
    string Currency,
    string? InvoiceLink,
    DateTime IssuedAt);

public class AbbTaxInvoiceReceivedHandler(InMemoryStore store, OutboxAdapterService adapterService)
{
    public IResult Handle(AbbTaxInvoiceReceivedRequest req)
    {
        var order = store.FindOrder(req.OrderId);
        if (order is null) return ApiResult.NotFound("order", req.OrderId);

        var existing = store.GetInvoice(req.OrderId);
        if (existing?.SourceStsRef == req.InvoiceNumber)
            return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, duplicate = true });

        var invoiceId = $"inv-{Guid.NewGuid():N}"[..10];
        var invoice = new InvoiceDto
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
        };
        store.SetInvoice(req.OrderId, invoice);

        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "STS",
            EventType = "ABBTaxInvoiceReceived",
            Detail = $"Invoice {req.InvoiceNumber} · {req.InvoiceAmount} {req.Currency} received from STS.",
            ReceivedAt = DateTime.UtcNow
        });
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("STS", "ABBTaxInvoiceReceived", order.Status,
            $"Invoice {req.InvoiceNumber} · {req.InvoiceAmount} {req.Currency} received from STS."));

        var invoicePayload = System.Text.Json.JsonSerializer.Serialize(
            TmsWmsTaxInvoicePayload.Build(order, invoice, order.Lines));

        if (order.PaymentFlow == "PRE_PAID")
        {
            foreach (var evt in adapterService.Dispatch(req.OrderId, order.ChannelType, order.SubChannel,
                order.BusinessUnit, "ABBTaxInvoiceSentToWMS", invoicePayload))
                store.AppendEvent(req.OrderId, evt);
        }
        else
        {
            foreach (var evt in adapterService.Dispatch(req.OrderId, order.ChannelType, order.SubChannel,
                order.BusinessUnit, "ABBTaxInvoiceSentToTMS", invoicePayload))
                store.AppendEvent(req.OrderId, evt);
        }

        var gatewayEvent = "ABBTaxInvoiceSentToGateway";
        foreach (var evt in adapterService.Dispatch(req.OrderId, order.ChannelType, order.SubChannel,
            order.BusinessUnit, gatewayEvent, invoicePayload))
            store.AppendEvent(req.OrderId, evt);

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId });
    }
}
