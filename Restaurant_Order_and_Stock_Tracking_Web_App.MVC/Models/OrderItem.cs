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

    /// <summary>Bu kalem için ödenmiş adet</summary>
    public int PaidQuantity { get; set; } = 0;

    // ── İPTAL / İADE ALANLARI ─────────────────────────────────────────

    /// <summary>
    /// Kısmi veya tam iptal edilen adet.
    /// OrderItemQuantity sabit kalır; sadece bu alan artar.
    /// ActiveQuantity = OrderItemQuantity - CancelledQuantity
    /// </summary>
    public int CancelledQuantity { get; set; } = 0;

    /// <summary>İptal sebebi (garson girer, opsiyonel)</summary>
    public string? CancelReason { get; set; }

    /// <summary>
    /// Yalnızca TrackStock=true olan ürünler için dolu olur.
    ///  true  → Zayi / Fire (ürün kullanıldı, stoka iade edilmez)
    ///  false → Kullanılmadı (stoka iade edilir)
    ///  null  → Stok takibi olmayan ürün, stok işlemi yapılmadı
    /// </summary>
    public bool? IsWasted { get; set; }

    // ── FİYAT / DURUM ─────────────────────────────────────────────────

    public decimal OrderItemUnitPrice { get; set; }

    /// <summary>
    /// Aktif (iptal edilmemiş) adet üzerinden hesaplanan tutar.
    /// İptal sonrası güncellenir.
    /// </summary>
    public decimal OrderItemLineTotal { get; set; }

    public string? OrderItemNote { get; set; }
    public string OrderItemStatus { get; set; }
    public DateTime OrderItemAddedAt { get; set; }

    // ── Hesaplanan özellikler — DB'ye yazılmaz ────────────────────────

    /// <summary>İptal edilmemiş aktif adet</summary>
    public int ActiveQuantity => OrderItemQuantity - CancelledQuantity;

    /// <summary>Ödenmemiş ve iptal edilmemiş adet</summary>
    public int RemainingQuantity => ActiveQuantity - PaidQuantity;

    /// <summary>Ödenmemiş kısım tutarı</summary>
    public decimal UnpaidLineTotal => RemainingQuantity * OrderItemUnitPrice;

    /// <summary>Ödenmiş kısım tutarı</summary>
    public decimal PaidLineTotal => PaidQuantity * OrderItemUnitPrice;

    /// <summary>İptal edilen kısım tutarı</summary>
    public decimal CancelledLineTotal => CancelledQuantity * OrderItemUnitPrice;
}