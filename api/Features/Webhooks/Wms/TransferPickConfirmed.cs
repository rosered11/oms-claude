namespace OmsApi;

public record TransferLineDto(string Sku, decimal TransferredQty);
public record TransferPickConfirmedRequest(string TransferOrderId, List<TransferLineDto> Lines,
    DateTime ConfirmedAt, string? UpdatedBy = null);

public class TransferPickConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(TransferPickConfirmedRequest req)
    {
        var to = store.FindTO(req.TransferOrderId);
        if (to is null) return ApiResult.NotFound("transfer order", req.TransferOrderId);

        var now = DateTime.UtcNow;
        to.Status = "PickConfirmed";
        to.UpdatedAt = now;
        to.UpdatedBy = req.UpdatedBy;

        foreach (var line in req.Lines)
        {
            var toLine = to.Lines.FirstOrDefault(l =>
                l.Sku.Equals(line.Sku, StringComparison.OrdinalIgnoreCase));
            if (toLine is not null)
            {
                toLine.TransferredQty = (int)line.TransferredQty;
                toLine.ConfirmedAt = req.ConfirmedAt;
            }
        }

        store.AddTransferConfirmation(req.TransferOrderId, new TransferConfirmationDto
        {
            Type = "PickConfirmed",
            ConfirmedAt = req.ConfirmedAt,
            ConfirmedBy = req.UpdatedBy ?? "WMS",
            Tracking = to.Tracking
        });

        return Results.Accepted(null, new { accepted = true, transferOrderId = req.TransferOrderId, newStatus = "PickConfirmed" });
    }
}
