namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Reports
{
    public class ReportsDashboardViewModel
    {
        /// <summary>Bugünkü adisyon toplamı (OrderTotalAmount sum — paid)</summary>
        public decimal TodayGrossSales { get; set; }

        /// <summary>Bugün tahsil edilen tutar (Payment.PaymentsAmount sum)</summary>
        public decimal TodayNetCollected { get; set; }

        /// <summary>Brüt ciro ile tahsilat arasındaki fark</summary>
        public decimal TodayDifference => TodayGrossSales - TodayNetCollected;

        /// <summary>Şu an açık (open) adisyon sayısı</summary>
        public int OpenOrderCount { get; set; }

        /// <summary>Bugünün en çok satan ürünü</summary>
        public string TopSellingItemToday { get; set; } = "—";

        /// <summary>Kritik stok seviyesindeki ürün sayısı</summary>
        public int CriticalStockCount { get; set; }

        /// <summary>Bugün iptal edilen kalemlerden kayıp gelir</summary>
        public decimal TodayCancelledAmount { get; set; }

        /// <summary>Bugün IsWasted=true olan kalemlerin tutarı (fire)</summary>
        public decimal TodayWasteAmount { get; set; }

        /// <summary>Bugün kapatılan adisyonların ortalama süresi (dakika)</summary>
        public double AvgOrderDurationMinutes { get; set; }

        /// <summary>Son yenilenme zamanı</summary>
        public DateTime LastRefreshedAt { get; set; } = DateTime.Now;

        // ── Bugünkü saatlik satış (mini grafik için) ──────────────────────────
        public List<HourlySalesDto> HourlySales { get; set; } = new();

        // ── Kritik stok ürünleri (en fazla 5) ────────────────────────────────
        public List<CriticalStockItemDto> CriticalStockItems { get; set; } = new();

        // ── En çok satan 5 ürün (bugün) ───────────────────────────────────────
        public List<TopProductDto> TopProductsToday { get; set; } = new();
    }

    public class CriticalStockItemDto
    {
        public string ProductName { get; set; } = "";
        public int CurrentStock { get; set; }
        public int CriticalThreshold { get; set; }
    }
}