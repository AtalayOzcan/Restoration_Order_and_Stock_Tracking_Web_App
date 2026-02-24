namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;

public class OrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public virtual Order Order { get; set; }
    public int MenuItemId { get; set; }
    public virtual MenuItem MenuItem { get; set; }

    /// <summary>Sipariş edilen toplam adet</summary>
    public int OrderItemQuantity { get; set; }

    /// <summary>
    /// Bu kalem için ödenmiş adet.
    /// Kısmi ödeme sonrası güncellenir.
    /// RemainingQuantity = OrderItemQuantity - PaidQuantity
    /// </summary>
    public int PaidQuantity { get; set; } = 0;

    public decimal OrderItemUnitPrice { get; set; }

    /// <summary>Toplam tutar (OrderItemQuantity × birim fiyat)</summary>
    public decimal OrderItemLineTotal { get; set; }

    public string? OrderItemNote { get; set; }
    public string OrderItemStatus { get; set; }
    public DateTime OrderItemAddedAt { get; set; }

    // ── Hesaplanan özellikler — DB'ye yazılmaz ──────────────────────

    /// <summary>Henüz ödenmemiş adet</summary>
    public int RemainingQuantity => OrderItemQuantity - PaidQuantity;

    /// <summary>Ödenmemiş kısım tutarı</summary>
    public decimal UnpaidLineTotal => RemainingQuantity * OrderItemUnitPrice;

    /// <summary>Ödenmiş kısım tutarı</summary>
    public decimal PaidLineTotal => PaidQuantity * OrderItemUnitPrice;
}