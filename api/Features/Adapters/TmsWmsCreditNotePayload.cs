namespace OmsApi;

public static class TmsWmsCreditNotePayload
{
    public static object Build(OrderDto order, CreditNoteDto creditNote, InvoiceDto? invoice, List<ReturnItemDto> returnItems)
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

        var items = returnItems.Select((item, idx) => new
        {
            line_item_no = idx + 1,
            sku_code = item.Sku,
            pr_code = item.Barcode,
            is_weight_item = item.Uom == "KG",
            avg_weight = 0,
            unit_price = item.UnitPrice / 100m,
            return_quantity = item.Quantity,
            return_line_item_price = item.UnitPrice * item.Quantity / 100m,
            sale_unit = item.Uom
        }).ToList();

        return new
        {
            store_code = order.StoreId,
            member_id = order.ExternalCustomerId ?? order.Id,
            customer_segment = "STANDARD",
            sale_channel = saleChannel,
            salesource,
            order_id = order.OrderNumber,
            abb_id = creditNote.CreditNoteNumber,
            tax_invoice_id = invoice?.InvoiceNumber,
            reference_abb_id = invoice?.InvoiceNumber ?? creditNote.CreditNoteNumber,
            reference_order_datetime = order.OrderDate,
            transaction_status = "FULLY_CN",
            order_datetime = order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
            items,
            billing_address = (object?)null,
            created_at = DateTime.UtcNow,
            created_by = "OMS",
            updated_at = DateTime.UtcNow,
            updated_by = "OMS"
        };
    }
}
