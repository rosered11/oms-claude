namespace OmsApi;

public class GetOrderPaymentHandler(InMemoryStore store)
{
    public IResult Handle(string id)
    {
        var order = store.FindOrder(id);
        if (order is null) return ApiResult.NotFound("order", id);

        var payment = store.GetOrderPayment(id);
        if (payment is null) return Results.NotFound(new
        {
            error_code = "NOT_FOUND",
            message = $"No payment record found for order '{id}'.",
            trace_id = Guid.NewGuid()
        });

        var transactions = store.GetPaymentTransactions(payment.PaymentId);
        return Results.Ok(new { payment, transactions });
    }
}
