namespace OmsApi;

public record CancelReturnRequest(string Reason, string CancelledBy);

public class CancelReturnHandler(InMemoryStore store)
{
    public IResult Handle(string id, CancelReturnRequest req)
    {
        var ret = store.FindReturn(id);
        if (ret is null) return ApiResult.NotFound("return", id);
        if (ret.Status is not ("Requested" or "ReturnRequested" or "PickupScheduled"))
            return Results.Conflict(new { error = "invalid_transition", detail = $"Return {id} is in status {ret.Status}. It cannot be cancelled after goods receipt." });

        ret.Status = "Cancelled";
        ret.UpdatedAt = DateTime.UtcNow;
        ret.UpdatedBy = req.CancelledBy;
        return Results.Ok(new { id, newStatus = "Cancelled" });
    }
}
