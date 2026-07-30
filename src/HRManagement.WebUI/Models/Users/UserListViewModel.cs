using System.ComponentModel.DataAnnotations;

namespace HRManagement.WebUI.Models.Users;

/// <summary>
/// Hesap listesi satırı. API'nin UserResponse'una ek olarak ekranın türettiği
/// alanları taşır (rol etiketi, "bu benim hesabım mı").
/// </summary>
public class UserRowViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Role { get; set; }
    public bool IsActive { get; set; }

    public string RoleLabel => RoleDisplay.Label(Role);

    /// <summary>
    /// Oturumu açan Admin'in kendi kaydı. Ekranda rol/erişim kontrolleri
    /// kilitlenir — kural zaten API'de, burada sadece boş yere denenmesin.
    /// </summary>
    public bool IsSelf { get; set; }
}

public class UserListViewModel
{
    public List<UserRowViewModel> Users { get; set; } = [];

    public int TotalCount => Users.Count;
    public int ActiveCount => Users.Count(u => u.IsActive);
    public int PassiveCount => TotalCount - ActiveCount;
    public int AdminCount => Users.Count(u => u.Role == RoleDisplay.Admin && u.IsActive);
}

/// <summary>
/// Hesap düzenleme formu. Username salt okunur taşınır (değiştirilemez ama
/// kullanıcı hangi hesabı düzenlediğini görmeli).
/// </summary>
public class UserFormViewModel
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Rol")]
    public int Role { get; set; }

    [Display(Name = "Hesap aktif")]
    public bool IsActive { get; set; }

    /// <summary>Kendi hesabı: rol ve aktiflik alanları ekranda kilitlenir.</summary>
    public bool IsSelf { get; set; }
}
