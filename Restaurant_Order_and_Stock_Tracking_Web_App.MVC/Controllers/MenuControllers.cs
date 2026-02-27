using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Menu;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using System.Globalization;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MenuController : Controller
    {
        private readonly RestaurantDbContext _context;

        public MenuController(RestaurantDbContext context)
        {
            _context = context;
        }

        // ── GET: /Menu ───────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Menü Ürünleri";

            // ✅ IsDeleted filtresi — soft delete edilmişleri gösterme
            var menuItems = await _context.MenuItems
                .Where(m => !m.IsDeleted)
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
                .AnyAsync(m => !m.IsDeleted && m.TrackStock && m.StockQuantity < 5);

            return View(menuItems);
        }

        // ── GET: /Menu/Detail/5 ──────────────────────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (item == null) return NotFound();

            ViewData["Title"] = $"{item.MenuItemName} — Detay";
            ViewData["HasLowStock"] = await _context.MenuItems
                .AnyAsync(m => !m.IsDeleted && m.TrackStock && m.StockQuantity < 5);

            return View(item);
        }

        // ── GET: /Menu/Create ────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Yeni Ürün";
            ViewData["Categories"] = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)
                .ToListAsync();

            return View();
        }
        // ── POST: /Menu/Create  (AJAX JSON) ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] MenuItemCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.MenuItemName))
                return Json(new { success = false, message = "Ürün adı boş olamaz." });

            // ✅ FIX #3: Virgül/nokta parse
            if (!decimal.TryParse(
                    dto.MenuItemPriceStr?.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out decimal menuItemPrice) || menuItemPrice < 0)
                return Json(new { success = false, message = "Geçerli bir fiyat giriniz." });

            bool catExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId);
            if (!catExists)
                return Json(new { success = false, message = "Geçersiz kategori seçildi." });

            var item = new MenuItem
            {
                MenuItemName = dto.MenuItemName.Trim(),
                CategoryId = dto.CategoryId,
                MenuItemPrice = menuItemPrice,
                Description = dto.Description?.Trim() ?? string.Empty,
                StockQuantity = dto.StockQuantity,
                TrackStock = dto.TrackStock,
                IsAvailable = dto.IsAvailable,
                IsDeleted = false,
                MenuItemCreatedTime = DateTime.UtcNow
            };

            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Ürün başarıyla eklendi." });
        }
        // ── GET: /Menu/Edit/5 ────────────────────────────────────────
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

        // ── POST: /Menu/Edit  (AJAX JSON) ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
          public async Task<IActionResult> Edit([FromBody] MenuItemEditDto dto)
        {
            if (dto.Id == null)
                return Json(new { success = false, message = "Geçersiz ürün ID." });

            var item = await _context.MenuItems.FindAsync(dto.Id);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            if (string.IsNullOrWhiteSpace(dto.MenuItemName))
                return Json(new { success = false, message = "Ürün adı boş olamaz." });

            // ✅ FIX #3: Virgül/nokta parse
            if (!decimal.TryParse(
                    dto.MenuItemPriceStr?.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out decimal menuItemPrice) || menuItemPrice < 0)
                return Json(new { success = false, message = "Geçerli bir fiyat giriniz." });

            bool catExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId);
            if (!catExists)
                return Json(new { success = false, message = "Geçersiz kategori seçildi." });

            item.MenuItemName = dto.MenuItemName.Trim();
            item.CategoryId = dto.CategoryId;
            item.MenuItemPrice = menuItemPrice;
            item.Description = dto.Description?.Trim() ?? string.Empty;
            item.StockQuantity = dto.StockQuantity;
            item.TrackStock = dto.TrackStock;
            item.IsAvailable = dto.IsAvailable;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Ürün güncellendi." });
        }
        // ── POST: /Menu/Delete  (AJAX JSON) ──────────────────────────
        // ✅ FIX #2: Siparişlerde kullanıldıysa → soft delete (IsDeleted=true)
        //            Hiç kullanılmadıysa → fiziksel sil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            bool usedInOrders = await _context.OrderItems
                .AnyAsync(oi => oi.MenuItemId == id);

            if (usedInOrders)
            {
                // Geçmiş siparişler var → soft delete (görünmez yapar, silmez)
                item.IsDeleted = true;
                item.IsAvailable = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Ürün pasife alındı (geçmiş siparişlerde kullanılmış)." });
            }

            // Hiç sipariş yok → fiziksel sil
            _context.MenuItems.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Ürün silindi." });
        }

        // ── GET: /Menu/GetById/5  (Edit modal için) ──────────────────
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
                // ✅ FIX #3: JS'e gönderirken InvariantCulture noktalı format
                menuItemPrice = m.MenuItemPrice.ToString("F2", CultureInfo.InvariantCulture),
                description = m.Description,
                stockQuantity = m.StockQuantity,
                trackStock = m.TrackStock,
                isAvailable = m.IsAvailable
            });
        }
    }
}