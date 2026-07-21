using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.Presentation.Models.Employees;

/// <summary>
/// "Yeni Çalışan" formunun ekran modeli. DTO'dan farkı: ekrana özel
/// parçalar taşır (dropdown seçenekleri, görüntü validasyon kuralları).
/// </summary>
public class EmployeeFormViewModel
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(11, MinimumLength = 11, ErrorMessage = "TCKN 11 haneli olmalıdır.")]
    [Display(Name = "TCKN (opsiyonel)")]
    public string? NationalId { get; set; }

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Telefon (opsiyonel)")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Doğum tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Doğum Tarihi")]
    public DateTime? BirthDate { get; set; }

    [Required(ErrorMessage = "İşe giriş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "İşe Giriş Tarihi")]
    public DateTime? HireDate { get; set; }

    [Required(ErrorMessage = "Pozisyon zorunludur.")]
    [Display(Name = "Pozisyon")]
    public string Position { get; set; } = string.Empty;

    [Required(ErrorMessage = "Departman seçiniz.")]
    [Display(Name = "Departman")]
    public int? DepartmentId { get; set; }

    // Dropdown seçenekleri form her gösterildiğinde controller tarafından doldurulur;
    // POST ile geri gelmez (sadece seçilen DepartmentId gelir).
    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = [];
}
