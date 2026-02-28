namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Orders;

public class OrderCloseDto
{
    public int OrderId { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public decimal PaymentAmount { get; set; }
}
