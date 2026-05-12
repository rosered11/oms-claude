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
        store.AppendEvent(id, ApiResult.DomainEvent("PrepaidInvoiceSent", o.Status, $"ABB/Tax Invoice {invoiceNumber} sent to WMS before TMS dispatch."));
        return Results.Accepted(null, new { orderId = id, invoiceNumber, invoicedAt = now });
    }
}
