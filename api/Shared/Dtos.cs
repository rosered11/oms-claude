namespace OmsApi;

// ── Orders ────────────────────────────────────────────────────────────────────

public class OrderDto
{
    public string Id { get; set; } = "";
    public string OrderNumber { get; set; } = "";
    public string Customer { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string? CustomerEmail { get; set; }
    public string ChannelType { get; set; } = "";
    public string BusinessUnit { get; set; } = "";
    public string StoreId { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Status { get; set; } = "";
    public string? PreHoldStatus { get; set; }
    public string? HoldReason { get; set; }
    public string Type { get; set; } = "";
    public string FulfillmentType { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public bool SubstitutionFlag { get; set; }
    public bool PosRecalcPending { get; set; }
    public int Items { get; set; }
    public decimal Amount { get; set; }
    public string CreatedBy { get; set; } = "";
    public string UpdatedBy { get; set; } = "";
    public DeliverySlotDto? DeliverySlot { get; set; }
    public List<OrderLineDto> Lines { get; set; } = [];
    public List<OrderPackageDto> Packages { get; set; } = [];
}

public class OrderLineDto
{
    public string Id { get; set; } = "";
    public string Sku { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Barcode { get; set; } = "";
    public decimal RequestedAmount { get; set; }
    public decimal PickedAmount { get; set; }
    public string Uom { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "THB";
    public string Status { get; set; } = "Active";
    public bool IsSubstitute { get; set; }
}

public class OrderPackageDto
{
    public string Id { get; set; } = "";
    public string TrackingId { get; set; } = "";
    public string VehicleType { get; set; } = "";
    public string Status { get; set; } = "";
    public double Weight { get; set; }
    public List<string> LineIds { get; set; } = [];
}

public class DeliverySlotDto
{
    public string SlotId { get; set; } = "";
    public string StoreId { get; set; } = "";
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
}

// ── Timeline ──────────────────────────────────────────────────────────────────

public class TimelineEventDto
{
    public int Id { get; set; }
    public string Time { get; set; } = "";
    public string Phase { get; set; } = "";
    public string Type { get; set; } = "";
    public string System { get; set; } = "";
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string? OutStatus { get; set; }
    public DateTime OccurredAt { get; set; }
}

// ── Returns ───────────────────────────────────────────────────────────────────

public class ReturnDto
{
    public string Id { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string ReturnOrderNumber { get; set; } = "";
    public string? InvoiceId { get; set; }
    public string? CreditNoteId { get; set; }
    public string Status { get; set; } = "";
    public string? GoodsReceiveNo { get; set; }
    public string ReturnReason { get; set; } = "";
    public DateTime RequestedAt { get; set; }
    public DateTime? PickupScheduledAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? InspectedAt { get; set; }
    public DateTime? PutAwayAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public string UpdatedBy { get; set; } = "";
    public List<ReturnItemDto> Items { get; set; } = [];
}

public class ReturnItemDto
{
    public string Id { get; set; } = "";
    public string OrderLineId { get; set; } = "";
    public string Sku { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Barcode { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Uom { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "THB";
    public string ItemReason { get; set; } = "";
    public string? Condition { get; set; }
    public string PutAwayStatus { get; set; } = "Pending";
    public string? AssignedSloc { get; set; }
    public string PaymentMethod { get; set; } = "";
}

// ── Inbound ───────────────────────────────────────────────────────────────────

public class PurchaseOrderDto
{
    public string Id { get; set; } = "";
    public string Supplier { get; set; } = "";
    public int Lines { get; set; }
    public string Status { get; set; } = "";
    public string Store { get; set; } = "";
    public decimal Value { get; set; }
    public string? GoodsReceiveNo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TransferOrderDto
{
    public string Id { get; set; } = "";
    public string Source { get; set; } = "";
    public string Dest { get; set; } = "";
    public int Lines { get; set; }
    public string Status { get; set; } = "";
    public string? Tracking { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── Stock ─────────────────────────────────────────────────────────────────────

public class StockLedgerDto
{
    public string Sku { get; set; } = "";
    public string SkuName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "THB";
    public List<StockLocationDto> Locations { get; set; } = [];
}

public class StockLocationDto
{
    public string StoreId { get; set; } = "";
    public string StoreName { get; set; } = "";
    public int Balance { get; set; }
    public List<StockEventDto> Events { get; set; } = [];
}

public class StockEventDto
{
    public int Id { get; set; }
    public string Time { get; set; } = "";
    public string Dir { get; set; } = "";
    public string Ref { get; set; } = "";
    public string RefType { get; set; } = "";
    public string Event { get; set; } = "";
    public int QtyChange { get; set; }
    public int Balance { get; set; }
    public string Detail { get; set; } = "";
}
