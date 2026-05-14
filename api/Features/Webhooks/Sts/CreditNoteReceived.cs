namespace OmsApi;

public record CreditNoteReceivedRequest(
    string OrderId,
    string CreditNoteNumber,
    decimal CreditAmount,
    string Currency,
    string? CreditNoteLink,
    string? Reason,
    DateTime IssuedAt);

public class CreditNoteReceivedHandler(InMemoryStore store)
{
    public IResult Handle(CreditNoteReceivedRequest req)
    {
        var order = store.FindOrder(req.OrderId);
        if (order is null) return ApiResult.NotFound("order", req.OrderId);

        var existing = store.GetCreditNote(req.OrderId);
        if (existing?.SourceStsRef == req.CreditNoteNumber)
            return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, duplicate = true });

        store.SetCreditNote(req.OrderId, new CreditNoteDto
        {
            CreditNoteId = $"CN-{Guid.NewGuid():N}"[..10],
            CreditNoteNumber = req.CreditNoteNumber,
            InvoiceId = store.GetInvoice(req.OrderId)?.InvoiceId ?? "",
            Amount = req.CreditAmount,
            Currency = req.Currency,
            Reason = req.Reason ?? "PriceAdjustment",
            Status = "Issued",
            CreditNoteLink = req.CreditNoteLink,
            SourceStsRef = req.CreditNoteNumber,
            IssuedAt = req.IssuedAt
        });

        if (order.IsPrepaid)
        {
            store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("WMS", "CreditNoteSentToWMS",
                $"Credit Note {req.CreditNoteNumber} dispatched to WMS (prepaid)."));
        }
        else
        {
            store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("TMS", "CreditNoteSentToTMS",
                $"Credit Note {req.CreditNoteNumber} dispatched to TMS (POD)."));
        }

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId });
    }
}
