namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Reports;

public class GeneralReportFilterDto
{
    public string Preset { get; set; } = "today";
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool IncludeCancelled { get; set; } = false;
    public string TimeBase { get; set; } = "orderitem";
    public string? Category { get; set; }
}
