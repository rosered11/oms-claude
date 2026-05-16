namespace OmsApi;

public static class GwUpdateStatusPayload
{
    public static object Build(OrderDto order, OrderPaymentDto? payment)
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

        object[]? payments = null;
        if (payment is not null)
        {
            string paymentMethod = payment.PaymentMethod switch
            {
                "CreditCard" => "CREDIT_CARD",
                "QRCode" => "QR_CODE",
                "PayOnDelivery" => "POD",
                _ => "POD"
            };

            string tendor = payment.PaymentMethod switch
            {
                "CreditCard" => "WCRD",
                "QRCode" => "QRPP",
                "PayOnDelivery" => "WCOD",
                _ => "WCOD"
            };

            payments =
            [
                new
                {
                    payment_type = order.IsPrepaid ? "PRE_PAID" : "POST_PAID",
                    payment_method = paymentMethod,
                    payment_jd = payment.PaymentMethod,
                    payment_amount = payment.TotalAmount / 100m,
                    tendor,
                    payment_datetime = payment.CreatedAt,
                    payment_status = payment.Status == "Captured" ? "PAID" : "UNPAID",
                    paid_at = payment.UpdatedAt,
                    created_at = payment.CreatedAt,
                    created_by = "OMS",
                    updated_at = payment.UpdatedAt,
                    updated_by = "OMS"
                }
            ];
        }

        return new
        {
            order_id = order.OrderNumber,
            sale_channel = saleChannel,
            sale_source = salesource,
            order_status = "DELIVERED",
            updated_at = DateTime.UtcNow,
            updated_by = "OMS",
            payments = payments ?? Array.Empty<object>()
        };
    }
}
