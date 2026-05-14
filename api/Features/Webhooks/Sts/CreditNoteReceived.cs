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

        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "STS",
            EventType = "CreditNoteReceived",
            Detail = $"Credit Note {req.CreditNoteNumber} · {req.CreditAmount} {req.Currency} received from STS.",
            ReceivedAt = DateTime.UtcNow
        });
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("STS", "CreditNoteReceived", order.Status,
            $"Credit Note {req.CreditNoteNumber} · {req.CreditAmount} {req.Currency} received from STS."));

        if (order.IsPrepaid)
        {
            store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("WMS", "CreditNoteSentToWMS",
                $"Credit Note {req.CreditNoteNumber} dispatched to WMS (prepaid)."));
            store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("Gateway", "CreditNoteSentToGW",
                $"Credit Note {req.CreditNoteNumber} dispatched to Gateway (prepaid)."));
        }
        else
        {
            store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("TMS", "CreditNoteSentToTMS",
                $"Credit Note {req.CreditNoteNumber} dispatched to TMS (POD)."));
            store.AppendEvent(req.OrderId, ApiResult.OutboxEvent("Gateway", "CreditNoteSentToGW",
                $"Credit Note {req.CreditNoteNumber} dispatched to Gateway (POD)."));
        }

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId });
    }
}
