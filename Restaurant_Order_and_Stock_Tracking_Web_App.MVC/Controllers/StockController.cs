using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    public class StockController : Controller
    {
        private readonly RestaurantDbContext _context;

        public StockController(RestaurantDbContext context)
        {
            _context = context;
        }

        // ── GET: /Stock ──────────────────────────────────────────────
        // Madde 1: Varsayılan olarak sadece TrackStock=true ürünler gelir.
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

            // Madde 1: varsayılan filtre
            var displayItems = showAll
                ? allItems
                : allItems.Where(m => m.TrackStock).ToList();

            // Madde 4: istatistikler tüm ürünler üzerinden
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

            // Madde 5: Son 30 gün tüketimi (OrderItems üzerinden)
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

        // ── POST: /Stock/UpdateStock  (AJAX JSON) ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(
            int menuItemId,
            string updateMode,
            int? newStockValue,
            string? movementDirection,
            int? movementQuantity,
            string? note,
            int? alertThreshold,
            int? criticalThreshold)
        {
            var item = await _context.MenuItems.FindAsync(menuItemId);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            int previousStock = item.StockQuantity;
            int newStock;
            string movementType;
            int quantityChange;

            if (updateMode == "direct")
            {
                if (newStockValue == null || newStockValue < 0)
                    return Json(new { success = false, message = "Geçerli bir stok değeri giriniz." });
                newStock = newStockValue.Value;
                quantityChange = newStock - previousStock;
                movementType = "Düzeltme";
            }
            else
            {
                if (movementQuantity == null || movementQuantity <= 0)
                    return Json(new { success = false, message = "Geçerli bir miktar giriniz." });
                if (string.IsNullOrWhiteSpace(note))
                    return Json(new { success = false, message = "Hareket bazlı işlem için açıklama zorunludur." });

                if (movementDirection == "in") { quantityChange = movementQuantity.Value; movementType = "Giriş"; }
                else { quantityChange = -movementQuantity.Value; movementType = "Çıkış"; }

                newStock = previousStock + quantityChange;
                if (newStock < 0)
                    return Json(new { success = false, message = "Stok miktarı sıfırın altına düşemez." });
            }

            if (alertThreshold.HasValue && alertThreshold.Value >= 0) item.AlertThreshold = alertThreshold.Value;
            if (criticalThreshold.HasValue && criticalThreshold.Value >= 0) item.CriticalThreshold = criticalThreshold.Value;

            item.StockQuantity = newStock;

            _context.StockLogs.Add(new StockLog
            {
                MenuItemId = item.MenuItemId,
                MovementType = movementType,
                QuantityChange = quantityChange,
                PreviousStock = previousStock,
                NewStock = newStock,
                Note = note?.Trim(),
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

        // ── GET: /Stock/GetHistory/5 ──────────────────────────────────
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
                    note = l.Note ?? "—"
                })
                .ToListAsync();

            return Json(new { success = true, itemName = item.MenuItemName, sku = $"SKU-{item.MenuItemId:D4}", logs });
        }

        // ── POST: /Stock/ToggleTrack ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTrack(int menuItemId, bool trackStock)
        {
            var item = await _context.MenuItems.FindAsync(menuItemId);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            item.TrackStock = trackStock;
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
        // Madde 3: İkili eşik — CriticalThreshold kullanır
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