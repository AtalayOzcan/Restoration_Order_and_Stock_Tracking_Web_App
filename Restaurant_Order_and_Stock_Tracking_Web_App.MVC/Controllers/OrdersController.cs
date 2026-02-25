using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using System.Globalization;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    public class OrdersController : Controller
    {
        private readonly RestaurantDbContext _db;

        public OrdersController(RestaurantDbContext db) => _db = db;

        // ─────────────────────────────────────────────────────────────
        // GET /Orders
        // ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string tab = "active", string? searchTable = null)
        {
            ViewData["Title"] = "Siparişler";
            ViewData["ActiveOrderCount"] = await _db.Orders.CountAsync(o => o.OrderStatus == "open");
            ViewData["ActiveTab"] = tab;
            ViewBag.SearchTable = searchTable;

            var localNow = DateTime.Now;
            var todayLocalStart = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Local);
            var todayUtcStart = todayLocalStart.ToUniversalTime();

            // ── Filtresiz (summary bar için) ────────────────────────
            var allActiveOrders = await _db.Orders
                .Where(o => o.OrderStatus == "open")
                .Include(o => o.Table)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .OrderBy(o => o.OrderOpenedAt)
                .ToListAsync();

            var allTodayPastOrders = await _db.Orders
                .Where(o => (o.OrderStatus == "paid" || o.OrderStatus == "cancelled")
                         && o.OrderOpenedAt >= todayUtcStart)
                .ToListAsync();

            ViewBag.AllActiveOrders = allActiveOrders;
            ViewBag.AllTodayRevenue = allTodayPastOrders.Where(o => o.OrderStatus == "paid").Sum(o => o.OrderTotalAmount);
            ViewBag.AllTodayPaidCount = allTodayPastOrders.Count(o => o.OrderStatus == "paid");

            // ── Filtrelenmiş (liste için) ────────────────────────────
            var activeOrders = allActiveOrders.ToList();
            var pastOrdersQuery = _db.Orders
                .Where(o => (o.OrderStatus == "paid" || o.OrderStatus == "cancelled")
                         && o.OrderOpenedAt >= todayUtcStart)
                .Include(o => o.Table)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Payments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTable))
                pastOrdersQuery = pastOrdersQuery.Where(o => o.Table != null &&
                    o.Table.TableName.ToLower().Contains(searchTable.ToLower()));

            var pastOrders = await pastOrdersQuery.OrderByDescending(o => o.OrderClosedAt).ToListAsync();

            if (!string.IsNullOrWhiteSpace(searchTable))
                activeOrders = activeOrders
                    .Where(o => o.Table != null &&
                        o.Table.TableName.Contains(searchTable, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            ViewBag.ActiveOrders = activeOrders;
            ViewBag.PastOrders = pastOrders;
            return View();
        }

        // ─────────────────────────────────────────────────────────────
        // GET /Orders/Create?tableId=5
        // ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Create(int tableId)
        {
            var table = await _db.Tables.FindAsync(tableId);
            if (table == null) { TempData["Error"] = "Masa bulunamadı."; return RedirectToAction("Index", "Tables"); }

            if (table.TableStatus == 1)
            {
                var existing = await _db.Orders.FirstOrDefaultAsync(o => o.TableId == tableId && o.OrderStatus == "open");
                if (existing != null) return RedirectToAction(nameof(Detail), new { id = existing.OrderId });
            }

            var categories = await _db.Categories
                .Where(c => c.IsActive).OrderBy(c => c.CategorySortOrder)
                .Include(c => c.MenuItems.Where(m => m.IsAvailable && !m.IsDeleted))
                .ToListAsync();

            ViewData["Title"] = $"{table.TableName} — Adisyon Aç";
            ViewBag.Table = table;
            ViewBag.Categories = categories;
            return View();
        }

        // ─────────────────────────────────────────────────────────────
        // POST /Orders/Create
        // ─────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int tableId, string openedBy, string? orderNote,
            List<int> menuItemIds, List<int> quantities, List<string?> itemNotes)
        {
            if (string.IsNullOrWhiteSpace(openedBy))
            { TempData["Error"] = "Garson adı boş olamaz."; return RedirectToAction(nameof(Create), new { tableId }); }

            if (menuItemIds == null || !menuItemIds.Any())
            { TempData["Error"] = "En az bir ürün eklemelisiniz."; return RedirectToAction(nameof(Create), new { tableId }); }

            var table = await _db.Tables.FindAsync(tableId);
            if (table == null) { TempData["Error"] = "Masa bulunamadı."; return RedirectToAction("Index", "Tables"); }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    TableId = tableId,
                    OrderStatus = "open",
                    OrderOpenedBy = openedBy.Trim(),
                    OrderNote = string.IsNullOrWhiteSpace(orderNote) ? null : orderNote.Trim(),
                    OrderTotalAmount = 0,
                    OrderOpenedAt = DateTime.UtcNow
                };
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                decimal total = 0;
                for (int i = 0; i < menuItemIds.Count; i++)
                {
                    var mi = await _db.MenuItems.FindAsync(menuItemIds[i]);
                    if (mi == null) continue;
                    int qty = quantities[i] < 1 ? 1 : quantities[i];
                    decimal line = mi.MenuItemPrice * qty;

                    _db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        MenuItemId = mi.MenuItemId,
                        OrderItemQuantity = qty,
                        PaidQuantity = 0,
                        OrderItemUnitPrice = mi.MenuItemPrice,
                        OrderItemLineTotal = line,
                        OrderItemNote = string.IsNullOrWhiteSpace(itemNotes.ElementAtOrDefault(i)) ? null : itemNotes[i]!.Trim(),
                        OrderItemStatus = "pending",
                        OrderItemAddedAt = DateTime.UtcNow
                    });
                    total += line;

                    if (mi.TrackStock)
                    {
                        mi.StockQuantity -= qty;
                        if (mi.StockQuantity <= 0) { mi.StockQuantity = 0; mi.IsAvailable = false; }
                    }
                }

                order.OrderTotalAmount = total;
                table.TableStatus = 1;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = "Adisyon açıldı.";
                return RedirectToAction(nameof(Detail), new { id = order.OrderId });
            }
            catch { await tx.RollbackAsync(); TempData["Error"] = "Adisyon açılırken hata oluştu."; return RedirectToAction(nameof(Create), new { tableId }); }
        }

        // ─────────────────────────────────────────────────────────────
        // GET /Orders/Detail/42
        // ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems.OrderBy(i => i.OrderItemAddedAt)).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) { TempData["Error"] = "Adisyon bulunamadı."; return RedirectToAction(nameof(Index)); }

            var categories = await _db.Categories
                .Where(c => c.IsActive).OrderBy(c => c.CategorySortOrder)
                .Include(c => c.MenuItems.Where(m => m.IsAvailable && !m.IsDeleted))
                .ToListAsync();

            ViewData["Title"] = $"{order.Table?.TableName} — Adisyon #{order.OrderId}";
            ViewBag.Categories = categories;
            return View(order);
        }

        // ─────────────────────────────────────────────────────────────
        // POST /Orders/UpdateItemStatus
        // ─────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItemStatus(int orderItemId, string newStatus, int orderId)
        {
            var validStatuses = new[] { "pending", "preparing", "served", "cancelled" };
            if (!validStatuses.Contains(newStatus)) { TempData["Error"] = "Geçersiz durum."; return RedirectToAction(nameof(Detail), new { id = orderId }); }

            var item = await _db.OrderItems.FindAsync(orderItemId);
            if (item == null) { TempData["Error"] = "Kalem bulunamadı."; return RedirectToAction(nameof(Detail), new { id = orderId }); }

            item.OrderItemStatus = newStatus;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // ─────────────────────────────────────────────────────────────
        // POST /Orders/AddItem
        // ─────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int orderId, int menuItemId, int quantity, string? note)
        {
            // ✅ #1 FIX: OrderItems dahil çek — aynı ürün varsa birleştireceğiz
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            var mi = await _db.MenuItems.FindAsync(menuItemId);

            if (order == null || mi == null) { TempData["Error"] = "Adisyon veya ürün bulunamadı."; return RedirectToAction(nameof(Detail), new { id = orderId }); }
            if (order.OrderStatus != "open") { TempData["Error"] = "Kapalı adisyona ürün eklenemez."; return RedirectToAction(nameof(Detail), new { id = orderId }); }
            if (quantity < 1) quantity = 1;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var noteNorm = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

                // ✅ #1: Aynı ürün, iptal edilmemiş, henüz tamamen ödenmemiş,
                //        aynı nota sahip kalem var mı? → varsa miktar ekle, yeni satır açma
                var existing = order.OrderItems.FirstOrDefault(oi =>
                    oi.MenuItemId == menuItemId &&
                    oi.OrderItemStatus != "cancelled" &&
                    oi.PaidQuantity < oi.OrderItemQuantity &&
                    oi.OrderItemNote == noteNorm);

                if (existing != null)
                {
                    existing.OrderItemQuantity += quantity;
                    existing.OrderItemLineTotal = existing.OrderItemUnitPrice * existing.OrderItemQuantity;
                }
                else
                {
                    _db.OrderItems.Add(new OrderItem
                    {
                        OrderId = orderId,
                        MenuItemId = menuItemId,
                        OrderItemQuantity = quantity,
                        PaidQuantity = 0,
                        OrderItemUnitPrice = mi.MenuItemPrice,
                        OrderItemLineTotal = mi.MenuItemPrice * quantity,
                        OrderItemNote = noteNorm,
                        OrderItemStatus = "pending",
                        OrderItemAddedAt = DateTime.UtcNow
                    });
                }

                order.OrderTotalAmount += mi.MenuItemPrice * quantity;

                if (mi.TrackStock)
                {
                    mi.StockQuantity -= quantity;
                    if (mi.StockQuantity <= 0) { mi.StockQuantity = 0; mi.IsAvailable = false; }
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Success"] = $"{mi.MenuItemName} eklendi.";
            }
            catch { await tx.RollbackAsync(); TempData["Error"] = "Ürün eklenirken hata oluştu."; }

            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // ─────────────────────────────────────────────────────────────
        // POST /Orders/AddPayment
        //
        // Kısmi ödeme akışı:
        //   1. Ödeme kaydı oluştur (Payment tablosu)
        //   2. Ödenen tutarı kalem bazlı PaidQuantity'ye yaz
        //      a) Modal'dan kalem seçimi geldiyse → seçilen kalemlere
        //      b) Direkt tutar girilmişse → FIFO ile en eski kalemlere
        //   3. Tüm kalemler ödendiyse adisyonu kapat
        // ─────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(
            int orderId,
            string payerName,
            string paymentMethod,
            string paymentAmountStr,
            string discountAmountStr,
            // Ödeme modalındaki iselState'den gelen kalem bazlı seçimler
            List<int>? paidItemIds = null,
            List<int>? paidItemQtys = null)
        {
            var culture = CultureInfo.InvariantCulture;

            if (!decimal.TryParse(paymentAmountStr?.Replace(',', '.'), NumberStyles.Any, culture, out decimal paymentAmount) || paymentAmount <= 0)
            { TempData["Error"] = "Geçerli bir ödeme tutarı giriniz."; return RedirectToAction(nameof(Detail), new { id = orderId }); }

            decimal.TryParse(discountAmountStr?.Replace(',', '.'), NumberStyles.Any, culture, out decimal discountAmount);
            if (discountAmount < 0)
            { TempData["Error"] = "İndirim tutarı negatif olamaz."; return RedirectToAction(nameof(Detail), new { id = orderId }); }

            var order = await _db.Orders
                .Include(o => o.Payments)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) { TempData["Error"] = "Adisyon bulunamadı."; return RedirectToAction(nameof(Index)); }
            if (order.OrderStatus != "open") { TempData["Error"] = "Bu adisyon zaten kapatılmış."; return RedirectToAction(nameof(Detail), new { id = orderId }); }

            var netTotal = order.OrderTotalAmount - discountAmount;
            var alreadyPaid = order.Payments.Sum(p => p.PaymentsAmount);
            var remaining = netTotal - alreadyPaid;

            if (paymentAmount > remaining + 0.01m)
            { TempData["Error"] = $"Ödeme tutarı kalan tutarı (₺{remaining:N2}) aşamaz."; return RedirectToAction(nameof(Detail), new { id = orderId }); }

            int methodCode = paymentMethod switch { "credit_card" => 1, "debit_card" => 2, "other" => 3, _ => 0 };

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Ödeme kaydı oluştur
                _db.Payments.Add(new Payment
                {
                    OrderId = orderId,
                    PaymentsMethod = methodCode,
                    PaymentsAmount = paymentAmount,
                    PaymentsChangeGiven = 0,
                    PaymentsPaidAt = DateTime.UtcNow,
                    PaymentsNote = string.IsNullOrWhiteSpace(payerName) ? "" : payerName.Trim()
                });

                // 2. PaidQuantity güncelle
                bool hasItemSel = paidItemIds != null && paidItemQtys != null
                               && paidItemIds.Count > 0 && paidItemIds.Count == paidItemQtys.Count;

                if (hasItemSel)
                {
                    // 2a. Kalem bazlı seçim — modaldan gelen veriler
                    for (int i = 0; i < paidItemIds!.Count; i++)
                    {
                        var oi = order.OrderItems.FirstOrDefault(x => x.OrderItemId == paidItemIds[i]);
                        if (oi == null || paidItemQtys![i] <= 0) continue;

                        int canPay = oi.OrderItemQuantity - oi.PaidQuantity;
                        oi.PaidQuantity += Math.Min(paidItemQtys[i], canPay);
                    }
                }
                else
                {
                    // 2b. FIFO — ödenen tutarı en eski kalemlere önce uygula
                    decimal budget = paymentAmount;
                    var unpaid = order.OrderItems
                        .Where(oi => oi.OrderItemStatus != "cancelled" && oi.PaidQuantity < oi.OrderItemQuantity)
                        .OrderBy(oi => oi.OrderItemAddedAt)
                        .ToList();

                    foreach (var oi in unpaid)
                    {
                        if (budget <= 0.001m) break;
                        int remaining2 = oi.OrderItemQuantity - oi.PaidQuantity;
                        int canAfford = (int)Math.Floor(budget / oi.OrderItemUnitPrice);
                        int payQty = Math.Min(canAfford, remaining2);
                        if (payQty > 0)
                        {
                            oi.PaidQuantity += payQty;
                            budget -= payQty * oi.OrderItemUnitPrice;
                        }
                    }
                }

                // 3. Adisyon kapanış kontrolü
                var newTotalPaid = alreadyPaid + paymentAmount;
                if (newTotalPaid >= netTotal - 0.01m)
                {
                    // Tam ödeme — tüm kalemleri kapat
                    foreach (var oi in order.OrderItems.Where(x => x.OrderItemStatus != "cancelled"))
                        oi.PaidQuantity = oi.OrderItemQuantity;

                    order.OrderStatus = "paid";
                    order.OrderClosedAt = DateTime.UtcNow;
                    if (order.Table != null) order.Table.TableStatus = 0;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                if (order.OrderStatus == "paid")
                {
                    TempData["Success"] = "Adisyon kapatıldı, ödeme tamamlandı.";
                    return RedirectToAction("Index", "Tables");
                }

                TempData["Success"] = $"₺{paymentAmount:N2} ödeme alındı. Kalan: ₺{netTotal - newTotalPaid:N2}";
            }
            catch { await tx.RollbackAsync(); TempData["Error"] = "Ödeme kaydedilirken hata oluştu."; }

            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // ─────────────────────────────────────────────────────────────
        // POST /Orders/Close  (tek seferlik tam ödeme)
        // ─────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int orderId, string paymentMethod, string paymentAmountStr)
        {
            if (!decimal.TryParse(paymentAmountStr?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal paymentAmount))
            { TempData["Error"] = "Geçerli bir tutar giriniz."; return RedirectToAction(nameof(Detail), new { id = orderId }); }

            var order = await _db.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) { TempData["Error"] = "Adisyon bulunamadı."; return RedirectToAction("Index", "Tables"); }
            if (order.OrderStatus != "open") { TempData["Error"] = "Bu adisyon zaten kapatılmış."; return RedirectToAction(nameof(Detail), new { id = orderId }); }
            if (paymentAmount < order.OrderTotalAmount) { TempData["Error"] = "Ödeme tutarı toplam tutardan az olamaz."; return RedirectToAction(nameof(Detail), new { id = orderId }); }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Payments.Add(new Payment
                {
                    OrderId = orderId,
                    PaymentsMethod = paymentMethod == "card" ? 1 : 0,
                    PaymentsAmount = paymentAmount,
                    PaymentsChangeGiven = paymentAmount - order.OrderTotalAmount,
                    PaymentsPaidAt = DateTime.UtcNow,
                    PaymentsNote = ""
                });

                // Tüm kalemleri ödenmiş işaretle
                foreach (var oi in order.OrderItems.Where(x => x.OrderItemStatus != "cancelled"))
                    oi.PaidQuantity = oi.OrderItemQuantity;

                order.OrderStatus = "paid";
                order.OrderClosedAt = DateTime.UtcNow;
                order.Table!.TableStatus = 0;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Success"] = "Adisyon kapatıldı, ödeme alındı.";
                return RedirectToAction("Index", "Tables");
            }
            catch { await tx.RollbackAsync(); TempData["Error"] = "Adisyon kapatılırken hata oluştu."; return RedirectToAction(nameof(Detail), new { id = orderId }); }
        }
        // ─────────────────────────────────────────────────────────────
        // POST /Orders/CancelItem
        //
        // İş kuralları:
        //   1. İptal miktarı: 1 ≤ cancelQty ≤ (ActiveQuantity - PaidQuantity)
        //      (Ödenmiş adeti iptal edemeyiz)
        //   2. CancelledQuantity artar; OrderItemLineTotal güncellenir
        //   3. Adisyon OrderTotalAmount düşülür
        //   4. TrackStock=true ve isWasted=false ise stok iade edilir
        //   5. ActiveQuantity == 0 olunca status="cancelled"
        // ─────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelItem(
            int orderItemId,
            int orderId,
            int cancelQty,
            string? cancelReason,
            bool? isWasted)
        {
            if (cancelQty < 1)
            {
                TempData["Error"] = "İptal miktarı en az 1 olmalıdır.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            var item = await _db.OrderItems
                .Include(oi => oi.MenuItem)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId);

            var order = await _db.Orders.FindAsync(orderId);

            if (item == null || order == null)
            {
                TempData["Error"] = "Kalem veya adisyon bulunamadı.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            if (order.OrderStatus != "open")
            {
                TempData["Error"] = "Kapalı adisyonda iptal yapılamaz.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            // Kaç adet iptal edilebilir?
            // ActiveQuantity = OrderItemQuantity - CancelledQuantity
            // Ödenmiş adeti iptal edemeyiz
            int activeQty = item.OrderItemQuantity - item.CancelledQuantity;
            int cancelable = activeQty - item.PaidQuantity;

            if (cancelQty > cancelable)
            {
                TempData["Error"] = $"En fazla {cancelable} adet iptal edilebilir " +
                                    $"({item.PaidQuantity} adet zaten ödendi).";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                decimal refund = item.OrderItemUnitPrice * cancelQty;

                // ── 1. Kalem güncelle ──────────────────────────────
                item.CancelledQuantity += cancelQty;
                item.CancelReason = string.IsNullOrWhiteSpace(cancelReason)
                                          ? null : cancelReason.Trim();
                // LineTotal sadece aktif adeti yansıtır
                item.OrderItemLineTotal = item.OrderItemUnitPrice
                                         * (item.OrderItemQuantity - item.CancelledQuantity);

                // Tüm aktif adet iptal edildiyse status="cancelled"
                if (item.OrderItemQuantity - item.CancelledQuantity <= 0)
                    item.OrderItemStatus = "cancelled";

                // ── 2. Adisyon tutarını düş ────────────────────────
                order.OrderTotalAmount = Math.Max(0, order.OrderTotalAmount - refund);

                // ── 3. Stok işlemi ─────────────────────────────────
                bool tracksStock = item.MenuItem != null && item.MenuItem.TrackStock;

                if (tracksStock)
                {
                    item.IsWasted = isWasted ?? false;

                    if (isWasted != true)
                    {
                        // Kullanılmadı → stoğa iade et
                        item.MenuItem!.StockQuantity += cancelQty;
                        if (!item.MenuItem.IsAvailable && item.MenuItem.StockQuantity > 0)
                            item.MenuItem.IsAvailable = true;
                    }
                    // isWasted==true → zayi, stok iade edilmez
                }
                else
                {
                    item.IsWasted = null; // stok takibi yok
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                string stockNote = tracksStock
                    ? (isWasted == true ? " | Zayi/Fire olarak işaretlendi."
                                        : " | Stoka iade edildi.")
                    : string.Empty;

                TempData["Success"] = $"{cancelQty} adet iptal edildi.{stockNote}";
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["Error"] = "İptal işlemi sırasında bir hata oluştu.";
            }

            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

    }
}