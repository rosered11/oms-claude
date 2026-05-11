namespace OmsApi;

public record CreateTransferOrderLineRequest(string Sku, int RequestedQty);

public record CreateTransferOrderRequest(string SourceStoreId, string Source,
    string DestStoreId, string Dest, List<CreateTransferOrderLineRequest> Lines);

public class CreateTransferOrderHandler(InMemoryStore store)
{
    public IResult Handle(CreateTransferOrderRequest req)
    {
        var id = store.NextId("TO", store.TransferOrders.Select(t => t.Id));
        var now = DateTime.UtcNow;
        var to = new TransferOrderDto
        {
            Id = id,
            Source = req.Source,
            Dest = req.Dest,
            Lines = req.Lines.Count,
            Status = "Draft",
            CreatedAt = now,
            UpdatedAt = now
        };
        store.AddTO(to);
        return Results.Created($"/api/inbound/transfer-orders/{id}", to);
    }
}
