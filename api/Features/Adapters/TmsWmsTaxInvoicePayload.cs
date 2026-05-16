namespace OmsApi;

public static class TmsWmsTaxInvoicePayload
{
    public static object Build(OrderDto order, InvoiceDto invoice, List<OrderLineDto> lines)
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

        return new
        {
            order_id = order.OrderNumber,
            sale_channel = saleChannel,
            sale_source = salesource,
            document_type = "INV",
            documents = new[]
            {
                new
                {
                    abb_id = invoice.InvoiceNumber,
                    tax_invoice_id = invoice.InvoiceNumber,
                    cn_abb_id = (string?)null,
                    cn_tax_id = (string?)null,
                    url = invoice.InvoiceLink ?? "",
                    is_success = invoice.Status == "Issued",
                    document_created_datetime = invoice.IssuedAt ?? invoice.GeneratedAt
                }
            }
        };
    }
}
