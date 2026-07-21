using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models.LeaveRequests;

/// <summary>
/// İzin talebi oluşturma formunun ekran modeli. API modelinden ayrıdır:
/// burada görüntü validasyonu, etiketler ve dropdown seçenekleri yaşar.
/// Çalışan ve tarih alanları nullable — böylece "boş bırakıldı" ile
/// "0 / 01.01.0001 girildi" birbirine karışmaz ve [Required] gerçekten çalışır.
/// </summary>
public class LeaveRequestFormViewModel
{
    [Required(ErrorMessage = "Çalışan seçimi zorunludur.")]
    [Display(Name = "Çalışan")]
    public int? EmployeeId { get; set; }

    [Required(ErrorMessage = "İzin türü zorunludur.")]
    [Range(1, 3, ErrorMessage = "Geçerli bir izin türü seçiniz.")]
    [Display(Name = "İzin Türü")]
    public int Type { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Başlangıç Tarihi")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Bitiş Tarihi")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    /// <summary>
    /// Çalışan dropdown'ının seçenekleri. Form her View'a dönmeden önce
    /// controller tarafından doldurulur; aksi halde liste boş görünür.
    /// </summary>
    public IEnumerable<SelectListItem> EmployeeOptions { get; set; } = [];

    /// <summary>İzin türü seçenekleri: 1=Yıllık İzin, 2=Ücretsiz İzin, 3=Hastalık İzni.</summary>
    public IEnumerable<SelectListItem> TypeOptions { get; set; } = [];
}
