namespace OmsApi;

public class PrepaidInvoiceHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);
        if (o.Status != OrderStatus.PickConfirmed)
            return Results.UnprocessableEntity(new { error = "unprocessable", detail = $"Prepaid invoice requires PickConfirmed status. Order is {o.Status}." });

        var invoiceNumber = $"INV-PRE-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var now = DateTime.UtcNow;
        store.AppendEvent(id, ApiResult.OutboxEvent("WMS", "ABBInvoiceSentToWMS",
            $"SC → WMS: ABB/Tax Invoice {invoiceNumber} · {o.Amount} THB. Invoice issued before TMS dispatch."));
        store.AppendEvent(id, ApiResult.OutboxEvent("GW", "TaxInvoiceForwarded",
            $"SC → GW: ABB/Tax Invoice {invoiceNumber} forwarded to customer. Prepaid transaction complete."));
        return Results.Accepted(null, new { orderId = id, invoiceNumber, invoicedAt = now });
    }
}
