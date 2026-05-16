namespace OmsApi;

public class GetEndpointConfigsHandler(InMemoryStore store)
{
    public IResult Handle() =>
        Results.Ok(new { data = store.EndpointConfigs.Select(ToDto) });

    public IResult HandleOne(string key)
    {
        var config = store.GetEndpointConfig(key);
        if (config is null) return ApiResult.NotFound("endpoint_config", key);
        return Results.Ok(new { data = ToDto(config) });
    }

    private static OutboxEndpointConfigDto ToDto(OutboxEndpointConfig c) => new()
    {
        EndpointKey = c.EndpointKey,
        BaseUrl = c.BaseUrl,
        Headers = c.Headers,
        TokenRequestHeaders = c.TokenRequestHeaders,
        AuthType = c.AuthType,
        StaticToken = c.StaticToken,
        StaticTokenHeader = c.StaticTokenHeader,
        TokenUrl = c.TokenUrl,
        ClientId = c.ClientId,
        Scope = c.Scope,
        GrantType = c.GrantType,
        AdditionalTokenParams = c.AdditionalTokenParams,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}

public record UpsertEndpointConfigRequest(
    string EndpointKey,
    string BaseUrl,
    Dictionary<string, string>? Headers,
    Dictionary<string, string>? TokenRequestHeaders,
    string AuthType,
    string? StaticToken,
    string? StaticTokenHeader,
    string? TokenUrl,
    string? ClientId,
    string? ClientSecret,
    string? Scope,
    string? GrantType,
    Dictionary<string, string>? AdditionalTokenParams,
    bool IsActive = true);

public class UpsertEndpointConfigHandler(InMemoryStore store)
{
    public IResult Handle(UpsertEndpointConfigRequest req)
    {
        var existing = store.GetEndpointConfig(req.EndpointKey);
        var config = new OutboxEndpointConfig
        {
            EndpointKey = req.EndpointKey,
            BaseUrl = req.BaseUrl,
            Headers = req.Headers ?? new(),
            TokenRequestHeaders = req.TokenRequestHeaders ?? new(),
            AuthType = req.AuthType,
            StaticToken = req.StaticToken,
            StaticTokenHeader = req.StaticTokenHeader ?? existing?.StaticTokenHeader ?? "Authorization",
            TokenUrl = req.TokenUrl,
            ClientId = req.ClientId,
            ClientSecret = req.ClientSecret ?? existing?.ClientSecret,
            Scope = req.Scope,
            GrantType = req.GrantType ?? "client_credentials",
            AdditionalTokenParams = req.AdditionalTokenParams ?? new(),
            IsActive = req.IsActive,
            CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        store.UpsertEndpointConfig(config);
        var saved = store.GetEndpointConfig(req.EndpointKey)!;
        return Results.Ok(new { data = saved });
    }
}

public class DeleteEndpointConfigHandler(InMemoryStore store)
{
    public IResult Handle(string key)
    {
        var deleted = store.DeleteEndpointConfig(key);
        if (!deleted) return ApiResult.NotFound("endpoint_config", key);
        return Results.Ok(new { deleted = true, endpointKey = key });
    }
}
