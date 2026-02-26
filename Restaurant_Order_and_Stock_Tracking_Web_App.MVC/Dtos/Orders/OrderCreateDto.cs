namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Orders;

public class OrderCreateDto
{
    public int TableId { get; set; }
    public string OpenedBy { get; set; }
    public string? OrderNote { get; set; }
    public List<int> MenuItemIds { get; set; }
    public List<int> Quantities { get; set; }
    public List<string?> ItemNotes { get; set; }
}