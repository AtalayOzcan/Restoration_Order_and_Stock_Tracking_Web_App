namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Reports
{
    public class TablePerformanceDto
    {
        public string TableName { get; set; } = "";
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AvgDurationMinutes { get; set; }
        public decimal AvgOrderValue { get; set; }
    }

    public class TableReportViewModel
    {
        public DateRangeFilter Filter { get; set; } = new();
        public List<TablePerformanceDto> Tables { get; set; } = new();
        public int BusiestHour { get; set; }
        public string BusiestTable { get; set; } = "—";
        public int TotalOrders { get; set; }
        public double AvgDuration { get; set; }
    }
}