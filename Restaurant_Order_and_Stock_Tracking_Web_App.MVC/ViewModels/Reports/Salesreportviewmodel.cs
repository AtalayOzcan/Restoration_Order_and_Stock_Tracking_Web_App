namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Reports
{
    // ── Shared DTOs (diğer viewmodel'lar da kullanır) ───────────────────────

    public class HourlySalesDto
    {
        public int Hour { get; set; }
        public decimal Amount { get; set; }
    }

    public class PaymentMethodDto
    {
        public string MethodName { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public double Percentage { get; set; }
    }

    public class TopProductDto
    {
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategorySalesDto
    {
        public string CategoryName { get; set; } = "";
        public decimal Revenue { get; set; }
        public double Percentage { get; set; }
    }

    // ── SalesReportViewModel ─────────────────────────────────────────────────

    public class SalesReportViewModel
    {
        public DateRangeFilter Filter { get; set; } = new();

        // Özet kartlar
        public decimal GrossSales { get; set; }
        public decimal NetCollected { get; set; }
        public decimal TotalDiscount { get; set; }
        public int TotalOrderCount { get; set; }
        public int CancelledOrderCount { get; set; }

        // Chart verileri (ilk yüklemede sunucu tarafı doldurur;
        // sonraki filtre değişikliklerinde AJAX endpoint'ten gelir)
        public List<HourlySalesDto> HourlySales { get; set; } = new();
        public List<PaymentMethodDto> PaymentBreakdown { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<CategorySalesDto> CategorySales { get; set; } = new();
    }
}