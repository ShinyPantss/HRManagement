using HRManagement.WebUI.Models;
using HRManagement.WebUI.Models.Api.Organization;
using HRManagement.WebUI.Models.Organization;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// Organizasyon şeması — departman → birim → kişi. GİRİŞLİ HERKESE açık ve
/// herkes ŞEMANIN TAMAMINI görür.
///
/// Veri /api/organization'dan tek çağrıda gelir. Bu uç bilinçli olarak
/// /api/employees'ten ayrıdır: orası personel dosyasıdır ve EmployeeVisibility
/// ile daralır; burası şirket içi rehberdir ve yalnızca ad/kıdem/kadro bağı
/// taşır (e-posta, telefon, T.C., izin verisi yanıtta hiç yok).
///
/// Ayrım şurada: şemayı herkes görür, KİŞİ DETAYINA yalnızca İK geçebilir
/// (CanOpenProfiles). Bu bir görünüm kararıdır — birisi adresi elle yazsa
/// /api/employees/{id}/detail görünürlük kuralını yeniden ve tam uygular.
/// </summary>
[Authorize]
public class OrganizationController : Controller
{
    private readonly IOrganizationApi _organizationApi;

    public OrganizationController(IOrganizationApi organizationApi)
    {
        _organizationApi = organizationApi;
    }

    public async Task<IActionResult> Index()
    {
        var model = new OrganizationViewModel
        {
            // Profilden profile gezinmek İK'nın işi; diğer roller şemayı okur.
            CanOpenProfiles = User.IsInRole("HR")
        };

        var response = await _organizationApi.GetAsync();

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Organizasyon şeması alınamadı.";
            return View(model);
        }

        var org = response.Data;

        model.TotalUnits = org.Units.Count;
        model.TotalEmployees = org.Members.Count(m => !m.IsIntern && m.IsActive);
        model.TotalInterns = org.Members.Count(m => m.IsIntern);

        model.Departments = org.Departments
            .Select(d =>
            {
                var deptMembers = org.Members.Where(m => m.DepartmentId == d.Id).ToList();

                var dept = new OrgDepartment
                {
                    Id = d.Id,
                    Name = d.Name,
                    HeadCount = deptMembers.Count(m => !m.IsIntern && m.IsActive),
                    InternCount = deptMembers.Count(m => m.IsIntern),
                    Manager = ToPerson(MostSeniorManager(deptMembers))
                };

                dept.Units = org.Units
                    .Where(u => u.DepartmentId == d.Id)
                    .OrderBy(u => u.Name)
                    .Select(u =>
                    {
                        var unitMembers = deptMembers.Where(m => m.UnitId == u.Id).ToList();
                        var lead = MostSeniorManager(unitMembers);

                        return new OrgUnit
                        {
                            Id = u.Id,
                            Name = u.Name,
                            Lead = ToPerson(lead),
                            // Sorumlu üstte yazıyor, listede tekrarlanmasın.
                            Members = unitMembers
                                .Where(m => lead is null || m.Id != lead.Id || m.IsIntern)
                                .OrderBy(m => m.IsIntern)
                                .ThenBy(m => m.Seniority ?? 99)
                                .ThenBy(m => m.FullName)
                                .Select(ToPerson!)
                                .ToList()!
                        };
                    })
                    .ToList();

                // Birime bağlı olmayanlar: GM/GMY gibi departman seviyesindekiler
                // ve birimi henüz atanmamış kayıtlar.
                dept.DirectMembers = deptMembers
                    .Where(m => m.UnitId is null)
                    .Where(m => dept.Manager is null || m.Id != dept.Manager.Id || m.IsIntern)
                    .OrderBy(m => m.IsIntern)
                    .ThenBy(m => m.Seniority ?? 99)
                    .ThenBy(m => m.FullName)
                    .Select(ToPerson!)
                    .ToList()!;

                return dept;
            })
            .OrderByDescending(d => d.HeadCount)
            .ToList();

        return View(model);
    }

    /// <summary>
    /// Kapsamdaki en kıdemli yönetici (GM/GMY/Müdür). Sayı küçüldükçe kıdem
    /// artar. Backend'deki UnitManagerResolver ile aynı kural — iki yer aynı
    /// soruya aynı cevabı vermeli. Stajyerler kademe dışıdır.
    /// </summary>
    private static OrgMemberResponse? MostSeniorManager(IEnumerable<OrgMemberResponse> people) =>
        people
            .Where(m => !m.IsIntern && m.IsActive && SeniorityDisplay.IsManagerial(m.Seniority))
            .OrderBy(m => m.Seniority)
            .FirstOrDefault();

    private static OrgPerson? ToPerson(OrgMemberResponse? m) => m is null ? null : new OrgPerson
    {
        Id = m.Id,
        FullName = m.FullName,
        Initials = Initials(m.FullName),
        Title = m.IsIntern
            ? $"Stajyer · {GradeDisplay.Label(m.Grade ?? 0)}"
            : SeniorityDisplay.Label(m.Seniority),
        IsActive = m.IsActive,
        IsIntern = m.IsIntern
    };

    private static string Initials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}"
            : char.ToUpperInvariant(parts[0][0]).ToString();
    }
}
