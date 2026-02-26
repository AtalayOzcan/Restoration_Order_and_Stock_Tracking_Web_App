namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Orders;

public class OrderItemCancelDto
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int CancelQty { get; set; }
    public string? CancelReason { get; set; }
    public bool? IsWasted { get; set; }
}