using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/config")]
public class ConfigController(
    GetRoutingRulesHandler getRoutingRules,
    CreateRoutingRuleHandler createRoutingRule,
    UpdateRoutingRuleHandler updateRoutingRule,
    DeleteRoutingRuleHandler deleteRoutingRule) : ControllerBase
{
    [HttpGet("outbox-routing-rules")]
    public IResult GetAll() => getRoutingRules.Handle();

    [HttpGet("outbox-routing-rules/{ruleId:long}")]
    public IResult GetOne(long ruleId) => getRoutingRules.HandleOne(ruleId);

    [HttpPost("outbox-routing-rules")]
    public IResult Create([FromBody] CreateRoutingRuleRequest req) => createRoutingRule.Handle(req);

    [HttpPut("outbox-routing-rules/{ruleId:long}")]
    public IResult Update(long ruleId, [FromBody] UpdateRoutingRuleRequest req) => updateRoutingRule.Handle(ruleId, req);

    [HttpDelete("outbox-routing-rules/{ruleId:long}")]
    public IResult Delete(long ruleId) => deleteRoutingRule.Handle(ruleId);
}
