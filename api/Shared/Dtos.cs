namespace OmsApi;

// ── Orders ────────────────────────────────────────────────────────────────────

public class OrderDto
{
    public string Id { get; set; } = "";
    public string OrderNumber { get; set; } = "";
    public string SourceOrderId { get; set; } = "";
    public string Customer { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string? CustomerEmail { get; set; }
    public string? ExternalCustomerId { get; set; }
    public string ChannelType { get; set; } = "";
    public string SubChannel { get; set; } = "*";
    public string BusinessUnit { get; set; } = "";
    public string StoreId { get; set; } = "";
    public string FulfillmentType { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string Status { get; set; } = "";
    public string? PreHoldStatus { get; set; }
    public string? HoldReason { get; set; }
    public bool SubstitutionFlag { get; set; }
    public bool IsPrepaid { get; set; }
    public string Type { get; set; } = "";
    public int Items { get; set; }
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public string UpdatedBy { get; set; } = "";
    public DeliverySlotDto? DeliverySlot { get; set; }
    public List<OrderAddressDto> Addresses { get; set; } = [];
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OrderPackageDto
{
    public string Id { get; set; } = "";
    public string TrackingId { get; set; } = "";
    public string VehicleType { get; set; } = "";
    public decimal Weight { get; set; }
    public string Status { get; set; } = "";
    public List<string> LineIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OrderAddressDto
{
    public string AddressId { get; set; } = "";
    public string AddressType { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string MobilePhone { get; set; } = "";
    public string? Email { get; set; }
    public string Address1 { get; set; } = "";
    public string Subdistrict { get; set; } = "";
    public string District { get; set; } = "";
    public string Province { get; set; } = "";
    public string PostalCode { get; set; } = "";
}

public class DeliverySlotDto
{
    public string SlotId { get; set; } = "";
    public string StoreId { get; set; } = "";
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public string? BookedVia { get; set; }
    public string? BookingRef { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SlotHistoryEntryDto> History { get; set; } = [];
}

public class SlotHistoryEntryDto
{
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public string? BookedVia { get; set; }
    public string? BookingRef { get; set; }
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class OrderHoldDto
{
    public string HoldId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string HoldReason { get; set; } = "";
    public DateTime HeldAt { get; set; }
    public string HeldBy { get; set; } = "";
    public DateTime? ReleasedAt { get; set; }
    public string? ReleasedBy { get; set; }
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
    public DateTime? InspectedAt { get; set; }
    public DateTime? PutAwayAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ReturnPutAwayLogDto
{
    public string LogId { get; set; } = "";
    public string ReturnId { get; set; } = "";
    public string ReturnItemId { get; set; } = "";
    public string Sku { get; set; } = "";
    public string AssignedSloc { get; set; } = "";
    public string Condition { get; set; } = "";
    public decimal Quantity { get; set; }
    public string PerformedBy { get; set; } = "";
    public DateTime PerformedAt { get; set; }
}

// ── Payment ───────────────────────────────────────────────────────────────────

public class OrderPaymentDto
{
    public string PaymentId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "THB";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PaymentTransactionDto
{
    public string TransactionId { get; set; } = "";
    public string PaymentId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "THB";
    public string PaymentMethod { get; set; } = "";
    public string? GatewayRef { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InvoiceDto
{
    public string InvoiceId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string InvoiceNumber { get; set; } = "";
    public string InvoiceType { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "THB";
    public string Status { get; set; } = "";
    public string? InvoiceLink { get; set; }
    public string? SourceStsRef { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
}

public class OrderLineAmountDto
{
    public string AmountId { get; set; } = "";
    public string OrderLineId { get; set; } = "";
    public int RecalcRound { get; set; }
    public string TriggerEvent { get; set; } = "";
    public decimal OriginalUnitPrice { get; set; }
    public decimal RecalculatedUnitPrice { get; set; }
    public decimal UnitNetAmount { get; set; }
    public DateTime RecalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderLineTaxDto> Taxes { get; set; } = [];
}

public class OrderLineTaxDto
{
    public string TaxId { get; set; } = "";
    public string AmountId { get; set; } = "";
    public string TaxType { get; set; } = "";
    public string TaxDescription { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Rate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderFeeDto
{
    public string FeeId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string? SourceFeeId { get; set; }
    public string FeeCode { get; set; } = "";
    public string FeeName { get; set; } = "";
    public string FeeType { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "THB";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OrderPromotionDto
{
    public string PromotionId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string? OrderLineId { get; set; }
    public string? SourcePromoId { get; set; }
    public string PromoCode { get; set; } = "";
    public string PromoName { get; set; } = "";
    public string PromoType { get; set; } = "";
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string Currency { get; set; } = "THB";
    public DateTime CreatedAt { get; set; }
}

// ── Refund / Credit Note ──────────────────────────────────────────────────────

public class RefundDto
{
    public string RefundId { get; set; } = "";
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = "THB";
    public string RefundMethod { get; set; } = "";
    public string Status { get; set; } = "";
    public string ReferenceNo { get; set; } = "";
    public DateTime ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreditNoteDto
{
    public string CreditNoteId { get; set; } = "";
    public string CreditNoteNumber { get; set; } = "";
    public string InvoiceId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "THB";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "";
    public string? CreditNoteLink { get; set; }
    public string? SourceStsRef { get; set; }
    public DateTime? IssuedAt { get; set; }
}

// ── Inbound ───────────────────────────────────────────────────────────────────

public class PurchaseOrderDto
{
    public string Id { get; set; } = "";
    public string PoNumber { get; set; } = "";
    public string SupplierId { get; set; } = "";
    public string Supplier { get; set; } = "";
    public string StoreId { get; set; } = "";
    public string Store { get; set; } = "";
    public string Status { get; set; } = "";
    public string? GoodsReceiveNo { get; set; }
    public string? UpdatedBy { get; set; }
    public decimal Value { get; set; }
    public int LineCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PurchaseOrderLineDto> Lines { get; set; } = [];
}

public class PurchaseOrderLineDto
{
    public string PoLineId { get; set; } = "";
    public string PurchaseOrderId { get; set; } = "";
    public string Sku { get; set; } = "";
    public int OrderedQty { get; set; }
    public int ReceivedQty { get; set; }
    public decimal UnitCost { get; set; }
    public string Currency { get; set; } = "THB";
    public string Condition { get; set; } = "Resellable";
    public string? Sloc { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? PutAwayAt { get; set; }
}

public class TransferOrderDto
{
    public string Id { get; set; } = "";
    public string TransferNumber { get; set; } = "";
    public string SourceStoreId { get; set; } = "";
    public string Source { get; set; } = "";
    public string DestStoreId { get; set; } = "";
    public string Dest { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Tracking { get; set; }
    public string? UpdatedBy { get; set; }
    public int LineCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<TransferOrderLineDto> Lines { get; set; } = [];
}

public class TransferOrderLineDto
{
    public string ToLineId { get; set; } = "";
    public string TransferOrderId { get; set; } = "";
    public string Sku { get; set; } = "";
    public int RequestedQty { get; set; }
    public int TransferredQty { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

// ── Substitutions ─────────────────────────────────────────────────────────────

public class SubstitutionDto
{
    public string SubstitutionId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string OrderLineId { get; set; } = "";
    public string OriginalSku { get; set; } = "";
    public string OriginalProductName { get; set; } = "";
    public string SubstituteSku { get; set; } = "";
    public string SubstituteProductName { get; set; } = "";
    public decimal SubstituteUnitPrice { get; set; }
    public decimal SubstitutedAmount { get; set; }
    public bool? CustomerApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Webhook Logs ──────────────────────────────────────────────────────────────

public class WebhookLogDto
{
    public string WebhookLogId { get; set; } = "";
    public string SourceSystem { get; set; } = "";
    public string EventType { get; set; } = "";
    public string Detail { get; set; } = "";
    public string? IdempotencyKey { get; set; }
    public string? RawPayload { get; set; }
    public DateTime ReceivedAt { get; set; }
}

// ── Goods Receipts ─────────────────────────────────────────────────────────────

public class GoodsReceiptLineDto
{
    public string Sku { get; set; } = "";
    public decimal ReceivedQty { get; set; }
    public string Condition { get; set; } = "";
    public string Sloc { get; set; } = "";
}

public class GoodsReceiptDto
{
    public string GoodsReceiveNo { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime ReceivedAt { get; set; }
    public DateTime? PutAwayAt { get; set; }
    public List<GoodsReceiptLineDto> Lines { get; set; } = [];
}

// ── Transfer Confirmations ─────────────────────────────────────────────────────

public class TransferConfirmationDto
{
    public string Type { get; set; } = "";
    public DateTime ConfirmedAt { get; set; }
    public string ConfirmedBy { get; set; } = "";
    public string? Tracking { get; set; }
}

// ── Damaged Goods ─────────────────────────────────────────────────────────────

public class DamagedReceiptDto
{
    public string DamagedReceiptId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string TrackingId { get; set; } = "";
    public string Status { get; set; } = "Received";
    public DateTime ReceivedAt { get; set; }
    public DateTime? PutAwayAt { get; set; }
    public string? UpdatedBy { get; set; }
    public List<DamagedGoodsItemDto> Items { get; set; } = [];
}

public class DamagedGoodsItemDto
{
    public string ItemId { get; set; } = "";
    public string DamagedReceiptId { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Condition { get; set; } = "";
    public string? Sloc { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

// ── Outbox ────────────────────────────────────────────────────────────────────

public class OutboxEndpointConfigDto
{
    public string EndpointKey { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> TokenRequestHeaders { get; set; } = new();
    public string AuthType { get; set; } = "None";
    public string? StaticToken { get; set; }
    public string StaticTokenHeader { get; set; } = "Authorization";
    public string? TokenUrl { get; set; }
    public string? ClientId { get; set; }
    public string? Scope { get; set; }
    public string GrantType { get; set; } = "client_credentials";
    public Dictionary<string, string> AdditionalTokenParams { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OutboxDispatchLogDto
{
    public long LogId { get; set; }
    public string OrderId { get; set; } = "";
    public string EndpointKey { get; set; } = "";
    public string TriggerEvent { get; set; } = "";
    public string TargetSystem { get; set; } = "";
    public string AuthType { get; set; } = "None";
    public string Status { get; set; } = "";
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
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
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
