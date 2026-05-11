namespace OmsApi;

public class GetOrdersHandler(InMemoryStore store)
{
    public IResult Handle(string? status, string? channelType, string? fulfillmentType,
        string? storeId, string? search, int page, int pageSize)
    {
        var q = store.Orders.AsEnumerable();

        if (!string.IsNullOrEmpty(status))
            q = q.Where(o => o.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(channelType))
            q = q.Where(o => o.ChannelType.Equals(channelType, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(fulfillmentType))
            q = q.Where(o => o.FulfillmentType.Equals(fulfillmentType, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(storeId))
            q = q.Where(o => o.StoreId.Equals(storeId, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(search))
            q = q.Where(o => o.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                           || o.Customer.Contains(search, StringComparison.OrdinalIgnoreCase));

        var total = q.Count();
        var items = q.OrderByDescending(o => o.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Results.Ok(new { data = items, total, page, pageSize });
    }
}
