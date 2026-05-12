namespace OmsApi;

public record PutAwayItemDto(string Sku, string Condition, string Sloc);
public record PutAwayConfirmedRequest(string ReturnId, List<PutAwayItemDto> Items, DateTime PutAwayAt);

public class PutAwayConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(PutAwayConfirmedRequest req)
    {
        var ret = store.FindReturn(req.ReturnId);
        if (ret is null) return ApiResult.NotFound("return", req.ReturnId);

        foreach (var item in req.Items)
        {
            var ri = ret.Items.FirstOrDefault(i => i.Sku.Equals(item.Sku, StringComparison.OrdinalIgnoreCase));
            if (ri is not null)
            {
                ri.Condition = item.Condition;
                ri.AssignedSloc = item.Sloc;
                ri.PutAwayStatus = "PutAway";
            }
        }

        ret.Status = "PutAway";
        ret.PutAwayAt = req.PutAwayAt;
        ret.UpdatedAt = DateTime.UtcNow;

        var creditNoteId = $"CN-RET-{ret.Id}";
        ret.CreditNoteId = creditNoteId;

        var refundAmount = ret.Items.Sum(i => i.Quantity * i.UnitPrice);
        var refundId = $"ref-{ret.Id}";

        store.SetRefund(ret.Id, new RefundDto
        {
            RefundId = refundId,
            RefundAmount = refundAmount,
            Currency = "THB",
            RefundMethod = ret.Items.FirstOrDefault()?.PaymentMethod ?? "Unknown",
            Status = "Processed",
            ReferenceNo = $"REF-TXN-{ret.Id}",
            ProcessedAt = req.PutAwayAt
        });

        store.SetCreditNote(ret.Id, new CreditNoteDto
        {
            CreditNoteId = creditNoteId,
            CreditNoteNumber = creditNoteId,
            InvoiceId = ret.InvoiceId ?? "",
            Amount = refundAmount,
            Currency = "THB",
            Reason = "Return",
            Status = "Issued"
        });

        return Results.Accepted(null, new
        {
            accepted = true,
            returnId = req.ReturnId,
            newReturnStatus = "PutAway",
            refundInitiated = true,
            creditNoteId
        });
    }
}
