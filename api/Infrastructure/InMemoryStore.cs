using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmsApi;

public class InMemoryStore
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public List<OrderDto> Orders { get; private set; } = [];
    public List<ReturnDto> Returns { get; private set; } = [];
    public List<PurchaseOrderDto> PurchaseOrders { get; private set; } = [];
    public List<TransferOrderDto> TransferOrders { get; private set; } = [];
    public List<TimelineEventDto> OrderTimeline { get; private set; } = [];
    public List<TimelineEventDto> PrepaidTimeline { get; private set; } = [];

    private readonly Dictionary<string, List<TimelineEventDto>> _orderEvents = [];

    public InMemoryStore(IWebHostEnvironment env)
    {
        var dataPath = Path.Combine(env.ContentRootPath, "..", "web-ui", "data");
        Orders = Load<List<OrderDto>>(dataPath, "orders.json") ?? [];
        Returns = Load<List<ReturnDto>>(dataPath, "returns.json") ?? [];
        PurchaseOrders = Load<List<PurchaseOrderDto>>(dataPath, "purchase-orders.json") ?? [];
        TransferOrders = Load<List<TransferOrderDto>>(dataPath, "transfer-orders.json") ?? [];
        OrderTimeline = Load<List<TimelineEventDto>>(dataPath, "order-timeline.json") ?? [];
        PrepaidTimeline = Load<List<TimelineEventDto>>(dataPath, "prepaid-timeline.json") ?? [];

        if (Orders.Count > 0)
            _orderEvents["ORD-001"] = [.. OrderTimeline];
    }

    // ── Orders ────────────────────────────────────────────────────────────────

    public OrderDto? FindOrder(string id) =>
        Orders.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public OrderDto? FindOrderByTracking(string trackingId) =>
        Orders.FirstOrDefault(o => o.Packages.Any(p => p.TrackingId == trackingId));

    public void AddOrder(OrderDto order)
    {
        Orders.Add(order);
        _orderEvents[order.Id] = [];
    }

    // ── Timeline ──────────────────────────────────────────────────────────────

    public List<TimelineEventDto> GetTimeline(string orderId)
    {
        _orderEvents.TryGetValue(orderId, out var events);
        return events ?? [];
    }

    public void AppendEvent(string orderId, TimelineEventDto evt)
    {
        if (!_orderEvents.TryGetValue(orderId, out var list))
        {
            list = [];
            _orderEvents[orderId] = list;
        }
        evt.Id = list.Count + 1;
        list.Add(evt);
    }

    // ── Returns ───────────────────────────────────────────────────────────────

    public ReturnDto? FindReturn(string id) =>
        Returns.FirstOrDefault(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public void AddReturn(ReturnDto ret) => Returns.Add(ret);

    // ── Inbound ───────────────────────────────────────────────────────────────

    public PurchaseOrderDto? FindPO(string id) =>
        PurchaseOrders.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public TransferOrderDto? FindTO(string id) =>
        TransferOrders.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public void AddPO(PurchaseOrderDto po) => PurchaseOrders.Add(po);
    public void AddTO(TransferOrderDto to) => TransferOrders.Add(to);

    // ── Stock ─────────────────────────────────────────────────────────────────

    public StockLedgerDto GetStockLedger(string sku)
    {
        var pickedOrders = Orders
            .Where(o => o.Lines.Any(l => l.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase) && l.PickedAmount > 0))
            .GroupBy(o => o.StoreId)
            .ToList();

        var locations = new List<StockLocationDto>();
        int eventId = 1;

        foreach (var storeGroup in pickedOrders)
        {
            var events = new List<StockEventDto>();
            int balance = 0;

            int poQty = 20;
            balance += poQty;
            events.Add(new StockEventDto
            {
                Id = eventId++,
                Time = "09:00",
                Dir = "in",
                Ref = "PO-001",
                RefType = "PurchaseOrder",
                Event = "PurchaseOrderPutAwayConfirmed",
                QtyChange = poQty,
                Balance = balance,
                Detail = $"Inbound PO received — {poQty} units shelved."
            });

            foreach (var order in storeGroup)
            {
                var line = order.Lines.FirstOrDefault(l => l.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
                if (line is null) continue;
                int qty = (int)line.PickedAmount;
                balance -= qty;
                events.Add(new StockEventDto
                {
                    Id = eventId++,
                    Time = order.UpdatedAt.ToString("HH:mm"),
                    Dir = "out",
                    Ref = order.OrderNumber,
                    RefType = "Order",
                    Event = "PickConfirmed",
                    QtyChange = -qty,
                    Balance = balance,
                    Detail = $"{order.Customer} — {qty} units picked for {order.FulfillmentType}."
                });
            }

            locations.Add(new StockLocationDto
            {
                StoreId = storeGroup.Key.ToLowerInvariant().Replace(" ", "-"),
                StoreName = storeGroup.Key,
                Balance = Math.Max(0, balance),
                Events = events
            });
        }

        if (locations.Count == 0)
        {
            locations.Add(new StockLocationDto
            {
                StoreId = "store-central-dc",
                StoreName = "Central DC",
                Balance = 0,
                Events = []
            });
        }

        var unitPrice = Orders
            .SelectMany(o => o.Lines)
            .FirstOrDefault(l => l.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase))
            ?.UnitPrice ?? 0;

        return new StockLedgerDto
        {
            Sku = sku.ToUpperInvariant(),
            SkuName = Orders.SelectMany(o => o.Lines)
                .FirstOrDefault(l => l.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase))
                ?.ProductName ?? sku,
            UnitPrice = unitPrice,
            Currency = "THB",
            Locations = locations
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static T? Load<T>(string dataPath, string fileName)
    {
        var path = Path.Combine(dataPath, fileName);
        if (!File.Exists(path)) return default;
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), _json);
    }

    public string NextId(string prefix, IEnumerable<string> existing)
    {
        var max = existing
            .Select(id => id.Replace(prefix + "-", ""))
            .Where(s => int.TryParse(s, out _))
            .Select(int.Parse)
            .DefaultIfEmpty(0)
            .Max();
        return $"{prefix}-{max + 1:D3}";
    }
}
