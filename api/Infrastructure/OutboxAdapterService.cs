using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OmsApi;

public class OutboxAdapterService(InMemoryStore store, IHttpClientFactory httpClientFactory)
{
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

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

            var apiHeaders = new Dictionary<string, string>(config.Headers);
            string authDetail;
            string? tokenBody = null;

            switch (config.AuthType)
            {
                case "OAuth2ClientCredentials":
                    var token = FetchOAuth2Token(config, out var rawTokenResponse, out tokenBody);
                    apiHeaders["Authorization"] = $"Bearer {token}";
                    authDetail = $"OAuth2 token fetched from {config.TokenUrl}";
                    log.TokenUrl = config.TokenUrl;
                    log.TokenRequestBody = tokenBody;
                    log.TokenRequestHeaders = config.TokenRequestHeaders.Count > 0
                        ? string.Join("\n", config.TokenRequestHeaders.Select(kv => $"{kv.Key}: {kv.Value}"))
                        : null;
                    log.TokenResponsePayload = rawTokenResponse;
                    break;
                case "StaticToken":
                    apiHeaders[config.StaticTokenHeader] = config.StaticToken ?? "";
                    authDetail = $"Static token set on header '{config.StaticTokenHeader}'";
                    break;
                default:
                    authDetail = "No auth";
                    break;
            }

            log.AuthType = config.AuthType;
            log.BaseUrl = config.BaseUrl;
            log.ApiRequestHeaders = apiHeaders.Count > 0
                ? string.Join("\n", apiHeaders.Select(kv => $"{kv.Key}: {kv.Value}"))
                : null;

            var (statusCode, responseBody) = CallApi(config.BaseUrl, apiHeaders, requestPayload);

            log.Status = statusCode >= 200 && statusCode < 300 ? "Success" : "Failed";
            log.HttpStatusCode = statusCode;
            log.ResponsePayload = responseBody;
            log.CompletedAt = DateTime.UtcNow;
            if (log.Status == "Failed")
                log.ErrorMessage = $"HTTP {statusCode}: {responseBody}";

            store.AddDispatchLog(log);
            results.Add(ApiResult.OutboxEvent(rule.TargetSystem, triggerEvent,
                $"{rule.EndpointKey} [{authDetail}] HTTP {statusCode}"));
        }

        return results;
    }

    public TimelineEventDto? Retry(long logId)
    {
        var log = store.GetDispatchLog(logId);
        if (log is null || log.Status != "Failed") return null;

        var config = store.GetEndpointConfig(log.EndpointKey);
        if (config is not null && config.IsActive && log.BaseUrl is not null)
        {
            var apiHeaders = new Dictionary<string, string>(config.Headers);
            switch (config.AuthType)
            {
                case "OAuth2ClientCredentials":
                    var token = FetchOAuth2Token(config, out _, out _);
                    apiHeaders["Authorization"] = $"Bearer {token}";
                    break;
                case "StaticToken":
                    apiHeaders[config.StaticTokenHeader] = config.StaticToken ?? "";
                    break;
            }
            var (statusCode, responseBody) = CallApi(log.BaseUrl, apiHeaders, log.RequestPayload ?? "{}");
            log.Status = statusCode >= 200 && statusCode < 300 ? "Success" : "Failed";
            log.HttpStatusCode = statusCode;
            log.ResponsePayload = responseBody;
            if (log.Status == "Failed")
                log.ErrorMessage = $"HTTP {statusCode}: {responseBody}";
            else
                log.ErrorMessage = null;
        }
        else
        {
            log.Status = "Success";
            log.HttpStatusCode = 200;
            log.ResponsePayload = "{\"accepted\":true,\"retried\":true}";
            log.ErrorMessage = null;
        }

        log.AttemptCount++;
        log.CompletedAt = DateTime.UtcNow;

        return ApiResult.OutboxEvent(log.TargetSystem, log.TriggerEvent,
            $"Retry #{log.AttemptCount}: {log.EndpointKey} HTTP {log.HttpStatusCode}");
    }

    private string FetchOAuth2Token(OutboxEndpointConfig config, out string rawResponse, out string? formBody)
    {
        formBody = $"grant_type={config.GrantType}&client_id={config.ClientId}";
        if (!string.IsNullOrEmpty(config.Scope)) formBody += $"&scope={config.Scope}";
        if (!string.IsNullOrEmpty(config.ClientSecret)) formBody += $"&client_secret={config.ClientSecret}";
        if (config.AdditionalTokenParams.Count > 0)
            formBody += "&" + string.Join("&", config.AdditionalTokenParams.Select(kv => $"{kv.Key}={kv.Value}"));

        rawResponse = "{}";
        try
        {
            var client = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, config.TokenUrl)
            {
                Content = new StringContent(formBody, Encoding.UTF8, "application/x-www-form-urlencoded")
            };
            foreach (var h in config.TokenRequestHeaders)
                request.Headers.TryAddWithoutValidation(h.Key, h.Value);

            var response = client.Send(request);
            rawResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var doc = JsonDocument.Parse(rawResponse);
            return doc.RootElement.GetProperty("access_token").GetString() ?? "error-token";
        }
        catch
        {
            return "fallback-token";
        }
    }

    private (int statusCode, string body) CallApi(string url, Dictionary<string, string> headers, string payload)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            foreach (var h in headers)
                request.Headers.TryAddWithoutValidation(h.Key, h.Value);

            var response = client.Send(request);
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return ((int)response.StatusCode, string.IsNullOrEmpty(body) ? "{}" : body);
        }
        catch (Exception ex)
        {
            return (0, $"{{\"error\":\"{ex.Message}\"}}");
        }
    }
}
