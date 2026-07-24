using System.ComponentModel.DataAnnotations;
using HRManagement.WebUI.Models.Units;
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
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Doğum Tarihi")]
    public DateTime? BirthDate { get; set; }

    [Required(ErrorMessage = "İşe giriş tarihi zorunludur.")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "İşe Giriş Tarihi")]
    public DateTime? HireDate { get; set; }

    [Required(ErrorMessage = "Departman seçimi zorunludur.")]
    [Display(Name = "Departman")]
    public int? DepartmentId { get; set; }

    // Birim opsiyonel: birimi olmayan departmanlar (Yönetim) için boş kalabilir.
    // Doluysa seçilen birim, seçilen departmana ait olmalı (API + Application doğrular).
    [Display(Name = "Birim (opsiyonel)")]
    public int? UnitId { get; set; }

    // Pozisyon ARTIK GİRİLMİYOR: Departman + Kıdem'den türetilir ("IT Uzmanı").
    [Required(ErrorMessage = "Kıdem seçimi zorunludur.")]
    [Display(Name = "Kıdem / Ünvan")]
    public int? Seniority { get; set; }

    [Display(Name = "Yönetici (opsiyonel)")]
    public int? ManagerId { get; set; }

    [Display(Name = "Kullanıcı Hesabı Id (opsiyonel)")]
    public int? UserId { get; set; }

    [Range(0, 365, ErrorMessage = "Yıllık izin günü 0-365 arasında olmalıdır.")]
    [Display(Name = "Yıllık İzin Günü (boşsa kıdemden hesaplanır)")]
    public int? AnnualLeaveDays { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; }

    /// <summary>
    /// İşaretliyse (varsayılan) çalışan eklenince Admin'e otomatik hesap talebi düşer;
    /// HR ayrıca talep açmak zorunda kalmaz. Yalnızca oluşturma ekranında gösterilir.
    /// Mevcut bir hesaba bağlandıysa (UserId dolu) sunucu talebi yine de açmaz.
    /// </summary>
    [Display(Name = "Bu çalışan için giriş hesabı talep et")]
    public bool RequestLoginAccount { get; set; } = true;

    /// <summary>
    /// Departman dropdown'ının seçenekleri. Form her View'a dönmeden önce
    /// controller tarafından doldurulur; aksi halde liste boş görünür.
    /// </summary>
    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = [];

    /// <summary>
    /// Yönetici adayları (düzenlemede kişinin kendisi hariç). Her adayın kıdemi
    /// de taşınır ki form, çalışanın seçtiği kıdeme göre listeyi süzebilsin:
    /// yönetici çalışandan kıdemce yüksek olmalı (Uzman, Müdür'e yönetici olamaz).
    /// </summary>
    public IEnumerable<ManagerCandidate> ManagerCandidates { get; set; } = [];

    /// <summary>Birim adayları (tümü); JS seçilen departmana göre süzer.</summary>
    public IEnumerable<UnitOption> UnitCandidates { get; set; } = [];

    /// <summary>Kıdem dropdown'ı (GM … Uzman).</summary>
    public IEnumerable<SelectListItem> SeniorityOptions { get; set; } = SeniorityDisplay.Options();
}

/// <summary>
/// Yönetici dropdown'ı için aday. Kıdem VE departman, JS süzmesi için taşınır:
/// yönetici çalışandan kıdemce yüksek + (GM hariç) aynı departmanda olmalı.
/// </summary>
public record ManagerCandidate(int Id, string Name, int? Seniority, int DepartmentId);
