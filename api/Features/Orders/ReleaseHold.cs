namespace OmsApi;

public record ReleaseHoldRequest(string ReleasedBy);

public class ReleaseHoldHandler(InMemoryStore store)
{
    public IResult Handle(string id, ReleaseHoldRequest req)
    {
        var o = store.FindOrder(id);
        if (o is null) return ApiResult.NotFound("order", id);
        if (o.Status != OrderStatus.OnHold) return ApiResult.InvalidTransition(o.Status, "resume");

        o.Status = o.PreHoldStatus ?? OrderStatus.Pending;
        o.PreHoldStatus = null;
        o.HoldReason = null;
        o.UpdatedAt = DateTime.UtcNow;
        o.UpdatedBy = req.ReleasedBy;
        store.AppendEvent(id, ApiResult.DomainEvent("OrderHoldReleased", o.Status,
            $"Hold released by {req.ReleasedBy}."));
        return Results.Ok(o);
    }
}
