namespace HRManagement.Application.DTOs;

/// <summary>
/// Şirketin organizasyon şeması — departman, birim ve kadro üyeliği.
///
/// KAPSAM KARARI (bilinçli): bu veri EmployeeVisibility'den GEÇMEZ, girişli
/// herkese aynı şekilde döner. Çünkü "kim hangi birimde çalışıyor, birimin
/// sorumlusu kim" bilgisi şirket içi rehber bilgisidir; EmployeeVisibility'nin
/// koruduğu şey PERSONEL DOSYASIDIR (izin bakiyesi, notlar, T.C., iletişim).
///
/// Bu yüzden burada yalnızca ad, kıdem ve kadro bağı taşınır:
/// e-posta, telefon, T.C., doğum tarihi, izin verisi BİLEREK yoktur.
/// Kişinin detayına geçiş isteyen istemci /api/employees/{id}/detail'e gider,
/// orada görünürlük kuralı yeniden ve tam olarak işler.
/// </summary>
public class OrganizationDto
{
    public List<OrgDepartmentDto> Departments { get; set; } = [];
    public List<OrgUnitDto> Units { get; set; } = [];
    public List<OrgMemberDto> Members { get; set; } = [];
}

public class OrgDepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class OrgUnitDto
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Kadro üyesi (çalışan ya da stajyer). Rehber düzeyinde alanlar taşır.
/// </summary>
public class OrgMemberDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;

    /// <summary>Çalışanda SeniorityLevel'in sayısı; stajyerde null.</summary>
    public int? Seniority { get; set; }

    public int DepartmentId { get; set; }
    public int? UnitId { get; set; }
    public bool IsActive { get; set; }

    /// <summary>true = Interns tablosundan; detay linki farklı ekrana gider.</summary>
    public bool IsIntern { get; set; }

    /// <summary>Stajyerin sınıfı (0=Hazırlık, 1-4). Çalışanda null.</summary>
    public int? Grade { get; set; }
}
