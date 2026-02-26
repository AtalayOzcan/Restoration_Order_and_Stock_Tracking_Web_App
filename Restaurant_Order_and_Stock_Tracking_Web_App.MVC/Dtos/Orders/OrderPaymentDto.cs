namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Orders;

public class OrderPaymentDto
{
    public int OrderId { get; set; }
    public string PayerName { get; set; }
    public string PaymentMethod { get; set; }
    public string PaymentAmountStr { get; set; }
    public string DiscountAmountStr { get; set; }
    public List<int>? PaidItemIds { get; set; }
    public List<int>? PaidItemQtys { get; set; }
}
