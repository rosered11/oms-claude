namespace OmsApi;

public record TransferLineDto(string Sku, decimal TransferredQty);
public record TransferPickConfirmedRequest(string TransferOrderId, List<TransferLineDto> Lines, DateTime ConfirmedAt);

public class TransferPickConfirmedHandler(InMemoryStore store)
{
    public IResult Handle(TransferPickConfirmedRequest req)
    {
        var to = store.FindTO(req.TransferOrderId);
        if (to is null) return ApiResult.NotFound("transfer order", req.TransferOrderId);

        to.Status = "PickConfirmed";
        to.UpdatedAt = DateTime.UtcNow;

        store.AddTransferConfirmation(req.TransferOrderId, new TransferConfirmationDto
        {
            Type = "PickConfirmed",
            ConfirmedAt = req.ConfirmedAt,
            ConfirmedBy = "WMS",
            Tracking = to.Tracking
        });

        return Results.Accepted(null, new { accepted = true, transferOrderId = req.TransferOrderId, newStatus = "PickConfirmed" });
    }
}
