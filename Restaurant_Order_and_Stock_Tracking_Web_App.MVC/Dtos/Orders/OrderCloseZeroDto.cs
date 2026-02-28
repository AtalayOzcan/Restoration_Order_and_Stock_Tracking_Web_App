namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Orders
{
    // 5. Hızlı Kapatma (Close / CloseZero)
    public class OrderCloseZeroDto
    {
        public int OrderId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentAmountStr { get; set; }
    }
}