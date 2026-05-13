namespace OmsApi;

public record TransferReceivedRequest(string TransferOrderId, DateTime ReceivedAt,
    string? UpdatedBy = null);

public class TransferReceivedHandler(InMemoryStore store)
{
    public IResult Handle(TransferReceivedRequest req)
    {
        var to = store.FindTO(req.TransferOrderId);
        if (to is null) return ApiResult.NotFound("transfer order", req.TransferOrderId);

        var now = DateTime.UtcNow;
        to.Status = "Completed";
        to.UpdatedAt = now;
        to.UpdatedBy = req.UpdatedBy;

        foreach (var line in to.Lines)
            line.ConfirmedAt ??= req.ReceivedAt;

        store.AddTransferConfirmation(req.TransferOrderId, new TransferConfirmationDto
        {
            Type = "TransferReceived",
            ConfirmedAt = req.ReceivedAt,
            ConfirmedBy = req.UpdatedBy ?? "WMS",
            Tracking = to.Tracking
        });

        return Results.Accepted(null, new { accepted = true, transferOrderId = req.TransferOrderId, newStatus = "Completed" });
    }
}
