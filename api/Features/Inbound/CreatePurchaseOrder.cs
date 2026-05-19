namespace OmsApi;

public record CreatePurchaseOrderLineRequest(string Sku, int OrderedQty, decimal UnitCost,
    string Currency, string Condition = "Resellable", string? Sloc = null);

public record CreatePurchaseOrderRequest(string PoNumber, string SupplierId, string Supplier,
    string StoreId, string Store, List<CreatePurchaseOrderLineRequest> Lines);

public class CreatePurchaseOrderHandler(InMemoryStore store)
{
    public IResult Handle(CreatePurchaseOrderRequest req)
    {
        var id = store.NextId("PO", store.PurchaseOrders.Select(p => p.Id));
        var now = DateTime.UtcNow;
        var po = new PurchaseOrderDto
        {
            Id = id,
            PoNumber = req.PoNumber,
            SupplierId = req.SupplierId,
            Supplier = req.Supplier,
            StoreId = req.StoreId,
            Store = req.Store,
            Status = "Created",
            Value = req.Lines.Sum(l => l.OrderedQty * l.UnitCost),
            CreatedAt = now,
            UpdatedAt = now,
            Lines = req.Lines.Select((l, i) => new PurchaseOrderLineDto
            {
                PoLineId = $"{id}-L{i + 1:D3}",
                PurchaseOrderId = id,
                Sku = l.Sku,
                OrderedQty = l.OrderedQty,
                ReceivedQty = 0,
                UnitCost = l.UnitCost,
                Currency = l.Currency,
                Condition = l.Condition,
                Sloc = l.Sloc
            }).ToList()
        };
        store.AddPO(po);
        return Results.Created($"/api/inbound/purchase-orders/{id}", po);
    }
}
