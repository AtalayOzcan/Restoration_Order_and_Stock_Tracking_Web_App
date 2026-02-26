namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Menu;

public class MenuItemCreateDto
{
   
    public string MenuItemName { get; set; }
    public int CategoryId { get; set; }
    public string MenuItemPriceStr { get; set; } // JS'den gelen string fiyat
    public string? Description { get; set; }
    public int StockQuantity { get; set; }
    public bool TrackStock { get; set; }
    public bool IsAvailable { get; set; }
}
