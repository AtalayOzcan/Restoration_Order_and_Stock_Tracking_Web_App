namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Orders
{
    // 3. Tekil Ürün Ekleme (AddItem)
    public class OrderItemAddDto
    {
        public int OrderId { get; set; }
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }
}