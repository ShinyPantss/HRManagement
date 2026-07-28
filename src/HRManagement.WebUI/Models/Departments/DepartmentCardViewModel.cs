namespace HRManagement.WebUI.Models.Departments;

/// <summary>
/// Departman kartının ekran modeli. Departman ucu yalnızca Id/Ad/Açıklama döner;
/// karttaki sayılar çalışan, birim ve stajyer listelerinden TÜRETİLİR — bu yüzden
/// ayrı bir view model var (yeni API ucu açmadan).
///
/// Eksik kalan iki metrik (departman bazında "şu an izinde" ve "bekleyen talep")
/// bilinçli olarak YOK: izin uçları departman bilgisi taşımıyor, isim eşleştirerek
/// üretmek aynı adlı iki kişide sessizce yanlış sayı verirdi.
/// </summary>
public class DepartmentCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Departmandaki aktif çalışan sayısı.</summary>
    public int ActiveCount { get; set; }

    /// <summary>Pasifler dahil toplam kayıt — aktifle farkı varsa kartta gösterilir.</summary>
    public int TotalCount { get; set; }

    /// <summary>Departmana bağlı birim sayısı.</summary>
    public int UnitCount { get; set; }

    /// <summary>Departmana bağlı stajyer sayısı.</summary>
    public int InternCount { get; set; }

    /// <summary>
    /// Departmanın yöneticisi: yönetici kademesindeki (GM/GMY/Müdür) en kıdemli
    /// aktif çalışan. Backend'deki UnitManagerResolver ile aynı kural.
    /// </summary>
    public string? ManagerName { get; set; }
}
