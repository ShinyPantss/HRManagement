using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models.LeaveRequests;

/// <summary>
/// İzin talebi oluşturma formunun ekran modeli. API modelinden ayrıdır:
/// burada görüntü validasyonu, etiketler ve dropdown seçenekleri yaşar.
/// Tarih alanları nullable — böylece "boş bırakıldı" ile "01.01.0001 girildi"
/// birbirine karışmaz ve [Required] gerçekten çalışır.
///
/// Çalışan SEÇİMİ YOK: talep her zaman giriş yapan kişinin kendisi içindir
/// (§5.3.1); kimlik API tarafında JWT'den çözülür.
/// </summary>
public class LeaveRequestFormViewModel
{
    [Required(ErrorMessage = "İzin türü zorunludur.")]
    [Range(1, 3, ErrorMessage = "Geçerli bir izin türü seçiniz.")]
    [Display(Name = "İzin Türü")]
    public int Type { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Başlangıç Tarihi")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Bitiş Tarihi")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    /// <summary>İzin türü seçenekleri: 1=Yıllık İzin, 2=Ücretsiz İzin, 3=Hastalık İzni.</summary>
    public IEnumerable<SelectListItem> TypeOptions { get; set; } = [];
}
