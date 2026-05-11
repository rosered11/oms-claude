namespace OmsApi;

public record CreateOrderLineRequest(string Sku, string ProductName, string Barcode,
    decimal RequestedQty, decimal UnitPrice, string UnitOfMeasure);

public record CreateOrderSlotRequest(DateTime ScheduledStart, DateTime ScheduledEnd);

public record CreateOrderRequest(
    string SourceOrderId, string ChannelType, string BusinessUnit, string StoreId,
    string FulfillmentType, string PaymentMethod, string CustomerName, string CustomerPhone,
    string? CustomerEmail, CreateOrderSlotRequest? DeliverySlot, List<CreateOrderLineRequest> Lines);

public class CreateOrderHandler(InMemoryStore store)
{
    public IResult Handle(CreateOrderRequest req)
    {
        var id = store.NextId("ORD", store.Orders.Select(o => o.Id));
        var num = store.NextId("SC", store.Orders.Select(o => o.OrderNumber));
        var now = DateTime.UtcNow;

        var order = new OrderDto
        {
            Id = id,
            OrderNumber = num,
            Customer = req.CustomerName,
            CustomerPhone = req.CustomerPhone,
            CustomerEmail = req.CustomerEmail,
            ChannelType = req.ChannelType,
            BusinessUnit = req.BusinessUnit,
            StoreId = req.StoreId,
            OrderDate = now,
            CreatedAt = now,
            UpdatedAt = now,
            Status = OrderStatus.Pending,
            Type = "Standard",
            FulfillmentType = req.FulfillmentType,
            PaymentMethod = req.PaymentMethod,
            Items = req.Lines.Count,
            Amount = req.Lines.Sum(l => l.RequestedQty * l.UnitPrice),
            CreatedBy = "api",
            UpdatedBy = "api",
            DeliverySlot = req.DeliverySlot is { } slot ? new DeliverySlotDto
            {
                SlotId = $"SLOT-{Guid.NewGuid():N}"[..12],
                StoreId = req.StoreId,
                ScheduledStart = slot.ScheduledStart,
                ScheduledEnd = slot.ScheduledEnd
            } : null,
            Lines = req.Lines.Select((l, i) => new OrderLineDto
            {
                Id = $"LINE-{i + 1:D3}",
                Sku = l.Sku,
                ProductName = l.ProductName,
                Barcode = l.Barcode,
                RequestedAmount = l.RequestedQty,
                PickedAmount = 0,
                Uom = l.UnitOfMeasure,
                UnitPrice = l.UnitPrice
            }).ToList()
        };

        store.AddOrder(order);
        store.AppendEvent(id, ApiResult.DomainEvent("OrderCreated", OrderStatus.Pending,
            $"Order {num} created via {req.ChannelType}."));
        return Results.Created($"/api/orders/{id}", order);
    }
}
