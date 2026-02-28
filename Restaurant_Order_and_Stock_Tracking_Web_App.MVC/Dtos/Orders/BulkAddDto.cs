namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Orders;

public class BulkAddDto
{
    //BulkAddRequest
    public int OrderId { get; set; }
    public List<BulkAddItem> Items { get; set; } = new();
}


public class BulkAddItem
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}