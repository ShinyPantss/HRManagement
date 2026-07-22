using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models.Interns;

/// <summary>
/// Stajyer ekleme/düzenleme formunun ekran modeli. API modelinden ayrıdır:
/// burada görüntü validasyonu, etiketler ve dropdown seçenekleri yaşar.
/// Tarih/departman alanları nullable — böylece boş bırakılan form alanı
/// 0 veya 01.01.0001 gibi sahte bir değere düşmeden [Required]'a takılır.
/// </summary>
public class InternFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Üniversite zorunludur.")]
    [Display(Name = "Üniversite")]
    public string University { get; set; } = string.Empty;

    [Display(Name = "Bölüm")]
    public string Major { get; set; } = string.Empty;

    [Range(1, 8, ErrorMessage = "Sınıf 1-8 arasında olmalıdır.")]
    [Display(Name = "Sınıf")]
    public int Grade { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Staj Başlangıcı")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Staj Bitişi")]
    public DateTime? EndDate { get; set; }

    // Mentor opsiyonel — stajyere henüz mentor atanmamış olabilir.
    [Display(Name = "Mentor Id (opsiyonel)")]
    public int? MentorId { get; set; }

    [Required(ErrorMessage = "Departman seçimi zorunludur.")]
    [Display(Name = "Departman")]
    public int? DepartmentId { get; set; }

    // Departman dropdown'ının kaynağı; controller her form gösteriminde doldurur.
    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = [];
}
