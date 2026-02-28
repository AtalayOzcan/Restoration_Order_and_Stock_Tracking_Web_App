namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models
{
    /// <summary>
    /// Stok hareket geçmişi tablosu.
    /// Her stok değişikliğinde (Giriş / Çıkış / Düzeltme) otomatik kayıt düşülür.
    /// </summary>
    public class StockLog
    {
        public int StockLogId { get; set; }

        public int MenuItemId { get; set; }
        public virtual MenuItem MenuItem { get; set; }

        /// <summary>"Giriş" | "Çıkış" | "Düzeltme"</summary>
        public string MovementType { get; set; }

        /// <summary>Giriş → pozitif, Çıkış → negatif, Düzeltme → fark (+/-)</summary>
        public int QuantityChange { get; set; }

        public int PreviousStock { get; set; }
        public int NewStock { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        // ── YENİ ALANLAR (Migration: AddStockLogFireFields) ──────────────────

        /// <summary>
        /// Fire kaynak türü.
        ///  "SiparişKaynaklı" → Sipariş iptali/zayi (OrdersController.CancelItem)
        ///  "StokKaynaklı"    → Depo fire/kırık/bozuk (StockController.UpdateStock, fire modu)
        ///  null              → Normal giriş/iade/düzeltme (fire değil)
        /// </summary>
        public string? SourceType { get; set; }

        /// <summary>
        /// SourceType="SiparişKaynaklı" ise ilgili adisyon numarası.
        /// Rapor tablosunda "Adisyon No" sütununda gösterilir.
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// İşlem anındaki birim fiyat.
        /// Sipariş kaynaklı → OrderItemUnitPrice (menü fiyatı değişse de doğru tutar)
        /// Stok kaynaklı    → MenuItem.MenuItemPrice (kayıt anındaki satış fiyatı)
        /// </summary>
        public decimal? UnitPrice { get; set; }
    }
}