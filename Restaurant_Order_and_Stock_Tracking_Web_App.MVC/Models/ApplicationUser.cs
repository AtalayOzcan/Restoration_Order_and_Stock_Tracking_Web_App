using Microsoft.AspNetCore.Identity;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    // PhoneNumber zaten IdentityUser'dan geliyor
}
