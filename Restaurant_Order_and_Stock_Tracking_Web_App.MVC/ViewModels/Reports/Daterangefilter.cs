namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Reports
{
    public class DateRangeFilter
    {
        /// <summary>"today" | "week" | "month" | "custom"</summary>
        public string Preset { get; set; } = "today";

        /// <summary>Filtrenin başlangıç tarihi (yerel saat — sorgularda ToUniversalTime() kullanılır)</summary>
        public DateTime From { get; set; }

        /// <summary>Filtrenin bitiş tarihi (exclusive — From+n gün)</summary>
        public DateTime To { get; set; }

        /// <summary>İptal edilmiş adisyonları raporlara dahil et</summary>
        public bool IncludeCancelled { get; set; } = false;

        /// <summary>"stocklog" | "orderitem" — stok tüketim raporunda hangi tarihin baz alınacağı</summary>
        public string TimeBase { get; set; } = "orderitem";

        // ── Yardımcılar ───────────────────────────────────────────────────────

        /// <summary>EF Core sorgularında kullanmak için UTC From</summary>
        public DateTime FromUtc => From.ToUniversalTime();

        /// <summary>EF Core sorgularında kullanmak için UTC To</summary>
        public DateTime ToUtc => To.ToUniversalTime();

        /// <summary>UI'da gösterilecek aralık metni</summary>
        public string DisplayRange => Preset switch
        {
            "today" => "Bugün",
            "week" => "Son 7 Gün",
            "month" => "Son 30 Gün",
            _ => $"{From:dd.MM.yyyy} – {To.AddSeconds(-1):dd.MM.yyyy}"
        };

        /// <summary>
        /// Preset string'inden DateRangeFilter üretir.
        /// Tarih sınırları yerel saate göre ayarlanır.
        /// </summary>
        public static DateRangeFilter FromPreset(
            string preset,
            DateTime? from = null,
            DateTime? to = null,
            bool includeCancelled = false,
            string timeBase = "orderitem")
        {
            var today = DateTime.Today; // yerel gece yarısı

            return preset switch
            {
                "today" => new DateRangeFilter
                {
                    Preset = "today",
                    From = today,
                    To = today.AddDays(1),
                    IncludeCancelled = includeCancelled,
                    TimeBase = timeBase
                },
                "week" => new DateRangeFilter
                {
                    Preset = "week",
                    From = today.AddDays(-6),
                    To = today.AddDays(1),
                    IncludeCancelled = includeCancelled,
                    TimeBase = timeBase
                },
                "month" => new DateRangeFilter
                {
                    Preset = "month",
                    From = today.AddDays(-29),
                    To = today.AddDays(1),
                    IncludeCancelled = includeCancelled,
                    TimeBase = timeBase
                },
                _ => new DateRangeFilter
                {
                    Preset = "custom",
                    From = from?.Date ?? today,
                    To = (to?.Date ?? today).AddDays(1),
                    IncludeCancelled = includeCancelled,
                    TimeBase = timeBase
                }
            };
        }
    }
}