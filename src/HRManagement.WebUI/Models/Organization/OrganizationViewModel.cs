namespace HRManagement.WebUI.Models.Organization;

/// <summary>
/// Organizasyon şeması: departman → birim → kişi. Veri /api/organization'dan
/// TEK çağrıda gelir ve girişli herkese aynıdır — role göre daralmaz.
///
/// Yöneticiler ve birim sorumluları KAYITTA TUTULMAZ, türetilir: ilgili
/// kapsamdaki yönetici kademesinden (GM/GMY/Müdür) en kıdemli aktif çalışan.
/// Backend'deki UnitManagerResolver ile aynı kural — iki yer aynı cevabı versin.
/// </summary>
public class OrganizationViewModel
{
    /// <summary>
    /// Kişi adları profil linkine dönüşsün mü. YALNIZCA İK: organizasyonda
    /// profilden profile gezinmek İK'nın işidir. Bu bir GÖRÜNÜM kararıdır —
    /// asıl yetki her hâlükârda API'de (EmployeeVisibility).
    /// </summary>
    public bool CanOpenProfiles { get; set; }

    public List<OrgDepartment> Departments { get; set; } = [];

    public int TotalEmployees { get; set; }
    public int TotalUnits { get; set; }
    public int TotalInterns { get; set; }
}

public class OrgDepartment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Departmanın en kıdemli yöneticisi. Null = atanmamış.</summary>
    public OrgPerson? Manager { get; set; }

    public List<OrgUnit> Units { get; set; } = [];

    /// <summary>Birime bağlı OLMAYAN kişiler (GM/GMY gibi departman seviyesi).</summary>
    public List<OrgPerson> DirectMembers { get; set; } = [];

    public int HeadCount { get; set; }
    public int InternCount { get; set; }
}

public class OrgUnit
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Birimin sorumlusu (en kıdemli yönetici kademesi). Null = atanmamış.</summary>
    public OrgPerson? Lead { get; set; }

    public List<OrgPerson> Members { get; set; } = [];
}

public class OrgPerson
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;

    /// <summary>Kıdem etiketi ya da stajyer için sınıf bilgisi.</summary>
    public string Title { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Stajyer mi — detay linki farklı ekrana gider (Mentorship/Detail).</summary>
    public bool IsIntern { get; set; }
}
