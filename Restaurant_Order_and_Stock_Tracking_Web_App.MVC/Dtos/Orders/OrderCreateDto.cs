namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Orders;

public class OrderCreateDto
{
    public int TableId { get; set; }
    public string? OrderNote { get; set; }
    // Paralel listeler yerine nesne listesi — parametre kirliliğini önler
    public List<OrderCreateItemDto> Items { get; set; } = new();
}

public class OrderCreateItemDto
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
