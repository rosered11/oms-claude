namespace OmsApi;

public record CreditNoteReceivedRequest(
    string OrderId,
    string CreditNoteNumber,
    decimal Amount,
    string Currency,
    string? CreditNoteLink,
    string? Reason,
    DateTime IssuedAt);

public class CreditNoteReceivedHandler(InMemoryStore store, OutboxAdapterService adapterService)
{
    public IResult Handle(CreditNoteReceivedRequest req)
    {
        var order = store.FindOrder(req.OrderId);
        if (order is null) return ApiResult.NotFound("order", req.OrderId);

        var existing = store.GetCreditNote(req.OrderId);
        if (existing?.SourceStsRef == req.CreditNoteNumber)
            return Results.Accepted(null, new { accepted = true, orderId = req.OrderId, duplicate = true });

        var invoice = store.GetInvoice(req.OrderId);
        var creditNote = new CreditNoteDto
        {
            CreditNoteId = $"CN-{Guid.NewGuid():N}"[..10],
            CreditNoteNumber = req.CreditNoteNumber,
            InvoiceId = invoice?.InvoiceId ?? "",
            Amount = req.Amount,
            Currency = req.Currency,
            Reason = req.Reason ?? "PriceAdjustment",
            Status = "Issued",
            CreditNoteLink = req.CreditNoteLink,
            SourceStsRef = req.CreditNoteNumber,
            IssuedAt = req.IssuedAt
        };
        store.SetCreditNote(req.OrderId, creditNote);

        store.AddWebhookLog(req.OrderId, new WebhookLogDto
        {
            WebhookLogId = $"whl-{Guid.NewGuid():N}"[..8],
            SourceSystem = "STS",
            EventType = "CreditNoteReceived",
            Detail = $"Credit Note {req.CreditNoteNumber} · {req.Amount} {req.Currency} received from STS.",
            ReceivedAt = DateTime.UtcNow
        });
        store.AppendEvent(req.OrderId, ApiResult.WebhookEvent("STS", "CreditNoteReceived", order.Status,
            $"Credit Note {req.CreditNoteNumber} · {req.Amount} {req.Currency} received from STS."));

        var ret = store.Returns.FirstOrDefault(r => r.OrderId == req.OrderId);
        var returnItems = ret?.Items ?? [];

        var cnPayload = System.Text.Json.JsonSerializer.Serialize(
            TmsWmsCreditNotePayload.Build(order, creditNote, invoice, returnItems));

        if (order.IsPrepaid)
        {
            foreach (var evt in adapterService.Dispatch(req.OrderId, order.ChannelType, order.SubChannel,
                order.BusinessUnit, "CreditNoteSentToWMS", cnPayload))
                store.AppendEvent(req.OrderId, evt);
        }
        else
        {
            foreach (var evt in adapterService.Dispatch(req.OrderId, order.ChannelType, order.SubChannel,
                order.BusinessUnit, "CreditNoteSentToTMS", cnPayload))
                store.AppendEvent(req.OrderId, evt);
        }

        foreach (var evt in adapterService.Dispatch(req.OrderId, order.ChannelType, order.SubChannel,
            order.BusinessUnit, "CreditNoteSentToGW", cnPayload))
            store.AppendEvent(req.OrderId, evt);

        return Results.Accepted(null, new { accepted = true, orderId = req.OrderId });
    }
}
