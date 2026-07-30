namespace HRManagement.API.Models.Organization;

// Organizasyon şeması yanıtı. Girişli HERKESE aynı içerik döner: departman,
// birim ve kadro üyeliği şirket içi rehber bilgisidir. Hassas alan (e-posta,
// telefon, T.C., izin) bilinçli olarak YOKTUR — kişi detayı isteyen istemci
// /api/employees/{id}/detail'e gider, görünürlük kuralı orada işler.

public sealed class OrganizationResponse
{
    public OrganizationResponse(
        List<OrgDepartmentResponse> departments,
        List<OrgUnitResponse> units,
        List<OrgMemberResponse> members)
    {
        Departments = departments;
        Units = units;
        Members = members;
    }

    public List<OrgDepartmentResponse> Departments { get; }
    public List<OrgUnitResponse> Units { get; }
    public List<OrgMemberResponse> Members { get; }
}

public sealed class OrgDepartmentResponse
{
    public OrgDepartmentResponse(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; }
}

public sealed class OrgUnitResponse
{
    public OrgUnitResponse(int id, int departmentId, string name)
    {
        Id = id;
        DepartmentId = departmentId;
        Name = name;
    }

    public int Id { get; }
    public int DepartmentId { get; }
    public string Name { get; }
}

public sealed class OrgMemberResponse
{
    public OrgMemberResponse(
        int id, string fullName, int? seniority, int departmentId,
        int? unitId, bool isActive, bool isIntern, int? grade)
    {
        Id = id;
        FullName = fullName;
        Seniority = seniority;
        DepartmentId = departmentId;
        UnitId = unitId;
        IsActive = isActive;
        IsIntern = isIntern;
        Grade = grade;
    }

    public int Id { get; }
    public string FullName { get; }

    /// <summary>SeniorityLevel sayısı (1=GM … 6=Uzman). Stajyerde null.</summary>
    public int? Seniority { get; }

    public int DepartmentId { get; }
    public int? UnitId { get; }
    public bool IsActive { get; }

    /// <summary>true = stajyer; detay linki Mentorship ekranına gider.</summary>
    public bool IsIntern { get; }

    /// <summary>Stajyerin sınıfı (0=Hazırlık, 1-4). Çalışanda null.</summary>
    public int? Grade { get; }
}
