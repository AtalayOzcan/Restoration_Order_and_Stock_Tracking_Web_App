using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Stock;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StockController : Controller
    {
        private readonly RestaurantDbContext _context;

        public StockController(RestaurantDbContext context)
        {
            _context = context;
        }

        // ── GET: /Stock ──────────────────────────────────────────────
        public async Task<IActionResult> Index(bool showAll = false)
        {
            ViewData["Title"] = "Stok Yönetimi";
            ViewData["ShowAll"] = showAll;

            var allItems = await _context.MenuItems
                .Where(m => !m.IsDeleted)
                .Include(m => m.Category)
                .OrderBy(m => m.Category.CategorySortOrder)
                .ThenBy(m => m.MenuItemName)
                .ToListAsync();

            var displayItems = showAll
                ? allItems
                : allItems.Where(m => m.TrackStock).ToList();

            int totalProducts = allItems.Count;
            int trackedProducts = allItems.Count(m => m.TrackStock);
            int lowStockCount = allItems.Count(m => IsLow(m));
            int criticalCount = allItems.Count(m => IsCritical(m));

            ViewData["TotalProducts"] = totalProducts;
            ViewData["TrackedProducts"] = trackedProducts;
            ViewData["LowStockCount"] = lowStockCount;
            ViewData["CriticalCount"] = criticalCount;

            bool hasAlert = allItems.Any(m => IsLow(m) || IsCritical(m));
            ViewData["HasLowStock"] = hasAlert;
            ViewData["HasAlert"] = hasAlert;

            ViewData["Categories"] = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            var allIds = allItems.Select(m => m.MenuItemId).ToList();

            var recentLogs = await _context.StockLogs
                .Where(l => allIds.Contains(l.MenuItemId))
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var sparklineMap = allIds.ToDictionary(
                id => id,
                id => recentLogs
                    .Where(l => l.MenuItemId == id)
                    .Take(5).Select(l => l.NewStock).Reverse().ToList()
            );
            ViewData["SparklineMap"] = sparklineMap;

            var lastUpdatedMap = allIds.ToDictionary(
                id => id,
                id =>
                {
                    var last = recentLogs.FirstOrDefault(l => l.MenuItemId == id);
                    return last?.CreatedAt ?? allItems.First(m => m.MenuItemId == id).MenuItemCreatedTime;
                }
            );
            ViewData["LastUpdatedMap"] = lastUpdatedMap;

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var consumed = await _context.OrderItems
                .Where(oi =>
                    allIds.Contains(oi.MenuItemId) &&
                    oi.OrderItemAddedAt >= thirtyDaysAgo &&
                    oi.OrderItemStatus != "cancelled")
                .GroupBy(oi => oi.MenuItemId)
                .Select(g => new { MenuItemId = g.Key, Consumed = g.Sum(oi => oi.OrderItemQuantity - oi.CancelledQuantity) })
                .ToDictionaryAsync(g => g.MenuItemId, g => g.Consumed);

            ViewData["ConsumedMap"] = consumed;

            return View(displayItems);
        }

        // ── POST: /Stock/UpdateStock ─────────────────────────────────
        // Eski: çok sayıda ayrı parametre (menuItemId, updateMode, newStockValue, ...)
        // Yeni: tek [FromBody] StockUpdateDto
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock([FromBody] StockUpdateDto dto)
        {
            var item = await _context.MenuItems.FindAsync(dto.MenuItemId);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            int previousStock = item.StockQuantity;
            int newStock;
            string movementType;
            int quantityChange;

            if (dto.UpdateMode == "direct")
            {
                if (dto.NewStockValue == null || dto.NewStockValue < 0)
                    return Json(new { success = false, message = "Geçerli bir stok değeri giriniz." });

                newStock = dto.NewStockValue.Value;
                quantityChange = newStock - previousStock;
                movementType = "Düzeltme";
            }
            else if (dto.UpdateMode == "fire")
            {
                // ── 🔥 Stok Kaynaklı Fire / Zayi Çıkışı ──────────────────────
                // BUG 1+2 DÜZELTMESİ: Depoda bozulan/kırılan ürünler bu moddan girilir.
                // SourceType="StokKaynaklı" → fire raporuna doğru kategoride düşer.
                if (dto.MovementQuantity == null || dto.MovementQuantity <= 0)
                    return Json(new { success = false, message = "Fire miktarını giriniz." });

                if (string.IsNullOrWhiteSpace(dto.Note))
                    return Json(new { success = false, message = "Fire nedenini açıklamak zorunludur (örn: 'Fare kolaları delmiş')." });

                quantityChange = -dto.MovementQuantity.Value;   // her zaman çıkış
                movementType = "Çıkış";
                newStock = previousStock + quantityChange; // eksi yönde

                if (newStock < 0)
                    return Json(new { success = false, message = "Stok sıfırın altına düşemez. Mevcut stok: " + previousStock });
            }
            else
            {
                if (dto.MovementQuantity == null || dto.MovementQuantity <= 0)
                    return Json(new { success = false, message = "Geçerli bir miktar giriniz." });

                if (string.IsNullOrWhiteSpace(dto.Note))
                    return Json(new { success = false, message = "Hareket bazlı işlem için açıklama zorunludur." });

                if (dto.MovementDirection == "in")
                {
                    quantityChange = dto.MovementQuantity.Value;
                    movementType = "Giriş";
                }
                else
                {
                    quantityChange = -dto.MovementQuantity.Value;
                    movementType = "Çıkış";
                }

                newStock = previousStock + quantityChange;
                if (newStock < 0)
                    return Json(new { success = false, message = "Stok miktarı sıfırın altına düşemez." });
            }

            if (dto.AlertThreshold.HasValue && dto.AlertThreshold.Value >= 0)
                item.AlertThreshold = dto.AlertThreshold.Value;
            if (dto.CriticalThreshold.HasValue && dto.CriticalThreshold.Value >= 0)
                item.CriticalThreshold = dto.CriticalThreshold.Value;

            item.StockQuantity = newStock;

            // ── StockLog: SourceType ve UnitPrice eklendi ─────────────────────
            // BUG 1: "StokKaynaklı" SourceType ile fire kaydı ayrışır
            // BUG 5: UnitPrice alanı, raporlarda doğru tutar hesabı sağlar
            _context.StockLogs.Add(new StockLog
            {
                MenuItemId = item.MenuItemId,
                MovementType = movementType,
                QuantityChange = quantityChange,
                PreviousStock = previousStock,
                NewStock = newStock,
                Note = dto.Note?.Trim(),
                SourceType = dto.UpdateMode == "fire" ? "StokKaynaklı" : null,
                OrderId = null,   // stok hareketi — adisyon bağlantısı yok
                UnitPrice = item.MenuItemPrice,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                newStock,
                status = GetStatusString(item),
                statusLabel = GetStatusLabel(item),
                statusPill = GetStatusPillClass(item),
                alertThreshold = item.AlertThreshold,
                criticalThreshold = item.CriticalThreshold,
                message = $"Stok güncellendi. Yeni stok: {newStock}"
            });
        }

        // ── GET: /Stock/GetHistory/5 ─────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetHistory(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            var logs = await _context.StockLogs
                .Where(l => l.MenuItemId == id)
                .OrderByDescending(l => l.CreatedAt)
                .Take(50)
                .Select(l => new
                {
                    l.StockLogId,
                    createdAt = l.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                    l.MovementType,
                    l.QuantityChange,
                    l.PreviousStock,
                    l.NewStock,
                    note = l.Note ?? "—",
                    // BUG 1: Geçmiş modalında fire türü de gösterilir
                    sourceType = l.SourceType ?? "",
                    orderId = l.OrderId
                })
                .ToListAsync();

            return Json(new { success = true, itemName = item.MenuItemName, sku = $"SKU-{item.MenuItemId:D4}", logs });
        }

        // ── POST: /Stock/ToggleTrack ──────────────────────────────────
        // Eski: int menuItemId, bool trackStock — düz parametre
        // Yeni: [FromBody] StockToggleTrackDto
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTrack([FromBody] StockToggleTrackDto dto)
        {
            var item = await _context.MenuItems.FindAsync(dto.MenuItemId);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            item.TrackStock = dto.TrackStock;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                trackStock = item.TrackStock,
                status = GetStatusString(item),
                statusLabel = GetStatusLabel(item),
                statusPill = GetStatusPillClass(item),
                message = item.TrackStock ? "Stok takibi aktif edildi." : "Stok takibi kapatıldı."
            });
        }

        // ── Private helpers ───────────────────────────────────────────
        private static bool IsCritical(MenuItem m) =>
            m.TrackStock && m.CriticalThreshold > 0 && m.StockQuantity <= m.CriticalThreshold;

        private static bool IsLow(MenuItem m) =>
            m.TrackStock && m.AlertThreshold > 0 && m.StockQuantity <= m.AlertThreshold && !IsCritical(m);

        private static string GetStatusString(MenuItem m)
        {
            if (!m.TrackStock) return "NotTracked";
            if (IsCritical(m)) return "Critical";
            if (IsLow(m)) return "Low";
            return "OK";
        }

        private static string GetStatusLabel(MenuItem m) => GetStatusString(m) switch
        {
            "Critical" => "🚨 Kritik",
            "Low" => "⚡ Düşük",
            "NotTracked" => "— Takip Dışı",
            _ => "✓ Yeterli"
        };

        private static string GetStatusPillClass(MenuItem m) => GetStatusString(m) switch
        {
            "Critical" => "pill-red",
            "Low" => "pill-amber",
            "NotTracked" => "pill-gray",
            _ => "pill-green"
        };
    }
}