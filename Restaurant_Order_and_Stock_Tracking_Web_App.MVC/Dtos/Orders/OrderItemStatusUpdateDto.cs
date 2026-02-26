namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Orders
{
    // 2. Durum Güncelleme (Update Status)
    public class OrderItemStatusUpdateDto
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public string NewStatus { get; set; }
    }
}