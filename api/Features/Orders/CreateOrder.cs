namespace OmsApi;

public record CreateOrderAddressRequest(
    string AddressType, string FirstName, string LastName, string MobilePhone,
    string? Email, string Address1, string Subdistrict, string District,
    string Province, string PostalCode);

public record CreateOrderLineRequest(string Sku, string ProductName, string Barcode,
    decimal RequestedQty, decimal UnitPrice, string UnitOfMeasure);

public record CreateOrderSlotRequest(DateTime ScheduledStart, DateTime ScheduledEnd,
    string? BookedVia = null, string? BookingRef = null);

public record CreateOrderRequest(
    string SourceOrderId, string ChannelType, string BusinessUnit, string StoreId,
    string FulfillmentType, string PaymentMethod, bool IsPrepaid,
    string CustomerName, string CustomerPhone, string? CustomerEmail,
    string? ExternalCustomerId,
    CreateOrderAddressRequest? DeliveryAddress,
    CreateOrderSlotRequest? DeliverySlot,
    List<CreateOrderLineRequest> Lines);

public class CreateOrderHandler(InMemoryStore store)
{
    public IResult Handle(CreateOrderRequest req)
    {
        var id = store.NextId("ORD", store.Orders.Select(o => o.Id));
        var num = store.NextId("SC", store.Orders.Select(o => o.OrderNumber));
        var now = DateTime.UtcNow;

        var addresses = new List<OrderAddressDto>();
        if (req.DeliveryAddress is { } addr)
        {
            addresses.Add(new OrderAddressDto
            {
                AddressId = $"ADDR-{Guid.NewGuid():N}"[..12],
                AddressType = addr.AddressType,
                FirstName = addr.FirstName,
                LastName = addr.LastName,
                MobilePhone = addr.MobilePhone,
                Email = addr.Email,
                Address1 = addr.Address1,
                Subdistrict = addr.Subdistrict,
                District = addr.District,
                Province = addr.Province,
                PostalCode = addr.PostalCode
            });
        }

        var order = new OrderDto
        {
            Id = id,
            OrderNumber = num,
            SourceOrderId = req.SourceOrderId,
            Customer = req.CustomerName,
            CustomerPhone = req.CustomerPhone,
            CustomerEmail = req.CustomerEmail,
            ExternalCustomerId = req.ExternalCustomerId,
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
            IsPrepaid = req.IsPrepaid,
            Items = req.Lines.Count,
            Amount = req.Lines.Sum(l => l.RequestedQty * l.UnitPrice),
            CreatedBy = "api",
            UpdatedBy = "api",
            Addresses = addresses,
            DeliverySlot = req.DeliverySlot is { } slot ? new DeliverySlotDto
            {
                SlotId = $"SLOT-{Guid.NewGuid():N}"[..12],
                StoreId = req.StoreId,
                ScheduledStart = slot.ScheduledStart,
                ScheduledEnd = slot.ScheduledEnd,
                BookedVia = slot.BookedVia,
                BookingRef = slot.BookingRef,
                CreatedAt = now,
                UpdatedAt = now
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
                UnitPrice = l.UnitPrice,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList()
        };

        store.AddOrder(order);
        store.AppendEvent(id, ApiResult.DomainEvent("OrderCreated", OrderStatus.Pending,
            $"Order {num} created via {req.ChannelType}."));
        return Results.Created($"/api/orders/{id}", order);
    }
}
