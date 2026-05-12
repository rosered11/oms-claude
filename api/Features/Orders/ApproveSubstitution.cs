namespace OmsApi;

public class ApproveSubstitutionHandler(InMemoryStore store)
{
    public IResult Handle(string orderId, string subId)
    {
        var sub = store.FindSubstitution(orderId, subId);
        if (sub is null) return ApiResult.NotFound("substitution", subId);
        if (sub.CustomerApproved.HasValue)
            return Results.Conflict(new { error = "conflict", detail = $"Substitution {subId} has already been actioned." });

        sub.CustomerApproved = true;
        sub.ApprovedAt = DateTime.UtcNow;
        return Results.Ok(new { substitutionId = subId, customerApproved = true, approvedAt = sub.ApprovedAt });
    }
}
