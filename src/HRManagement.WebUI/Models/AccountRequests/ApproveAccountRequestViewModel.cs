using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models.AccountRequests;

/// <summary>
/// Admin'in talebi onaylama formu. Şifreyi Admin belirler (talepte tutulmaz).
/// Rol boş bırakılırsa API talebin önerdiği rolü kullanır.
/// </summary>
public class ApproveAccountRequestViewModel
{
    public int Id { get; set; }

    // Salt görüntü — hangi talebi onayladığımızı gösterir.
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public string SuggestedRole { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [Display(Name = "Kullanıcı Adı")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    // Bilinçli olarak DataType.Password YOK: geçici şifre görünür olmalı ki Admin
    // okuyup kullanıcıya iletebilsin (type=password alanı value render etmez).
    [Required(ErrorMessage = "Geçici şifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [Display(Name = "Geçici Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Rol (boşsa önerilen kullanılır)")]
    public int? Role { get; set; }

    public IEnumerable<SelectListItem> RoleOptions { get; set; } = [];
}
