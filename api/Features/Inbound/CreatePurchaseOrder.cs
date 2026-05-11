namespace OmsApi;

public record CreatePurchaseOrderLineRequest(string Sku, int OrderedQty, decimal UnitCost, string Currency);

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
            Supplier = req.Supplier,
            Lines = req.Lines.Count,
            Status = "Draft",
            Store = req.Store,
            Value = req.Lines.Sum(l => l.OrderedQty * l.UnitCost),
            CreatedAt = now,
            UpdatedAt = now
        };
        store.AddPO(po);
        return Results.Created($"/api/inbound/purchase-orders/{id}", po);
    }
}
