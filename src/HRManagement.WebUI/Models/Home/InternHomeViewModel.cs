using HRManagement.WebUI.Models.Api.Interns;
using HRManagement.WebUI.Models.Api.Mentorship;

namespace HRManagement.WebUI.Models.Home;

/// <summary>
/// Stajyerin ANA SAYFASI ("Staj Panelim"). Profilim'den ayrıdır:
///   Ana Sayfa → staj ilerlemesi, mentor/yönetici, açık görevler, izin durumu
///   Profilim  → kimlik künyesi (kişisel + eğitim + organizasyon bilgileri)
///
/// İki uç birleştirilir: /api/interns/me (özet + izinler) ve
/// /api/interns/my-tasks (görev listesi). Yeni API ucu açılmadı.
/// </summary>
public class InternHomeViewModel
{
    /// <summary>Null = staj kaydı çözülemedi; ekran boş-durum gösterir.</summary>
    public MyInternProfileResponse? Profile { get; set; }

    /// <summary>Mentorun atadığı görevler; alınamazsa boş kalır (panel yine çizilir).</summary>
    public List<InternTaskResponse> Tasks { get; set; } = [];

    /// <summary>null = henüz mentor atanmamış.</summary>
    public string? MentorFullName { get; set; }
}
