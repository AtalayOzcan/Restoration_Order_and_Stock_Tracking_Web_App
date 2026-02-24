using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using System.Text.RegularExpressions;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    public class TablesController : Controller
    {
        private readonly RestaurantDbContext _db;

        public TablesController(RestaurantDbContext db)
        {
            _db = db;
        }

        // ── GET /Tables ───────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            await CleanupExpiredReservationsAsync();

            ViewData["Title"] = "Masalar";
            ViewData["OccupiedCount"] = await _db.Tables.CountAsync(t => t.TableStatus == 1);

            // ✅ #5: Dolu masalardaki sipariş kalemlerini de çek
            // ✅ #4: Birleştirme için tüm masaları ver
            var tables = await _db.Tables
                .Include(t => t.Orders.Where(o => o.OrderStatus == "open"))
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                .ToListAsync();

            // ✅ #6: Natural sort — "Masa 2" < "Masa 10" < "Masa 11"
            tables = tables
                .OrderBy(t => NaturalSortKey(t.TableName).prefix)
                .ThenBy(t => NaturalSortKey(t.TableName).number)
                .ThenBy(t => NaturalSortKey(t.TableName).suffix)
                .ToList();

            return View(tables);
        }

        // Natural sort yardımcısı — sayıyı prefix'ten ayırır
        private static (string prefix, int number, string suffix) NaturalSortKey(string name)
        {
            var m = Regex.Match(name ?? "", @"^(.*?)(\d+)(.*)$");
            if (m.Success)
                return (m.Groups[1].Value.ToLower(), int.Parse(m.Groups[2].Value), m.Groups[3].Value.ToLower());
            return ((name ?? "").ToLower(), 0, "");
        }

        // Süresi dolan rezervasyonları temizle
        private async Task CleanupExpiredReservationsAsync()
        {
            var cutoff = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(30));
            var expired = await _db.Tables
                .Where(t => t.TableStatus == 2
                         && t.ReservationTime.HasValue
                         && t.ReservationTime.Value <= cutoff)
                .ToListAsync();

            if (!expired.Any()) return;

            foreach (var t in expired)
            {
                t.TableStatus = 0; t.ReservationName = null;
                t.ReservationPhone = null; t.ReservationGuestCount = null; t.ReservationTime = null;
            }
            await _db.SaveChangesAsync();
        }

        // ── POST /Tables/Create ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string tableName, int tableCapacity)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            { TempData["Error"] = "Masa adı boş olamaz."; return RedirectToAction(nameof(Index)); }

            if (tableCapacity < 1 || tableCapacity > 20)
            { TempData["Error"] = "Kapasite 1 ile 20 arasında olmalıdır."; return RedirectToAction(nameof(Index)); }

            if (await _db.Tables.AnyAsync(t => t.TableName == tableName.Trim()))
            { TempData["Error"] = $"'{tableName}' adında bir masa zaten var."; return RedirectToAction(nameof(Index)); }

            _db.Tables.Add(new Table
            {
                TableName = tableName.Trim(),
                TableCapacity = tableCapacity,
                TableStatus = 0,
                TableCreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"'{tableName.Trim()}' başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /Tables/Reserve ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int tableId, string reservationName,
            string reservationPhone, int reservationGuestCount, string reservationTime)
        {
            var table = await _db.Tables.FindAsync(tableId);
            if (table == null) { TempData["Error"] = "Masa bulunamadı."; return RedirectToAction(nameof(Index)); }
            if (table.TableStatus != 0) { TempData["Error"] = "Yalnızca boş masalar rezerve edilebilir."; return RedirectToAction(nameof(Index)); }
            if (string.IsNullOrWhiteSpace(reservationName)) { TempData["Error"] = "İsim soyisim boş olamaz."; return RedirectToAction(nameof(Index)); }
            if (string.IsNullOrWhiteSpace(reservationPhone)) { TempData["Error"] = "Telefon numarası boş olamaz."; return RedirectToAction(nameof(Index)); }
            if (reservationGuestCount < 1 || reservationGuestCount > table.TableCapacity)
            { TempData["Error"] = $"Kişi sayısı 1 ile {table.TableCapacity} arasında olmalıdır."; return RedirectToAction(nameof(Index)); }
            if (!TimeSpan.TryParse(reservationTime, out TimeSpan parsedTime))
            { TempData["Error"] = "Geçerli bir rezervasyon saati giriniz."; return RedirectToAction(nameof(Index)); }

            var localNow = DateTime.Now;
            var reservationLocal = localNow.Date.Add(parsedTime);
            if (reservationLocal < localNow.AddMinutes(-5))
            { TempData["Error"] = "Rezervasyon saati geçmiş bir saat olamaz."; return RedirectToAction(nameof(Index)); }

            table.TableStatus = 2;
            table.ReservationName = reservationName.Trim();
            table.ReservationPhone = reservationPhone.Trim();
            table.ReservationGuestCount = reservationGuestCount;
            table.ReservationTime = DateTime.SpecifyKind(reservationLocal, DateTimeKind.Local).ToUniversalTime();

            await _db.SaveChangesAsync();
            TempData["Success"] = $"'{table.TableName}' — {reservationName} adına rezerve edildi.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /Tables/CancelReserve ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReserve(int tableId)
        {
            var table = await _db.Tables.FindAsync(tableId);
            if (table == null) { TempData["Error"] = "Masa bulunamadı."; return RedirectToAction(nameof(Index)); }
            if (table.TableStatus != 2) { TempData["Error"] = "Bu masa zaten rezerve değil."; return RedirectToAction(nameof(Index)); }

            table.TableStatus = 0; table.ReservationName = null;
            table.ReservationPhone = null; table.ReservationGuestCount = null; table.ReservationTime = null;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"'{table.TableName}' rezervasyonu iptal edildi.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /Tables/Delete ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int tableId)
        {
            var table = await _db.Tables.FindAsync(tableId);
            if (table == null) { TempData["Error"] = "Masa bulunamadı."; return RedirectToAction(nameof(Index)); }
            if (table.TableStatus == 1) { TempData["Error"] = "Açık adisyonu olan masa silinemez."; return RedirectToAction(nameof(Index)); }

            _db.Tables.Remove(table);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"'{table.TableName}' silindi.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /Tables/MergeOrder ───────────────────────────────────
        // ✅ YENİ #4: Masa birleştirme
        // sourceTableId adisyonu → targetTableId adisyonuna birleşir
        // Aynı MenuItemId ise miktarlar toplanır, ayrı satır açılmaz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MergeOrder(int sourceTableId, int targetTableId)
        {
            if (sourceTableId == targetTableId)
            { TempData["Error"] = "Kaynak ve hedef masa aynı olamaz."; return RedirectToAction(nameof(Index)); }

            // Kaynak adisyon — tüm detayları çek
            var sourceOrder = await _db.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.TableId == sourceTableId && o.OrderStatus == "open");

            if (sourceOrder == null)
            { TempData["Error"] = "Kaynak masada açık adisyon bulunamadı."; return RedirectToAction(nameof(Index)); }

            var targetTable = await _db.Tables.FindAsync(targetTableId);
            if (targetTable == null)
            { TempData["Error"] = "Hedef masa bulunamadı."; return RedirectToAction(nameof(Index)); }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Hedef masada açık adisyon var mı?
                var targetOrder = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.TableId == targetTableId && o.OrderStatus == "open");

                if (targetOrder == null)
                {
                    // Hedef boş → kaynak adisyonu o masaya taşı
                    sourceOrder.TableId = targetTableId;
                    sourceOrder.Table.TableStatus = 0;  // kaynak boşalt
                    targetTable.TableStatus = 1;      // hedef dolu yap
                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = $"Adisyon '{targetTable.TableName}' masasına taşındı.";
                    return RedirectToAction("Detail", "Orders", new { id = sourceOrder.OrderId });
                }

                // Hedef masada da adisyon var → birleştir
                foreach (var srcItem in sourceOrder.OrderItems)
                {
                    // Aynı MenuItem var mı hedef adisyonda?
                    var existing = targetOrder.OrderItems
                        .FirstOrDefault(ti => ti.MenuItemId == srcItem.MenuItemId);

                    if (existing != null)
                    {
                        // Var → miktarı ve toplamı ekle
                        existing.OrderItemQuantity += srcItem.OrderItemQuantity;
                        existing.OrderItemLineTotal += srcItem.OrderItemLineTotal;
                        // Durum: en geri olanı koru (pending > preparing > served)
                        if (StatusPriority(srcItem.OrderItemStatus) < StatusPriority(existing.OrderItemStatus))
                            existing.OrderItemStatus = srcItem.OrderItemStatus;
                        // Kaynak kalemi sil (artık hedefte merge edildi)
                        _db.OrderItems.Remove(srcItem);
                    }
                    else
                    {
                        // Yok → kalemi hedef adisyona taşı
                        srcItem.OrderId = targetOrder.OrderId;
                    }
                }

                // Ödemeleri hedef adisyona taşı
                foreach (var payment in sourceOrder.Payments)
                    payment.OrderId = targetOrder.OrderId;

                // Hedef toplamı güncelle
                await _db.SaveChangesAsync(); // önce item değişiklikleri kalıcı olsun
                targetOrder.OrderTotalAmount = await _db.OrderItems
                    .Where(oi => oi.OrderId == targetOrder.OrderId && oi.OrderItemStatus != "cancelled")
                    .SumAsync(oi => oi.OrderItemLineTotal);

                // Kaynak adisyonu kapat
                sourceOrder.OrderStatus = "cancelled";
                sourceOrder.OrderClosedAt = DateTime.UtcNow;
                sourceOrder.OrderTotalAmount = 0;

                // Masaları güncelle
                sourceOrder.Table.TableStatus = 0;  // kaynak → boş
                targetTable.TableStatus = 1;  // hedef → dolu (zaten dolu ama garantile)

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"'{sourceOrder.Table.TableName}' adisyonu '{targetTable.TableName}' masasına birleştirildi.";
                return RedirectToAction("Detail", "Orders", new { id = targetOrder.OrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Birleştirme sırasında hata oluştu: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Durum önceliği: küçük = daha geri (pending en geri)
        private static int StatusPriority(string status) => status switch
        {
            "pending" => 0,
            "preparing" => 1,
            "served" => 2,
            _ => 3
        };
    }
}