using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Hubs;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    /// <summary>
    /// Müşterilerin QR kod ile açtığı menü ekranı.
    /// Bu controller kimlik doğrulama gerektirmez — [AllowAnonymous] davranışı varsayılan.
    /// </summary>
    public class QrMenuController : Controller
    {
        private readonly RestaurantDbContext _context;
        private readonly IHubContext<RestaurantHub> _hub;

        public QrMenuController(RestaurantDbContext context, IHubContext<RestaurantHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        // ── GET /QrMenu/Index/{tableName} ─────────────────────────────
        /// <summary>
        /// Müşterinin QR kodunu okutunca açılan sayfa.
        /// URL örneği: /QrMenu/Index/Masa-1
        ///             /QrMenu/Index/Teras%201   (boşluk içeren adlar URL-encode ile)
        /// </summary>
        [HttpGet]
        [Route("QrMenu/Index/{tableName}")]
        public async Task<IActionResult> Index(string tableName)
        {
            var decodedName = Uri.UnescapeDataString(tableName);

            var table = await _context.Tables
                .FirstOrDefaultAsync(t => t.TableName == decodedName);

            if (table == null)
                return NotFound("Masa bulunamadı.");

            var menuItems = await _context.MenuItems
                .Where(m => !m.IsDeleted && m.IsAvailable)
                .Include(m => m.Category)
                .OrderBy(m => m.Category.CategorySortOrder)
                .ThenBy(m => m.MenuItemName)
                .ToListAsync();

            ViewData["TableName"] = table.TableName;
            ViewData["IsWaiterCalled"] = table.IsWaiterCalled;

            return View(menuItems);
        }

        // ── POST /QrMenu/CallWaiter ───────────────────────────────────
        /// <summary>
        /// Müşteri "Garson Çağır" butonuna basınca çağrılır.
        /// Payload: { "tableName": "Masa 1" }
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("QrMenu/CallWaiter")]
        public async Task<IActionResult> CallWaiter([FromBody] CallWaiterRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.TableName))
                return BadRequest(new { success = false, message = "Geçersiz masa adı." });

            var table = await _context.Tables
                .FirstOrDefaultAsync(t => t.TableName == request.TableName);

            if (table == null)
                return NotFound(new { success = false, message = "Masa bulunamadı." });

            if (table.IsWaiterCalled)
                return Ok(new { success = true, alreadyCalled = true, message = "Garson zaten çağrıldı." });

            table.IsWaiterCalled = true;
            await _context.SaveChangesAsync();

            // Tüm bağlı garson/admin ekranlarına bildir
            await _hub.Clients.All.SendAsync("WaiterCalled", new
            {
                tableName = table.TableName
            });

            return Ok(new { success = true, alreadyCalled = false, message = "Garson çağrıldı." });
        }
    }

    /// <summary>Müşteri tarafından gönderilen istek gövdesi.</summary>
    public class CallWaiterRequest
    {
        public string TableName { get; set; } = string.Empty;
    }
}
