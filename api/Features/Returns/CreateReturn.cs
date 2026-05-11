namespace OmsApi;

public record CreateReturnItemRequest(string OrderLineId, string Sku, decimal Quantity, string ItemReason);

public record CreateReturnRequest(string OrderId, string ReturnReason,
    List<CreateReturnItemRequest> Items, string RequestedBy);

public class CreateReturnHandler(InMemoryStore store)
{
    public IResult Handle(CreateReturnRequest req)
    {
        var order = store.FindOrder(req.OrderId);
        if (order is null) return ApiResult.NotFound("order", req.OrderId);

        var id = store.NextId("RET", store.Returns.Select(r => r.Id));
        var now = DateTime.UtcNow;

        var ret = new ReturnDto
        {
            Id = id,
            OrderId = req.OrderId,
            ReturnOrderNumber = id,
            Status = "Requested",
            ReturnReason = req.ReturnReason,
            RequestedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = req.RequestedBy,
            UpdatedBy = req.RequestedBy,
            Items = req.Items.Select((i, idx) =>
            {
                var line = order.Lines.FirstOrDefault(l => l.Id == i.OrderLineId);
                return new ReturnItemDto
                {
                    Id = $"RITEM-{idx + 1:D3}",
                    OrderLineId = i.OrderLineId,
                    Sku = i.Sku,
                    ProductName = line?.ProductName ?? i.Sku,
                    Barcode = line?.Barcode ?? "",
                    Quantity = i.Quantity,
                    Uom = line?.Uom ?? "EA",
                    UnitPrice = line?.UnitPrice ?? 0,
                    ItemReason = i.ItemReason,
                    PaymentMethod = order.PaymentMethod
                };
            }).ToList()
        };

        store.AddReturn(ret);
        return Results.Created($"/api/returns/{id}", ret);
    }
}
