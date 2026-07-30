namespace HRManagement.WebUI.Models.Api.Organization;

// API'nin Models/Organization tipleriyle aynı JSON şekli
// (Contracts projesi yok — elle senkron tutulur).

public class OrganizationResponse
{
    public List<OrgDepartmentResponse> Departments { get; set; } = [];
    public List<OrgUnitResponse> Units { get; set; } = [];
    public List<OrgMemberResponse> Members { get; set; } = [];
}

public class OrgDepartmentResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class OrgUnitResponse
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class OrgMemberResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;

    /// <summary>SeniorityLevel sayısı (1=GM … 6=Uzman). Stajyerde null.</summary>
    public int? Seniority { get; set; }

    public int DepartmentId { get; set; }
    public int? UnitId { get; set; }
    public bool IsActive { get; set; }
    public bool IsIntern { get; set; }

    /// <summary>Stajyerin sınıfı (0=Hazırlık, 1-4). Çalışanda null.</summary>
    public int? Grade { get; set; }
}
