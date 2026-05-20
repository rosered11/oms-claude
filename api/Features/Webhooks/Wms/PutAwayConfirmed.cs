namespace OmsApi;

public record PutAwayItemDto(string Sku, string Condition, string Sloc, decimal Quantity = 0,
    string PerformedBy = "WMS");
public record PutAwayConfirmedRequest(string ReturnId, List<PutAwayItemDto> Items, DateTime PutAwayAt);

public class PutAwayConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(PutAwayConfirmedRequest req)
    {
        var ret = store.FindReturn(req.ReturnId);
        if (ret is null) return ApiResult.NotFound("return", req.ReturnId);

        var now = DateTime.UtcNow;

        foreach (var item in req.Items)
        {
            var ri = ret.Items.FirstOrDefault(i => i.Sku.Equals(item.Sku, StringComparison.OrdinalIgnoreCase));
            if (ri is not null)
            {
                ri.Condition = item.Condition;
                ri.AssignedSloc = item.Sloc;
                ri.PutAwayStatus = "PutAway";
                ri.PutAwayAt = req.PutAwayAt;
                ri.UpdatedAt = now;

                store.AddReturnPutAwayLog(new ReturnPutAwayLogDto
                {
                    LogId = $"LOG-{Guid.NewGuid():N}"[..12],
                    ReturnId = req.ReturnId,
                    ReturnItemId = ri.Id,
                    Sku = item.Sku,
                    AssignedSloc = item.Sloc,
                    Condition = item.Condition,
                    Quantity = item.Quantity > 0 ? item.Quantity : ri.Quantity,
                    PerformedBy = item.PerformedBy,
                    PerformedAt = req.PutAwayAt
                });
            }
        }

        ret.Status = "PutAway";
        ret.PutAwayAt = req.PutAwayAt;
        ret.UpdatedAt = now;

        var order = store.FindOrder(ret.OrderId);
        if (order is not null)
        {
            var orderLineIds = order.Lines.Select(l => l.Id).ToHashSet();
            var returnedLineIds = ret.Items.Select(i => i.OrderLineId).ToHashSet();
            var isFullReturn = orderLineIds.All(id => returnedLineIds.Contains(id));

            if (isFullReturn)
            {
                order.Status = OrderStatus.Returned;
                order.UpdatedAt = now;
                store.AppendEvent(order.Id, ApiResult.DomainEvent("OrderReturned", OrderStatus.Returned,
                    $"All return items put away for return {req.ReturnId}. Order status → Returned."));
            }
        }

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
            ProcessedAt = req.PutAwayAt,
            CreatedAt = now
        });

        store.SetCreditNote(ret.Id, new CreditNoteDto
        {
            CreditNoteId = creditNoteId,
            CreditNoteNumber = creditNoteId,
            InvoiceId = ret.InvoiceId ?? "",
            Amount = refundAmount,
            Currency = "THB",
            Reason = "Return",
            Status = "Issued",
            IssuedAt = now
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
