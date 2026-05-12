namespace OmsApi;

public record TransferReceivedRequest(string TransferOrderId, DateTime ReceivedAt);

public class TransferReceivedHandler(InMemoryStore store)
{
    public IResult Handle(TransferReceivedRequest req)
    {
        var to = store.FindTO(req.TransferOrderId);
        if (to is null) return ApiResult.NotFound("transfer order", req.TransferOrderId);

        to.Status = "Completed";
        to.UpdatedAt = DateTime.UtcNow;

        store.AddTransferConfirmation(req.TransferOrderId, new TransferConfirmationDto
        {
            Type = "TransferReceived",
            ConfirmedAt = req.ReceivedAt,
            ConfirmedBy = "WMS",
            Tracking = to.Tracking
        });

        return Results.Accepted(null, new { accepted = true, transferOrderId = req.TransferOrderId, newStatus = "Completed" });
    }
}
