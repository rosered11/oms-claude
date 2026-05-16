using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/config")]
public class ConfigController(
    GetRoutingRulesHandler getRoutingRules,
    CreateRoutingRuleHandler createRoutingRule,
    UpdateRoutingRuleHandler updateRoutingRule,
    DeleteRoutingRuleHandler deleteRoutingRule,
    GetEndpointConfigsHandler getEndpointConfigs,
    UpsertEndpointConfigHandler upsertEndpointConfig,
    DeleteEndpointConfigHandler deleteEndpointConfig) : ControllerBase
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

    [HttpGet("outbox-endpoints")]
    public IResult GetEndpoints() => getEndpointConfigs.Handle();

    [HttpGet("outbox-endpoints/{key}")]
    public IResult GetEndpoint(string key) => getEndpointConfigs.HandleOne(key);

    [HttpPost("outbox-endpoints")]
    public IResult UpsertEndpoint([FromBody] UpsertEndpointConfigRequest req) => upsertEndpointConfig.Handle(req);

    [HttpDelete("outbox-endpoints/{key}")]
    public IResult DeleteEndpoint(string key) => deleteEndpointConfig.Handle(key);
}
