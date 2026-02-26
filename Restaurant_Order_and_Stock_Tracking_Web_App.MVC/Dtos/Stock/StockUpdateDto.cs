namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Stock;

public class StockUpdateDto
{
    public int MenuItemId { get; set; }
    public string UpdateMode { get; set; } // "direct" veya "movement"
    public int? NewStockValue { get; set; }
    public string? MovementDirection { get; set; }
    public int? MovementQuantity { get; set; }
    public string? Note { get; set; }
    public int? AlertThreshold { get; set; }
    public int? CriticalThreshold { get; set; }
}