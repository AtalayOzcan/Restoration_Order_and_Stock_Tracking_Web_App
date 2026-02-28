namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Stock;

public class StockUpdateDto
{
    public int MenuItemId { get; set; }

    /// <summary>
    /// "direct"   → Direkt stok değeri girişi (sayım düzeltmesi)
    /// "movement" → Hareket bazlı giriş/çıkış (normal mal girişi, normal çıkış)
    /// "fire"     → 🔥 Stok Kaynaklı Fire/Zayi (depoda bozulan, kırılan)
    ///              MovementDirection her zaman "out", SourceType="StokKaynaklı" yazılır
    /// </summary>
    public string UpdateMode { get; set; }

    public int? NewStockValue { get; set; }
    public string? MovementDirection { get; set; }
    public int? MovementQuantity { get; set; }
    public string? Note { get; set; }
    public int? AlertThreshold { get; set; }
    public int? CriticalThreshold { get; set; }
}