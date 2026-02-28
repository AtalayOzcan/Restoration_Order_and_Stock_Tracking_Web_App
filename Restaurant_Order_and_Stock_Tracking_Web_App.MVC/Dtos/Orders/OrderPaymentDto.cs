namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Orders;

public class OrderPaymentDto
{
    public int OrderId { get; set; }
    public string? PayerName { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    // JS'ten doğrudan decimal gönderilir (string parse gerek yok)
    public decimal PaymentAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    // Paralel paidItemIds/paidItemQtys yerine nesne listesi
    public List<PaidItemSelectionDto>? PaidItems { get; set; }
}

public class PaidItemSelectionDto
{
    public int OrderItemId { get; set; }
    public int Quantity { get; set; }
}
