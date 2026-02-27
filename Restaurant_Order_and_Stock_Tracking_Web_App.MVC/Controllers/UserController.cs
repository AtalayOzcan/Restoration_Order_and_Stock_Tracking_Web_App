using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Users;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers;

[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly RestaurantDbContext _db;

    public UserController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        RestaurantDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    // ── GET /User ─────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Kullanıcı Yönetimi";

        var users = await _userManager.Users.ToListAsync();
        var model = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? "—",
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        return View(model);
    }

    // ── GET /User/Create ──────────────────────────────────────────────
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "Yeni Kullanıcı";
        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return View();
    }

    // ── POST /User/Create ─────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        if (await _userManager.FindByNameAsync(model.UserName) != null)
        {
            ModelState.AddModelError("UserName", "Bu kullanıcı adı zaten kullanılıyor.");
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.Role))
            await _userManager.AddToRoleAsync(user, model.Role);

        TempData["Success"] = $"'{user.FullName}' kullanıcısı oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /User/Edit/{id} ───────────────────────────────────────────
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        ViewData["Title"] = "Kullanıcı Düzenle";
        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

        var model = new UserEditViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = roles.FirstOrDefault() ?? ""
        };

        return View(model);
    }

    // ── POST /User/Edit ───────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        if (user.UserName != model.UserName)
        {
            var existing = await _userManager.FindByNameAsync(model.UserName);
            if (existing != null)
            {
                ModelState.AddModelError("UserName", "Bu kullanıcı adı zaten kullanılıyor.");
                ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }
        }

        user.UserName = model.UserName;
        user.FullName = model.FullName;
        user.Email = model.Email;
        user.PhoneNumber = model.PhoneNumber;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var e in updateResult.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!string.IsNullOrEmpty(model.Role))
            await _userManager.AddToRoleAsync(user, model.Role);

        await _userManager.UpdateSecurityStampAsync(user);

        TempData["Success"] = $"'{user.FullName}' güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /User/ResetPassword ──────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["Error"] = "Şifre en az 6 karakter olmalıdır.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);
            TempData["Success"] = $"'{user.FullName}' şifresi sıfırlandı.";
        }
        else
        {
            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    // ── POST /User/Delete ─────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Admin kendini silemez
        var currentUserId = _userManager.GetUserId(User);
        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Kendi hesabınızı silemezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        // Son Admin koruması
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin"))
        {
            var adminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
            if (adminCount <= 1)
            {
                TempData["Error"] = "Sistemde en az bir Admin bulunmalıdır. Bu kullanıcı silinemez.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Açık siparişi olan garson koruması
        var hasActiveOrders = await _db.Orders
            .AnyAsync(o => o.OrderOpenedBy == user.FullName && o.OrderStatus == "open");
        if (hasActiveOrders)
        {
            TempData["Error"] = $"'{user.FullName}' adına açık siparişler bulunuyor. Önce siparişleri kapatın.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.DeleteAsync(user);
        TempData["Success"] = "Kullanıcı silindi.";
        return RedirectToAction(nameof(Index));
    }
}
