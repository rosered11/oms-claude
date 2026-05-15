using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

public class GetRoutingRulesHandler(InMemoryStore store)
{
    public IResult Handle() =>
        Results.Ok(new { data = store.RoutingRules });

    public IResult HandleOne(long ruleId)
    {
        var rule = store.RoutingRules.FirstOrDefault(r => r.RuleId == ruleId);
        if (rule is null) return ApiResult.NotFound("routing_rule", ruleId.ToString());
        return Results.Ok(new { data = rule });
    }
}

public record CreateRoutingRuleRequest(
    string ChannelType, string SubChannel, string BusinessUnit, string TriggerEvent,
    string TargetSystem, string EndpointKey, int ExecutionOrder);

public class CreateRoutingRuleHandler(InMemoryStore store)
{
    public IResult Handle(CreateRoutingRuleRequest req)
    {
        var nextId = store.RoutingRules.Count > 0
            ? store.RoutingRules.Max(r => r.RuleId) + 1
            : 1;

        var rule = new OutboxRoutingRule
        {
            RuleId = nextId,
            ChannelType = req.ChannelType,
            SubChannel = req.SubChannel,
            BusinessUnit = req.BusinessUnit,
            TriggerEvent = req.TriggerEvent,
            TargetSystem = req.TargetSystem,
            EndpointKey = req.EndpointKey,
            ExecutionOrder = req.ExecutionOrder,
            IsActive = true
        };
        store.RoutingRules.Add(rule);
        return Results.Created($"/api/config/outbox-routing-rules/{rule.RuleId}", new { data = rule });
    }
}

public record UpdateRoutingRuleRequest(
    string ChannelType, string SubChannel, string BusinessUnit, string TriggerEvent,
    string TargetSystem, string EndpointKey, int ExecutionOrder, bool IsActive);

public class UpdateRoutingRuleHandler(InMemoryStore store)
{
    public IResult Handle(long ruleId, UpdateRoutingRuleRequest req)
    {
        var rule = store.RoutingRules.FirstOrDefault(r => r.RuleId == ruleId);
        if (rule is null) return ApiResult.NotFound("routing_rule", ruleId.ToString());

        rule.ChannelType = req.ChannelType;
        rule.SubChannel = req.SubChannel;
        rule.BusinessUnit = req.BusinessUnit;
        rule.TriggerEvent = req.TriggerEvent;
        rule.TargetSystem = req.TargetSystem;
        rule.EndpointKey = req.EndpointKey;
        rule.ExecutionOrder = req.ExecutionOrder;
        rule.IsActive = req.IsActive;
        return Results.Ok(new { data = rule });
    }
}

public class DeleteRoutingRuleHandler(InMemoryStore store)
{
    public IResult Handle(long ruleId)
    {
        var rule = store.RoutingRules.FirstOrDefault(r => r.RuleId == ruleId);
        if (rule is null) return ApiResult.NotFound("routing_rule", ruleId.ToString());
        rule.IsActive = false;
        return Results.Ok(new { data = rule });
    }
}
