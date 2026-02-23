using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    public class MenuController : Controller
    {
        private readonly RestaurantDbContext _context;

        public MenuController(RestaurantDbContext context)
        {
            _context = context;
        }

        // ── GET: /Menu ───────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Menü Ürünleri";

            var menuItems = await _context.MenuItems
                .Include(m => m.Category)
                .OrderBy(m => m.Category.CategorySortOrder)
                .ThenBy(m => m.MenuItemName)
                .ToListAsync();

            ViewData["Categories"] = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            ViewData["HasLowStock"] = await _context.MenuItems
                .AnyAsync(m => m.TrackStock && m.StockQuantity < 5);

            return View(menuItems);
        }

        // ── GET: /Menu/Detail/5 ──────────────────────────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (item == null) return NotFound();

            ViewData["Title"] = $"{item.MenuItemName} — Detay";
            ViewData["HasLowStock"] = await _context.MenuItems
                .AnyAsync(m => m.TrackStock && m.StockQuantity < 5);

            return View(item);
        }

        // ── GET: /Menu/Create ────────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Yeni Ürün";
            ViewData["Categories"] = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)
                .ToListAsync();

            return View();
        }

        // ── POST: /Menu/Create  (AJAX JSON) ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string menuItemName, int categoryId, decimal menuItemPrice,
            string? description, int stockQuantity, bool trackStock, bool isAvailable)
        {
            if (string.IsNullOrWhiteSpace(menuItemName))
                return Json(new { success = false, message = "Ürün adı boş olamaz." });

            if (menuItemPrice < 0)
                return Json(new { success = false, message = "Fiyat negatif olamaz." });

            bool catExists = await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
            if (!catExists)
                return Json(new { success = false, message = "Geçersiz kategori seçildi." });

            var item = new MenuItem
            {
                MenuItemName = menuItemName.Trim(),
                CategoryId = categoryId,
                MenuItemPrice = menuItemPrice,
                Description = description?.Trim() ?? string.Empty,
                StockQuantity = stockQuantity,
                TrackStock = trackStock,
                IsAvailable = isAvailable,
                MenuItemCreatedTime = DateTime.UtcNow
            };

            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Ürün başarıyla eklendi." });
        }

        // ── GET: /Menu/Edit/5 ────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (item == null) return NotFound();

            ViewData["Title"] = "Ürün Düzenle";
            ViewData["Categories"] = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)
                .ToListAsync();

            return View(item);
        }

        // ── POST: /Menu/Edit  (AJAX JSON) ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, string menuItemName, int categoryId, decimal menuItemPrice,
            string? description, int stockQuantity, bool trackStock, bool isAvailable)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            if (string.IsNullOrWhiteSpace(menuItemName))
                return Json(new { success = false, message = "Ürün adı boş olamaz." });

            if (menuItemPrice < 0)
                return Json(new { success = false, message = "Fiyat negatif olamaz." });

            bool catExists = await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
            if (!catExists)
                return Json(new { success = false, message = "Geçersiz kategori seçildi." });

            item.MenuItemName = menuItemName.Trim();
            item.CategoryId = categoryId;
            item.MenuItemPrice = menuItemPrice;
            item.Description = description?.Trim() ?? string.Empty;
            item.StockQuantity = stockQuantity;
            item.TrackStock = trackStock;
            item.IsAvailable = isAvailable;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Ürün güncellendi." });
        }

        // ── POST: /Menu/Delete  (AJAX JSON) ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            // Siparişlerde kullanılmış mı kontrol et
            bool usedInOrders = await _context.OrderItems
                .AnyAsync(oi => oi.MenuItemId == id);

            if (usedInOrders)
                return Json(new { success = false, message = "Bu ürün geçmiş siparişlerde kullanılmış. Silmek yerine pasif yapabilirsiniz." });

            _context.MenuItems.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Ürün silindi." });
        }

        // ── GET: /Menu/GetById/5  (Edit modal için) ──────────────────────
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var m = await _context.MenuItems.FindAsync(id);
            if (m == null) return Json(new { success = false });

            return Json(new
            {
                success = true,
                menuItemId = m.MenuItemId,
                menuItemName = m.MenuItemName,
                categoryId = m.CategoryId,
                menuItemPrice = m.MenuItemPrice,
                description = m.Description,
                stockQuantity = m.StockQuantity,
                trackStock = m.TrackStock,
                isAvailable = m.IsAvailable
            });
        }
    }
}