using System.ComponentModel.DataAnnotations;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Users;

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad Soyad zorunludur.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon numarası girin.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol seçiniz.")]
    public string Role { get; set; } = string.Empty;
}
