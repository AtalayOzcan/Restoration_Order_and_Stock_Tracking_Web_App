namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Reports
{
    public class WasteItemDto
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal TotalLoss { get; set; }
        public DateTime Date { get; set; }
        public string Note { get; set; } = "";
        public string CancelReason { get; set; } = "";
    }

    public class TopWasteProductDto
    {
        public string ProductName { get; set; } = "";
        public int TotalQuantity { get; set; }
        public decimal TotalLoss { get; set; }
    }

    public class WasteReportViewModel
    {
        public DateRangeFilter Filter { get; set; } = new();

        public decimal TotalWasteLoss { get; set; }
        public int TotalWasteCount { get; set; }

        /// <summary>IsWasted=true olan OrderItem kaynaklı fireler</summary>
        public List<WasteItemDto> OrderWastes { get; set; } = new();

        /// <summary>StockLog Çıkış/Düzeltme kaynaklı fireler</summary>
        public List<WasteItemDto> StockLogWastes { get; set; } = new();

        /// <summary>En fazla fire veren 10 ürün</summary>
        public List<TopWasteProductDto> TopWasteProducts { get; set; } = new();

        /// <summary>IsWasted=false olan iptal kalemlerin toplam tutarı (stoka iade)</summary>
        public decimal TotalRefundedToStock { get; set; }

        // Chart için
        public decimal OrderWasteTotal { get; set; }
        public decimal StockLogWasteTotal { get; set; }
    }
}