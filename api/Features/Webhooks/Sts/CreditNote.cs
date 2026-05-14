namespace OmsApi;

public record StsCreditNoteRequest(
    string OrderId,
    string CreditNoteNumber,
    string CreditNoteLink,
    decimal Amount,
    string Currency,
    DateTime IssuedAt);

public class StsCreditNoteHandler(InMemoryStore store)
{
    public IResult Handle(StsCreditNoteRequest req)
    {
        var order = store.FindOrder(req.OrderId);
        if (order is null) return ApiResult.NotFound("order", req.OrderId);

        var existing = store.GetCreditNote(req.OrderId);
        if (existing?.SourceStsRef == req.CreditNoteNumber)
            return Results.Conflict(new
            {
                error = "conflict",
                detail = $"Credit Note {req.CreditNoteNumber} already received for {req.OrderId}."
            });

        store.SetCreditNote(req.OrderId, new CreditNoteDto
        {
            CreditNoteId = $"CN-{Guid.NewGuid():N}"[..10],
            CreditNoteNumber = req.CreditNoteNumber,
            InvoiceId = store.GetInvoice(req.OrderId)?.InvoiceId ?? "",
            Amount = req.Amount,
            Currency = req.Currency,
            Reason = "Return",
            Status = "Issued",
            CreditNoteLink = req.CreditNoteLink,
            SourceStsRef = req.CreditNoteNumber,
            IssuedAt = req.IssuedAt
        });

        var forwardedTo = order.IsPrepaid ? new[] { "WMS" } : new[] { "TMS" };

        store.AppendEvent(req.OrderId, ApiResult.OutboxEvent(forwardedTo[0],
            order.IsPrepaid ? "CreditNoteSentToWMS" : "CreditNoteSentToTMS",
            $"Credit Note {req.CreditNoteNumber} forwarded to {forwardedTo[0]}."));

        return Results.Accepted(null, new
        {
            accepted = true,
            orderId = req.OrderId,
            creditNoteNumber = req.CreditNoteNumber,
            forwardedTo
        });
    }
}
