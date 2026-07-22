using System.ComponentModel.DataAnnotations;

namespace HRManagement.WebUI.Models.Account;

/// <summary>
/// Login formunun ViewModel'i. Buradaki validation yalnızca UX içindir —
/// asıl otorite API + Application katmanındaki LoginCommandValidator'dır.
/// </summary>
public class LoginViewModel
{
    [Display(Name = "Kullanıcı adı veya e-posta")]
    [Required(ErrorMessage = "Kullanıcı adı veya e-posta zorunludur.")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Display(Name = "Şifre")]
    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }
}
