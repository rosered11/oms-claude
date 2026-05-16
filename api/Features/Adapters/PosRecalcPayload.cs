namespace OmsApi;

public static class PosRecalcPayload
{
    public static object Build(OrderDto order, List<OrderPromotionDto> promotions)
    {
        string saleChannel = order.ChannelType switch
        {
            "Gateway" => "CFW",
            "App" => "CHEF",
            "POS" => "CHO",
            _ => "CFW"
        };

        string salesource = order.SubChannel switch
        {
            "WA" => "WA",
            "XB" => "XB",
            "CF" => "CF",
            "CO" => "CO",
            _ => "WA"
        };

        var items = order.Lines.Select((line, idx) => new
        {
            SEQ = idx + 1,
            SK_CODE = line.Sku,
            QNT = line.RequestedAmount,
            WeightItemFlag = line.Uom == "KG",
            AvgWeight = (decimal?)null,
            QNTItem = (decimal?)null,
            itemUnit = line.Uom,
            AMT = line.UnitPrice * line.RequestedAmount / 100m,
            UPC = line.UnitPrice / 100m,
            CTLID = promotions
                .Where(p => p.OrderLineId == line.Id || p.SourcePromoId != null)
                .Select(p => p.SourcePromoId)
                .ToList(),
            PriceRequestUPC = (decimal?)null,
            DiscountCode = (string?)null,
            ExcludedBMGN = false,
            ReferenceSEQ = line.Id
        }).ToList();

        return new
        {
            Orderno = order.OrderNumber,
            StoreCode = order.StoreId,
            CustomerSegment = "STANDARD",
            CustomerCDP_ID = order.ExternalCustomerId ?? order.Id,
            SaleChannel = saleChannel,
            salesource,
            OrderItems = items,
            couponItem = Array.Empty<object>()
        };
    }
}
