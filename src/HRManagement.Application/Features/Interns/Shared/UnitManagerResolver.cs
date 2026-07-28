using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.Interns.Shared;

/// <summary>
/// Stajyerin YÖNETİCİSİNİ türetir (kullanıcı kararı, 2026-07-27): stajyerin
/// kayıtta ManagerId'si yoktur — yöneticisi, bağlı olduğu BİRİMİN (birim yoksa
/// DEPARTMANIN) yönetici kademesindeki en kıdemli aktif çalışanıdır.
///
/// "Yönetici kademesi" şirket kuralıyla aynı: GM/GMY/Müdür (IsManagerial).
/// Müdür Yardımcısı ve uzmanlar birim yöneticisi SAYILMAZ. Birimde yönetici
/// yoksa departmana düşülür; orada da yoksa null (ekran "—" gösterir).
/// </summary>
public sealed class UnitManagerResolver
{
    private readonly IEmployeeRepository _employeeRepository;

    public UnitManagerResolver(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Employee?> ResolveAsync(int departmentId, int? unitId)
    {
        // Departman bazlı sorgu repo'da yok; ölçek küçük — tümü çekilip süzülür.
        var managers = (await _employeeRepository.GetAllAsync())
            .Where(e => e.IsActive
                && e.Seniority is SeniorityLevel seniority
                && seniority.IsManagerial())
            .ToList();

        // Önce birimin yöneticisi (en kıdemli = en küçük Seniority değeri)...
        if (unitId is int uid)
        {
            var unitManager = managers
                .Where(e => e.DepartmentId == departmentId && e.UnitId == uid)
                .OrderBy(e => e.Seniority)
                .FirstOrDefault();

            if (unitManager is not null)
                return unitManager;
        }

        // ...yoksa departmanın yöneticisi.
        return managers
            .Where(e => e.DepartmentId == departmentId)
            .OrderBy(e => e.Seniority)
            .FirstOrDefault();
    }
}
