using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models.Employees;

/// <summary>
/// Çalışan ekleme/düzenleme formunun ekran modeli. API modelinden ayrıdır:
/// burada görüntü validasyonu, etiketler ve dropdown seçenekleri yaşar.
/// Tarih/departman alanları nullable — böylece "boş bırakıldı" ile
/// "0/01.01.0001 girildi" birbirine karışmaz ve [Required] gerçekten çalışır.
/// </summary>
public class EmployeeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "T.C. Kimlik No (opsiyonel)")]
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

    [Required(ErrorMessage = "Departman seçimi zorunludur.")]
    [Display(Name = "Departman")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Departman dropdown'ının seçenekleri. Form her View'a dönmeden önce
    /// controller tarafından doldurulur; aksi halde liste boş görünür.
    /// </summary>
    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = [];
}
