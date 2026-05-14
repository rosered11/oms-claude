namespace OmsApi;

public record PaymentConfirmedRequest(string OrderId, string InvoiceNumber, string PaymentMethod,
    decimal PaidAmount, string Currency, DateTime PaidAt, string? GatewayRef = null,
    string? IdempotencyKey = null);

public class PaymentConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(PaymentConfirmedRequest req)
    {
        var o = store.FindOrder(req.OrderId);
        if (o is null) return ApiResult.NotFound("order", req.OrderId);
        if (o.Status != OrderStatus.Invoiced) return ApiResult.InvalidTransition(o.Status, OrderStatus.Paid);

        var now = DateTime.UtcNow;
        var paymentId = $"pay-{req.OrderId}";
        var txnId = $"txn-{Guid.NewGuid():N}"[..12];

        o.Status = OrderStatus.Paid;
        o.UpdatedAt = now;

        store.SetOrderPayment(req.OrderId, new OrderPaymentDto
        {
            PaymentId = paymentId,
            OrderId = req.OrderId,
            PaymentMethod = req.PaymentMethod,
            TotalAmount = req.PaidAmount,
            Currency = req.Currency,
            Status = "Paid",
            CreatedAt = now,
            UpdatedAt = now
        });

        store.AddPaymentTransaction(new PaymentTransactionDto
        {
            TransactionId = txnId,
            PaymentId = paymentId,
            Amount = req.PaidAmount,
            Currency = req.Currency,
            PaymentMethod = req.PaymentMethod,
            GatewayRef = req.GatewayRef,
            CreatedAt = req.PaidAt
        });

        store.AppendEvent(req.OrderId, ApiResult.DomainEvent("PaymentConfirmed", OrderStatus.Paid,
            $"Payment {req.PaidAmount} {req.Currency} confirmed via {req.PaymentMethod}."));

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, newStatus = OrderStatus.Paid });
    }
}
