namespace OmsApi;

public record InvoicedRequest(string OrderId, string InvoiceNumber, decimal TotalAmount,
    string Currency, DateTime InvoicedAt);

public class InvoicedHandler(InMemoryStore store)
{
    public IResult Handle(InvoicedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status != OrderStatus.Delivered) return ApiResult.InvalidTransition(o.Status, OrderStatus.Invoiced);

        o.Status = OrderStatus.Invoiced;
        o.UpdatedAt = DateTime.UtcNow;
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("POS", "Invoiced", OrderStatus.Invoiced,
            $"Invoice {req.InvoiceNumber} issued for {req.TotalAmount} {req.Currency}."));
        return Results.Ok(o);
    }
}
