namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models
{
    public class MenuItem
    {
        public int MenuItemId { get; set; }
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; } // virtual olma nedeni nedir = arkada halihazırda performans amaçlı query tutar(lazy loading)
        public string MenuItemName { get; set; }
        public decimal MenuItemPrice { get; set; }
        /// <summary>Uyarı eşiği: Stok &lt;= bu değer → "Düşük" (turuncu)</summary>
        public int AlertThreshold { get; set; } = 0;

        /// <summary>Kritik eşik: Stok &lt;= bu değer → "Kritik" (kırmızı).
        /// Genellikle AlertThreshold'un yarısı kadar ayarlanır.</summary>
        public int CriticalThreshold { get; set; } = 0;
        public int StockQuantity { get; set; }
        public bool TrackStock { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string Description { get; set; }
        public DateTime MenuItemCreatedTime { get; set; }

        /*Bir menü ürününn geçmişteki binlerce saiparişini kendi üzerinde liste olarak tutmaya gertek yok
         Geçmiş siparişleri ve satış raporları "Order" -> OrderItem üzerinden çekilecektir.
         */
        //public virtual ICollection<OrderItem> OrderItems { get; set; }

    }
}