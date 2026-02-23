using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    public class CategoryController : Controller
    {
        private readonly RestaurantDbContext _context;

        public CategoryController(RestaurantDbContext context)
        {
            _context = context;
        }

        // ── GET: /Category ───────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Kategoriler";

            var categories = await _context.Categories
                .Include(c => c.MenuItems)
                .OrderBy(c => c.CategorySortOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            ViewData["HasLowStock"] = await _context.MenuItems
                .AnyAsync(m => m.TrackStock && m.StockQuantity < 5);

            return View(categories);
        }

        // ── GET: /Category/Detail/5 ──────────────────────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var category = await _context.Categories
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound();

            ViewData["Title"] = $"{category.CategoryName} — Detay";
            ViewData["HasLowStock"] = await _context.MenuItems
                .AnyAsync(m => m.TrackStock && m.StockQuantity < 5);

            return View(category);
        }

        // ── GET: /Category/Create ────────────────────────────────────────
        public IActionResult Create()
        {
            ViewData["Title"] = "Yeni Kategori";
            return View();
        }

        // ── POST: /Category/Create  (AJAX JSON) ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string categoryName, int categorySortOrder, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return Json(new { success = false, message = "Kategori adı boş olamaz." });

            bool exists = await _context.Categories
                .AnyAsync(c => c.CategoryName.ToLower() == categoryName.Trim().ToLower());

            if (exists)
                return Json(new { success = false, message = "Bu kategori adı zaten mevcut." });

            var category = new Category
            {
                CategoryName = categoryName.Trim(),
                CategorySortOrder = categorySortOrder,
                IsActive = isActive
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Kategori başarıyla eklendi." });
        }

        // ── GET: /Category/Edit/5 ────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            ViewData["Title"] = "Kategori Düzenle";
            return View(category);
        }

        // ── POST: /Category/Edit  (AJAX JSON) ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string categoryName, int categorySortOrder, bool isActive)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return Json(new { success = false, message = "Kategori bulunamadı." });

            if (string.IsNullOrWhiteSpace(categoryName))
                return Json(new { success = false, message = "Kategori adı boş olamaz." });

            bool exists = await _context.Categories
                .AnyAsync(c => c.CategoryName.ToLower() == categoryName.Trim().ToLower()
                            && c.CategoryId != id);

            if (exists)
                return Json(new { success = false, message = "Bu kategori adı zaten mevcut." });

            category.CategoryName = categoryName.Trim();
            category.CategorySortOrder = categorySortOrder;
            category.IsActive = isActive;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Kategori güncellendi." });
        }

        // ── POST: /Category/Delete  (AJAX JSON) ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return Json(new { success = false, message = "Kategori bulunamadı." });

            if (category.MenuItems != null && category.MenuItems.Any())
                return Json(new
                {
                    success = false,
                    message = $"Bu kategoriye bağlı {category.MenuItems.Count} ürün var. Önce ürünleri silin veya başka kategoriye taşıyın."
                });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Kategori silindi." });
        }

        // ── GET: /Category/GetById/5  (Edit modal için) ─────────────────
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _context.Categories.FindAsync(id);
            if (c == null) return Json(new { success = false });

            return Json(new
            {
                success = true,
                categoryId = c.CategoryId,
                categoryName = c.CategoryName,
                categorySortOrder = c.CategorySortOrder,
                isActive = c.IsActive
            });
        }
    }
}