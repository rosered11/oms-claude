namespace OmsApi;

public class OutboxAdapterService(InMemoryStore store)
{
    public IEnumerable<TimelineEventDto> Dispatch(
        string orderId, string channelType, string subChannel, string businessUnit,
        string triggerEvent, string requestPayload)
    {
        var rules = store.GetRoutingRules(channelType, subChannel, businessUnit, triggerEvent).ToList();
        var results = new List<TimelineEventDto>();

        foreach (var rule in rules)
        {
            var config = store.GetEndpointConfig(rule.EndpointKey);
            var log = new OutboxDispatchLog
            {
                LogId = store.NextDispatchLogId(),
                OrderId = orderId,
                EndpointKey = rule.EndpointKey,
                TriggerEvent = triggerEvent,
                TargetSystem = rule.TargetSystem,
                RequestPayload = requestPayload,
                AttemptCount = 1,
                CreatedAt = DateTime.UtcNow
            };

            if (config is null || !config.IsActive)
            {
                log.Status = "Failed";
                log.ErrorMessage = config is null
                    ? $"No endpoint config found for key '{rule.EndpointKey}'."
                    : $"Endpoint '{rule.EndpointKey}' is inactive.";
                log.CompletedAt = DateTime.UtcNow;
                store.AddDispatchLog(log);
                results.Add(ApiResult.OutboxEvent(rule.TargetSystem, triggerEvent,
                    $"{requestPayload} → {rule.EndpointKey} [FAILED: {log.ErrorMessage}]"));
                continue;
            }

            // Headers sent to Token URL (OAuth2 only)
            var tokenHeaders = new Dictionary<string, string>(config.TokenRequestHeaders);

            // Headers sent to Base URL
            var apiHeaders = new Dictionary<string, string>(config.Headers);

            string authDetail;
            string? tokenBody = null;

            switch (config.AuthType)
            {
                case "OAuth2ClientCredentials":
                    apiHeaders["Authorization"] = "Bearer <oauth2-token>";
                    tokenBody = $"grant_type={config.GrantType}&client_id={config.ClientId}"
                        + (!string.IsNullOrEmpty(config.Scope) ? $"&scope={config.Scope}" : "")
                        + (config.AdditionalTokenParams.Count > 0
                            ? "&" + string.Join("&", config.AdditionalTokenParams.Select(kv => $"{kv.Key}={kv.Value}"))
                            : "");
                    authDetail = $"OAuth2 token fetched from {config.TokenUrl}";
                    break;
                case "StaticToken":
                    apiHeaders[config.StaticTokenHeader] = config.StaticToken ?? "";
                    authDetail = $"Static token set on header '{config.StaticTokenHeader}'";
                    break;
                default:
                    authDetail = "No auth";
                    break;
            }

            // Populate per-phase log fields
            log.AuthType = config.AuthType;
            log.BaseUrl = config.BaseUrl;
            log.ApiRequestHeaders = apiHeaders.Count > 0
                ? string.Join("\n", apiHeaders.Select(kv => $"{kv.Key}: {kv.Value}"))
                : null;

            if (config.AuthType == "OAuth2ClientCredentials")
            {
                log.TokenUrl = config.TokenUrl;
                log.TokenRequestBody = tokenBody;
                log.TokenRequestHeaders = tokenHeaders.Count > 0
                    ? string.Join("\n", tokenHeaders.Select(kv => $"{kv.Key}: {kv.Value}"))
                    : null;
                log.TokenResponsePayload = "{\"access_token\":\"<simulated-token>\",\"token_type\":\"Bearer\",\"expires_in\":3600}";
            }

            log.Status = "Success";
            log.HttpStatusCode = 200;
            log.ResponsePayload = "{\"accepted\":true}";
            log.CompletedAt = DateTime.UtcNow;
            store.AddDispatchLog(log);

            results.Add(ApiResult.OutboxEvent(rule.TargetSystem, triggerEvent,
                $"{rule.EndpointKey} [{authDetail}] HTTP 200"));
        }

        return results;
    }

    public TimelineEventDto? Retry(long logId)
    {
        var log = store.GetDispatchLog(logId);
        if (log is null || log.Status != "Failed") return null;

        log.Status = "Success";
        log.AttemptCount++;
        log.HttpStatusCode = 200;
        log.ResponsePayload = "{\"accepted\":true,\"retried\":true}";
        log.CompletedAt = DateTime.UtcNow;
        log.ErrorMessage = null;

        return ApiResult.OutboxEvent(log.TargetSystem, log.TriggerEvent,
            $"Retry #{log.AttemptCount}: {log.EndpointKey} HTTP 200");
    }
}
