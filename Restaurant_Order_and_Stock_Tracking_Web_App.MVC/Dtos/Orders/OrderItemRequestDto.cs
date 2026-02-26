namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Orders
{
    public class OrderItemRequestDto
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }
}