using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmsApi;

public class OutboxRoutingRule
{
    public long RuleId { get; set; }
    public string ChannelType { get; set; } = "";
    public string SubChannel { get; set; } = "*";
    public string BusinessUnit { get; set; } = "";
    public string TriggerEvent { get; set; } = "";
    public string TargetSystem { get; set; } = "";
    public string EndpointKey { get; set; } = "";
    public int ExecutionOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class OutboxEndpointConfig
{
    public string EndpointKey { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();           // → Base URL
    public Dictionary<string, string> TokenRequestHeaders { get; set; } = new(); // → Token URL (OAuth2 only)
    public string AuthType { get; set; } = "None"; // None | StaticToken | OAuth2ClientCredentials
    public string? StaticToken { get; set; }
    public string StaticTokenHeader { get; set; } = "Authorization";
    public string? TokenUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Scope { get; set; }
    public string GrantType { get; set; } = "client_credentials";
    public Dictionary<string, string> AdditionalTokenParams { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class OutboxDispatchLog
{
    public long LogId { get; set; }
    public string OrderId { get; set; } = "";
    public string EndpointKey { get; set; } = "";
    public string TriggerEvent { get; set; } = "";
    public string TargetSystem { get; set; } = "";
    public string AuthType { get; set; } = "None";
    public string Status { get; set; } = "Pending"; // Pending | Success | Failed | Retrying
    // Phase 1 — Token Fetch (OAuth2 only)
    public string? TokenUrl { get; set; }
    public string? TokenRequestHeaders { get; set; }
    public string? TokenRequestBody { get; set; }
    public string? TokenResponsePayload { get; set; }
    // Phase 2 — API Call
    public string? BaseUrl { get; set; }
    public string? ApiRequestHeaders { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public int HttpStatusCode { get; set; }
    public int AttemptCount { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

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
    public List<OutboxRoutingRule> RoutingRules { get; private set; } = [];
    public List<OutboxEndpointConfig> EndpointConfigs { get; private set; } = [];
    public List<OutboxDispatchLog> DispatchLogs { get; private set; } = [];

    private long _nextDispatchLogId = 1;

    private readonly Dictionary<string, List<TimelineEventDto>> _orderEvents = [];
    private readonly Dictionary<string, List<SubstitutionDto>> _substitutions = [];
    private readonly Dictionary<string, List<WebhookLogDto>> _webhookLogs = [];
    private readonly Dictionary<string, List<GoodsReceiptDto>> _goodsReceipts = [];
    private readonly Dictionary<string, List<TransferConfirmationDto>> _transferConfirmations = [];
    private readonly List<DamagedReceiptDto> _damagedReceipts = [];
    private readonly Dictionary<string, RefundDto> _refunds = [];
    private readonly Dictionary<string, CreditNoteDto> _creditNotes = [];

    // order_holds
    private readonly Dictionary<string, List<OrderHoldDto>> _orderHolds = [];

    // payment module
    private readonly Dictionary<string, InvoiceDto> _invoices = [];
    private readonly Dictionary<string, OrderPaymentDto> _orderPayments = [];
    private readonly Dictionary<string, List<PaymentTransactionDto>> _paymentTransactions = [];
    private readonly Dictionary<string, List<OrderLineAmountDto>> _lineAmounts = [];
    private readonly Dictionary<string, List<OrderFeeDto>> _orderFees = [];
    private readonly Dictionary<string, List<OrderPromotionDto>> _orderPromotions = [];

    // return_put_away_logs
    private readonly Dictionary<string, List<ReturnPutAwayLogDto>> _returnPutAwayLogs = [];

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

        SeedRoutingRules();
        SeedEndpointConfigs();
        SeedPaymentData();
    }

    private void SeedRoutingRules()
    {
        long id = 1;
        void Add(string ch, string sub, string bu, string evt, string sys, string ep, int order) =>
            RoutingRules.Add(new OutboxRoutingRule
            {
                RuleId = id++, ChannelType = ch, SubChannel = sub, BusinessUnit = bu,
                TriggerEvent = evt, TargetSystem = sys, EndpointKey = ep,
                ExecutionOrder = order, IsActive = true
            });

        // TikTok (all BUs: CMG, CFR, …) — Marketplace-specific rules fire for any BU under TikTok
        Add("Marketplace", "TikTok", "*", "OrderCreatedEvent",    "WMS",         "wms.create-order",      1);
        Add("Marketplace", "TikTok", "*", "OrderCreatedEvent",    "Marketplace", "tiktok.order-create",   2);
        Add("Marketplace", "TikTok", "*", "PickConfirmedEvent",   "TMS",         "tms.pick-confirm",      1);
        Add("Marketplace", "TikTok", "*", "PickConfirmedEvent",   "Marketplace", "tiktok.pick-confirm",   2);
        Add("Marketplace", "TikTok", "*", "PackedEvent",          "TMS",         "tms.pack-confirm",      1);
        Add("Marketplace", "TikTok", "*", "OutForDeliveryEvent",  "Marketplace", "tiktok.awb-notify",     1);

        // Lazada (all BUs)
        Add("Marketplace", "Lazada", "*", "OrderCreatedEvent",    "WMS",         "wms.create-order",      1);
        Add("Marketplace", "Lazada", "*", "OrderCreatedEvent",    "Marketplace", "lazada.order-create",   2);
        Add("Marketplace", "Lazada", "*", "PickConfirmedEvent",   "TMS",         "tms.pick-confirm",      1);
        Add("Marketplace", "Lazada", "*", "PickConfirmedEvent",   "Marketplace", "lazada.pick-confirm",   2);
        Add("Marketplace", "Lazada", "*", "PackedEvent",          "TMS",         "tms.pack-confirm",      1);
        Add("Marketplace", "Lazada", "*", "PackedEvent",          "Marketplace", "lazada.pack-confirm",   2);

        // Wildcard fallback — all other channels / sub-channels / BUs
        Add("*", "*", "*", "OrderCreatedEvent",      "WMS",     "wms.create-order",    1);
        Add("*", "*", "*", "PickConfirmedEvent",     "TMS",     "tms.pick-confirm",    1);
        Add("*", "*", "*", "PackedEvent",            "TMS",     "tms.pack-confirm",    1);
        Add("*", "*", "*", "OutForDeliveryEvent",    "GW",      "gw.out-for-delivery", 1);
        Add("*", "*", "*", "DeliveredEvent",         "GW",      "gw.delivered",        1);
        Add("*", "*", "*", "ABBTaxInvoiceSentToTMS", "TMS",     "tms.abb-tax-invoice", 1);
        Add("*", "*", "*", "ABBInvoiceSentToGW",     "Gateway", "gateway.abb-invoice", 1);
        Add("*", "*", "*", "CreditNoteSentToGW",     "Gateway", "gateway.credit-note", 1);
        Add("*", "*", "*", "OrderCancelledEvent",    "WMS",     "wms.cancel-order",    1);
        Add("*", "*", "*", "OrderCancelledEvent",    "TMS",     "tms.cancel-booking",  2);
        Add("*", "*", "*", "OrderCancelledEvent",    "GW",      "gw.order-cancelled",  3);
        Add("*", "*", "*", "PosRecalculateEvent",    "POS",     "pos.recalculate",     1);
        Add("*", "*", "*", "ABBInvoiceSentToWMS",    "WMS",     "wms.tax-invoice",     1);
        Add("*", "*", "*", "CreditNoteSentToWMS",    "WMS",     "wms.credit-note",     1);
    }

    private void SeedEndpointConfigs()
    {
        static DateTime Utc() => DateTime.UtcNow;

        void Add(string key, string baseUrl, string authType,
            string? staticToken = null, string? tokenUrl = null,
            string? clientId = null, string? clientSecret = null,
            Dictionary<string, string>? headers = null) =>
            EndpointConfigs.Add(new OutboxEndpointConfig
            {
                EndpointKey = key,
                BaseUrl = baseUrl,
                AuthType = authType,
                StaticToken = staticToken,
                TokenUrl = tokenUrl,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Headers = headers ?? new(),
                IsActive = true,
                CreatedAt = Utc(),
                UpdatedAt = Utc()
            });

        Add("wms.create-order",    "https://wms.internal/api/orders",
            "OAuth2ClientCredentials", tokenUrl: "https://wms.internal/oauth/token",
            clientId: "oms-client", clientSecret: "***");
        Add("tms.pick-confirm",    "https://tms.internal/api/picks",
            "StaticToken", staticToken: "static-tms-token");
        Add("tms.pack-confirm",    "https://tms.internal/api/packs",
            "StaticToken", staticToken: "static-tms-token");
        Add("tiktok.order-create", "https://api.tiktokshop.com/orders",
            "OAuth2ClientCredentials", tokenUrl: "https://auth.tiktokshop.com/token",
            clientId: "oms-tiktok", clientSecret: "***");
        Add("lazada.order-create", "https://api.lazada.com/orders",
            "OAuth2ClientCredentials", tokenUrl: "https://auth.lazada.com/token",
            clientId: "oms-lazada", clientSecret: "***");
        Add("gw.out-for-delivery", "https://gw.internal/api/status-update",
            "StaticToken", staticToken: "static-gw-token");
        Add("gw.delivered",        "https://gw.internal/api/status-update",
            "StaticToken", staticToken: "static-gw-token");
        Add("tms.abb-tax-invoice", "https://tms.internal/api/invoices",
            "StaticToken", staticToken: "static-tms-token");
        Add("gateway.abb-invoice", "https://gw.internal/api/invoices",
            "StaticToken", staticToken: "static-gw-token");
        Add("gateway.credit-note", "https://gw.internal/api/credit-notes",
            "StaticToken", staticToken: "static-gw-token");
        Add("wms.cancel-order",    "https://wms.internal/api/orders/cancel",
            "OAuth2ClientCredentials", tokenUrl: "https://wms.internal/oauth/token",
            clientId: "oms-client", clientSecret: "***");
        Add("tms.cancel-booking",  "https://tms.internal/api/bookings/cancel",
            "StaticToken", staticToken: "static-tms-token");
        Add("gw.order-cancelled",  "https://gw.internal/api/orders/cancel",
            "StaticToken", staticToken: "static-gw-token");
        Add("pos.recalculate",     "https://pos.internal/api/recalculate",
            "StaticToken", staticToken: "static-pos-token",
            headers: new() { ["accessToken"] = "pos-access-token", ["refId"] = "" });
        Add("wms.tax-invoice",     "https://wms.internal/api/invoices",
            "StaticToken", staticToken: "static-wms-token");
        Add("wms.credit-note",     "https://wms.internal/api/credit-notes",
            "StaticToken", staticToken: "static-wms-token");
        Add("tiktok.pick-confirm", "https://api.tiktokshop.com/picks",
            "OAuth2ClientCredentials", tokenUrl: "https://auth.tiktokshop.com/token",
            clientId: "oms-tiktok", clientSecret: "***");
        Add("lazada.pick-confirm", "https://api.lazada.com/picks",
            "OAuth2ClientCredentials", tokenUrl: "https://auth.lazada.com/token",
            clientId: "oms-lazada", clientSecret: "***");
        Add("lazada.pack-confirm", "https://api.lazada.com/packs",
            "OAuth2ClientCredentials", tokenUrl: "https://auth.lazada.com/token",
            clientId: "oms-lazada", clientSecret: "***");
        Add("tiktok.awb-notify",   "https://api.tiktokshop.com/awb",
            "OAuth2ClientCredentials", tokenUrl: "https://auth.tiktokshop.com/token",
            clientId: "oms-tiktok", clientSecret: "***");
    }

    private void SeedPaymentData()
    {
        static DateTime Utc(int y, int mo, int d, int h, int mi, int s) =>
            new(y, mo, d, h, mi, s, DateTimeKind.Utc);

        // ORD-001 — 10% fresh produce coupon at order creation
        AddOrderPromotion(new OrderPromotionDto
        {
            PromotionId = "promo-001-1", OrderId = "ORD-001",
            SourcePromoId = "P-FRESH10", PromoCode = "FRESH10",
            PromoName = "10% Fresh Produce Discount", PromoType = "PercentageDiscount",
            DiscountAmount = 24500, DiscountPercentage = 0.10m,
            Currency = "THB", CreatedAt = Utc(2024, 1, 15, 14, 2, 0)
        });

        // ORD-003 — 20% member discount + delivery fee
        AddOrderPromotion(new OrderPromotionDto
        {
            PromotionId = "promo-003-1", OrderId = "ORD-003",
            SourcePromoId = "P-MEMBER20", PromoCode = "MEMBER20",
            PromoName = "20% Member Discount", PromoType = "PercentageDiscount",
            DiscountAmount = 64000, DiscountPercentage = 0.20m,
            Currency = "THB", CreatedAt = Utc(2024, 1, 15, 10, 30, 0)
        });
        AddOrderFee(new OrderFeeDto
        {
            FeeId = "fee-003-1", OrderId = "ORD-003", FeeCode = "DELIVERY_FEE",
            FeeName = "Delivery Fee", FeeType = "Delivery", Amount = 4900,
            Currency = "THB", CreatedAt = Utc(2024, 1, 15, 10, 30, 0),
            UpdatedAt = Utc(2024, 1, 15, 10, 30, 0)
        });

        // ORD-004 — delivery fee (PayOnDelivery)
        AddOrderFee(new OrderFeeDto
        {
            FeeId = "fee-004-1", OrderId = "ORD-004", FeeCode = "DELIVERY_FEE",
            FeeName = "Delivery Fee", FeeType = "Delivery", Amount = 4900,
            Currency = "THB", CreatedAt = Utc(2024, 1, 15, 9, 0, 0),
            UpdatedAt = Utc(2024, 1, 15, 9, 0, 0)
        });

        // ORD-005 — CreditCard auth + capture
        SetOrderPayment("ORD-005", new OrderPaymentDto
        {
            PaymentId = "pay-005", OrderId = "ORD-005",
            PaymentMethod = "CreditCard", TotalAmount = 75000, Currency = "THB",
            Status = "Captured",
            CreatedAt = Utc(2024, 1, 14, 16, 2, 0), UpdatedAt = Utc(2024, 1, 14, 19, 31, 0)
        });
        AddPaymentTransaction(new PaymentTransactionDto
        {
            TransactionId = "txn-005-1", PaymentId = "pay-005",
            Amount = 75000, Currency = "THB", PaymentMethod = "CreditCard",
            GatewayRef = "GW-AUTH-2024-005", CreatedAt = Utc(2024, 1, 14, 16, 2, 0)
        });
        AddPaymentTransaction(new PaymentTransactionDto
        {
            TransactionId = "txn-005-2", PaymentId = "pay-005",
            Amount = 75000, Currency = "THB", PaymentMethod = "CreditCard",
            GatewayRef = "GW-CAP-2024-005", CreatedAt = Utc(2024, 1, 14, 19, 31, 0)
        });

        // ORD-009 — promo + delivery fee (PickConfirmed, recalc pending)
        AddOrderPromotion(new OrderPromotionDto
        {
            PromotionId = "promo-009-1", OrderId = "ORD-009",
            SourcePromoId = "P-SEA15", PromoCode = "SEAFOOD15",
            PromoName = "15% Seafood Promotion", PromoType = "PercentageDiscount",
            DiscountAmount = 46500, DiscountPercentage = 0.15m,
            Currency = "THB", CreatedAt = Utc(2024, 1, 15, 13, 0, 0)
        });
        AddOrderFee(new OrderFeeDto
        {
            FeeId = "fee-009-1", OrderId = "ORD-009", FeeCode = "DELIVERY_FEE",
            FeeName = "Delivery Fee", FeeType = "Delivery", Amount = 4900,
            Currency = "THB", CreatedAt = Utc(2024, 1, 15, 13, 0, 0),
            UpdatedAt = Utc(2024, 1, 15, 13, 0, 0)
        });

        // ORD-010 — BankTransfer capture + SUMMER15 coupon (Paid)
        SetOrderPayment("ORD-010", new OrderPaymentDto
        {
            PaymentId = "pay-010", OrderId = "ORD-010",
            PaymentMethod = "BankTransfer", TotalAmount = 140000, Currency = "THB",
            Status = "Captured",
            CreatedAt = Utc(2024, 1, 14, 11, 0, 0), UpdatedAt = Utc(2024, 1, 14, 13, 5, 0)
        });
        AddPaymentTransaction(new PaymentTransactionDto
        {
            TransactionId = "txn-010-1", PaymentId = "pay-010",
            Amount = 140000, Currency = "THB", PaymentMethod = "BankTransfer",
            GatewayRef = "GW-CAP-2024-010", CreatedAt = Utc(2024, 1, 14, 13, 5, 0)
        });
        AddOrderPromotion(new OrderPromotionDto
        {
            PromotionId = "promo-010-1", OrderId = "ORD-010",
            SourcePromoId = "P-SUMMER15", PromoCode = "SUMMER15",
            PromoName = "Summer Campaign 15% Off", PromoType = "PercentageDiscount",
            DiscountAmount = 21000, DiscountPercentage = 0.15m,
            Currency = "THB", CreatedAt = Utc(2024, 1, 14, 11, 0, 0)
        });

        // ORD-014 — CreditCard capture + SNACK10 coupon + delivery fee (Paid)
        SetOrderPayment("ORD-014", new OrderPaymentDto
        {
            PaymentId = "pay-014", OrderId = "ORD-014",
            PaymentMethod = "CreditCard", TotalAmount = 210000, Currency = "THB",
            Status = "Captured",
            CreatedAt = Utc(2024, 1, 14, 9, 5, 0), UpdatedAt = Utc(2024, 1, 14, 17, 0, 0)
        });
        AddPaymentTransaction(new PaymentTransactionDto
        {
            TransactionId = "txn-014-1", PaymentId = "pay-014",
            Amount = 210000, Currency = "THB", PaymentMethod = "CreditCard",
            GatewayRef = "GW-CAP-2024-014", CreatedAt = Utc(2024, 1, 14, 9, 5, 0)
        });
        AddOrderPromotion(new OrderPromotionDto
        {
            PromotionId = "promo-014-1", OrderId = "ORD-014",
            SourcePromoId = "P-SNACK10", PromoCode = "SNACK10",
            PromoName = "Snack Party 10% Off", PromoType = "PercentageDiscount",
            DiscountAmount = 23333, DiscountPercentage = 0.10m,
            Currency = "THB", CreatedAt = Utc(2024, 1, 14, 9, 0, 0)
        });
        AddOrderFee(new OrderFeeDto
        {
            FeeId = "fee-014-1", OrderId = "ORD-014", FeeCode = "DELIVERY_FEE",
            FeeName = "Delivery Fee", FeeType = "Delivery", Amount = 4900,
            Currency = "THB", CreatedAt = Utc(2024, 1, 14, 9, 0, 0),
            UpdatedAt = Utc(2024, 1, 14, 9, 0, 0)
        });
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

    // ── Order Holds ───────────────────────────────────────────────────────────

    public List<OrderHoldDto> GetOrderHolds(string orderId)
    {
        _orderHolds.TryGetValue(orderId, out var list);
        return list ?? [];
    }

    public OrderHoldDto AddOrderHold(OrderHoldDto hold)
    {
        if (!_orderHolds.TryGetValue(hold.OrderId, out var list))
        {
            list = [];
            _orderHolds[hold.OrderId] = list;
        }
        list.Add(hold);
        return hold;
    }

    public OrderHoldDto? GetActiveHold(string orderId) =>
        GetOrderHolds(orderId).LastOrDefault(h => h.ReleasedAt is null);

    // ── Substitutions ─────────────────────────────────────────────────────────

    public List<SubstitutionDto> GetSubstitutions(string orderId)
    {
        _substitutions.TryGetValue(orderId, out var list);
        return list ?? [];
    }

    public SubstitutionDto? FindSubstitution(string orderId, string subId) =>
        GetSubstitutions(orderId).FirstOrDefault(s => s.SubstitutionId.Equals(subId, StringComparison.OrdinalIgnoreCase));

    public SubstitutionDto AddSubstitution(SubstitutionDto sub)
    {
        if (!_substitutions.TryGetValue(sub.OrderId, out var list))
        {
            list = [];
            _substitutions[sub.OrderId] = list;
        }
        list.Add(sub);
        return sub;
    }

    // ── Webhook Logs ──────────────────────────────────────────────────────────

    public List<WebhookLogDto> GetWebhookLogs(string orderId)
    {
        _webhookLogs.TryGetValue(orderId, out var list);
        return list ?? [];
    }

    public void AddWebhookLog(string orderId, WebhookLogDto log)
    {
        if (!_webhookLogs.TryGetValue(orderId, out var list))
        {
            list = [];
            _webhookLogs[orderId] = list;
        }
        list.Add(log);
    }

    // ── Returns ───────────────────────────────────────────────────────────────

    public ReturnDto? FindReturn(string id) =>
        Returns.FirstOrDefault(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public void AddReturn(ReturnDto ret) => Returns.Add(ret);

    public RefundDto? GetRefund(string returnId)
    {
        _refunds.TryGetValue(returnId, out var r);
        return r;
    }

    public CreditNoteDto? GetCreditNote(string returnId)
    {
        _creditNotes.TryGetValue(returnId, out var cn);
        return cn;
    }

    public void SetRefund(string returnId, RefundDto refund) => _refunds[returnId] = refund;
    public void SetCreditNote(string returnId, CreditNoteDto cn) => _creditNotes[returnId] = cn;

    // ── Return Put-Away Logs ──────────────────────────────────────────────────

    public List<ReturnPutAwayLogDto> GetReturnPutAwayLogs(string returnId)
    {
        _returnPutAwayLogs.TryGetValue(returnId, out var list);
        return list ?? [];
    }

    public void AddReturnPutAwayLog(ReturnPutAwayLogDto log)
    {
        if (!_returnPutAwayLogs.TryGetValue(log.ReturnId, out var list))
        {
            list = [];
            _returnPutAwayLogs[log.ReturnId] = list;
        }
        list.Add(log);
    }

    // ── Invoices ──────────────────────────────────────────────────────────────

    public InvoiceDto? GetInvoice(string orderId)
    {
        _invoices.TryGetValue(orderId, out var inv);
        return inv;
    }

    public void SetInvoice(string orderId, InvoiceDto invoice) => _invoices[orderId] = invoice;

    // ── Order Payments ────────────────────────────────────────────────────────

    public OrderPaymentDto? GetOrderPayment(string orderId)
    {
        _orderPayments.TryGetValue(orderId, out var p);
        return p;
    }

    public void SetOrderPayment(string orderId, OrderPaymentDto payment) => _orderPayments[orderId] = payment;

    public List<PaymentTransactionDto> GetPaymentTransactions(string paymentId)
    {
        _paymentTransactions.TryGetValue(paymentId, out var list);
        return list ?? [];
    }

    public void AddPaymentTransaction(PaymentTransactionDto txn)
    {
        if (!_paymentTransactions.TryGetValue(txn.PaymentId, out var list))
        {
            list = [];
            _paymentTransactions[txn.PaymentId] = list;
        }
        list.Add(txn);
    }

    // ── Line Amounts ──────────────────────────────────────────────────────────

    public List<OrderLineAmountDto> GetLineAmounts(string orderLineId)
    {
        _lineAmounts.TryGetValue(orderLineId, out var list);
        return list ?? [];
    }

    public void AddLineAmount(string orderLineId, OrderLineAmountDto amount)
    {
        if (!_lineAmounts.TryGetValue(orderLineId, out var list))
        {
            list = [];
            _lineAmounts[orderLineId] = list;
        }
        list.Add(amount);
    }

    // ── Order Fees ────────────────────────────────────────────────────────────

    public List<OrderFeeDto> GetOrderFees(string orderId)
    {
        _orderFees.TryGetValue(orderId, out var list);
        return list ?? [];
    }

    public void AddOrderFee(OrderFeeDto fee)
    {
        if (!_orderFees.TryGetValue(fee.OrderId, out var list))
        {
            list = [];
            _orderFees[fee.OrderId] = list;
        }
        list.Add(fee);
    }

    // ── Order Promotions ──────────────────────────────────────────────────────

    public List<OrderPromotionDto> GetOrderPromotions(string orderId)
    {
        _orderPromotions.TryGetValue(orderId, out var list);
        return list ?? [];
    }

    public void AddOrderPromotion(OrderPromotionDto promo)
    {
        if (!_orderPromotions.TryGetValue(promo.OrderId, out var list))
        {
            list = [];
            _orderPromotions[promo.OrderId] = list;
        }
        list.Add(promo);
    }

    // ── Inbound ───────────────────────────────────────────────────────────────

    public PurchaseOrderDto? FindPO(string id) =>
        PurchaseOrders.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public TransferOrderDto? FindTO(string id) =>
        TransferOrders.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public void AddPO(PurchaseOrderDto po) => PurchaseOrders.Add(po);
    public void AddTO(TransferOrderDto to) => TransferOrders.Add(to);

    public List<GoodsReceiptDto> GetGoodsReceipts(string poId)
    {
        _goodsReceipts.TryGetValue(poId, out var list);
        return list ?? [];
    }

    public void AddGoodsReceipt(string poId, GoodsReceiptDto gr)
    {
        if (!_goodsReceipts.TryGetValue(poId, out var list))
        {
            list = [];
            _goodsReceipts[poId] = list;
        }
        list.Add(gr);
    }

    public List<TransferConfirmationDto> GetTransferConfirmations(string toId)
    {
        _transferConfirmations.TryGetValue(toId, out var list);
        return list ?? [];
    }

    public void AddTransferConfirmation(string toId, TransferConfirmationDto conf)
    {
        if (!_transferConfirmations.TryGetValue(toId, out var list))
        {
            list = [];
            _transferConfirmations[toId] = list;
        }
        list.Add(conf);
    }

    public DamagedReceiptDto? FindDamagedReceipt(string id) =>
        _damagedReceipts.FirstOrDefault(d => d.DamagedReceiptId.Equals(id, StringComparison.OrdinalIgnoreCase));

    public DamagedReceiptDto AddDamagedReceipt(DamagedReceiptDto dr)
    {
        _damagedReceipts.Add(dr);
        return dr;
    }

    public string NextDamagedReceiptId() =>
        NextId("DMG", _damagedReceipts.Select(d => d.DamagedReceiptId));

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

    // ── Endpoint Configs ──────────────────────────────────────────────────────

    public OutboxEndpointConfig? GetEndpointConfig(string key) =>
        EndpointConfigs.FirstOrDefault(c => c.EndpointKey.Equals(key, StringComparison.OrdinalIgnoreCase));

    public void UpsertEndpointConfig(OutboxEndpointConfig config)
    {
        var existing = GetEndpointConfig(config.EndpointKey);
        if (existing is null)
        {
            EndpointConfigs.Add(config);
        }
        else
        {
            existing.BaseUrl = config.BaseUrl;
            existing.Headers = config.Headers;
            existing.AuthType = config.AuthType;
            existing.StaticToken = config.StaticToken;
            existing.TokenUrl = config.TokenUrl;
            existing.ClientId = config.ClientId;
            existing.ClientSecret = config.ClientSecret;
            existing.Scope = config.Scope;
            existing.IsActive = config.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    public bool DeleteEndpointConfig(string key)
    {
        var existing = GetEndpointConfig(key);
        if (existing is null) return false;
        existing.IsActive = false;
        existing.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    // ── Dispatch Logs ─────────────────────────────────────────────────────────

    public long NextDispatchLogId() => _nextDispatchLogId++;

    public void AddDispatchLog(OutboxDispatchLog log) => DispatchLogs.Add(log);

    public OutboxDispatchLog? GetDispatchLog(long logId) =>
        DispatchLogs.FirstOrDefault(l => l.LogId == logId);

    public IEnumerable<OutboxDispatchLog> GetDispatchLogs(string? orderId = null) =>
        orderId is null
            ? DispatchLogs
            : DispatchLogs.Where(l => l.OrderId.Equals(orderId, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<OutboxDispatchLog> GetFailedDispatchLogs() =>
        DispatchLogs.Where(l => l.Status == "Failed");

    // ── Routing Rules ─────────────────────────────────────────────────────────

    public IEnumerable<OutboxRoutingRule> GetRoutingRules(
        string channelType, string subChannel, string businessUnit, string triggerEvent)
    {
        bool Eq(string ruleVal, string orderVal) =>
            ruleVal == "*" || ruleVal.Equals(orderVal, StringComparison.OrdinalIgnoreCase);

        IList<OutboxRoutingRule> Bucket(bool chWild, bool subWild, bool buWild) =>
            RoutingRules
                .Where(r => r.IsActive && r.TriggerEvent == triggerEvent
                    && (r.ChannelType  == "*") == chWild  && Eq(r.ChannelType,  channelType)
                    && (r.SubChannel   == "*") == subWild && Eq(r.SubChannel,   subChannel)
                    && (r.BusinessUnit == "*") == buWild  && Eq(r.BusinessUnit, businessUnit))
                .OrderBy(r => r.ExecutionOrder)
                .ToList();

        // Most-specific bucket that has at least one rule wins; stops at first non-empty bucket
        IList<OutboxRoutingRule>[] buckets =
        [
            Bucket(false, false, false),  // exact: ch + sub + bu
            Bucket(false, false, true),   // ch + sub, any bu
            Bucket(false, true,  false),  // ch + bu,  any sub
            Bucket(false, true,  true),   // ch only
            Bucket(true,  true,  true),   // full wildcard
        ];
        return buckets.FirstOrDefault(b => b.Count > 0) ?? [];
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
