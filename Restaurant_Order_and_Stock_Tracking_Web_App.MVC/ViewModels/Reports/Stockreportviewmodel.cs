namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Reports
{
    public class StockConsumptionDto
    {
        public int MenuItemId { get; set; }
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int CurrentStock { get; set; }
        public int ConsumedInPeriod { get; set; }
        public double DailyAvgConsumption { get; set; }

        /// <summary>CurrentStock / DailyAvgConsumption; 0 ise 999 döner</summary>
        public int EstimatedDaysLeft =>
            DailyAvgConsumption > 0
                ? (int)Math.Floor(CurrentStock / DailyAvgConsumption)
                : 999;

        public decimal? CostPrice { get; set; }
    }

    public class StockReportViewModel
    {
        public DateRangeFilter Filter { get; set; } = new();
        public List<StockConsumptionDto> Products { get; set; } = new();

        // Dropdown için kategori listesi
        public List<string> Categories { get; set; } = new();
    }
}