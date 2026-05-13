namespace OmsApi;

public record CreateTransferOrderLineRequest(string Sku, int RequestedQty);

public record CreateTransferOrderRequest(string SourceStoreId, string Source,
    string DestStoreId, string Dest, List<CreateTransferOrderLineRequest> Lines);

public class CreateTransferOrderHandler(InMemoryStore store)
{
    public IResult Handle(CreateTransferOrderRequest req)
    {
        var id = store.NextId("TO", store.TransferOrders.Select(t => t.Id));
        var transferNumber = store.NextId("TRF", store.TransferOrders.Select(t => t.TransferNumber));
        var now = DateTime.UtcNow;
        var to = new TransferOrderDto
        {
            Id = id,
            TransferNumber = transferNumber,
            SourceStoreId = req.SourceStoreId,
            Source = req.Source,
            DestStoreId = req.DestStoreId,
            Dest = req.Dest,
            Status = "Draft",
            CreatedAt = now,
            UpdatedAt = now,
            Lines = req.Lines.Select((l, i) => new TransferOrderLineDto
            {
                ToLineId = $"{id}-L{i + 1:D3}",
                TransferOrderId = id,
                Sku = l.Sku,
                RequestedQty = l.RequestedQty,
                TransferredQty = 0
            }).ToList()
        };
        store.AddTO(to);
        return Results.Created($"/api/inbound/transfer-orders/{id}", to);
    }
}
