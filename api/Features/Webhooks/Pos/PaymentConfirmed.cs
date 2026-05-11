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
        return Results.Ok(o);
    }
}
