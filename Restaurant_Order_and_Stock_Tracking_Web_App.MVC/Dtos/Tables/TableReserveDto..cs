namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Tables;

public class TableReserveDto
{
    public int TableId { get; set; }
    public string ReservationName { get; set; }
    public string ReservationPhone { get; set; }
    public int ReservationGuestCount { get; set; }
    public string ReservationTime { get; set; }
}