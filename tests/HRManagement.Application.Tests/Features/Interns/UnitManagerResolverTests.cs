using HRManagement.Application.Features.Interns.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Features.Interns;

/// <summary>
/// Stajyerin türetilmiş yöneticisi kuralının testleri (kullanıcı kararı,
/// 2026-07-27): önce BİRİMİN yönetici kademesindeki (GM/GMY/Müdür) en kıdemli
/// aktif çalışanı; birimde yoksa DEPARTMANINKİ; o da yoksa null.
/// </summary>
public class UnitManagerResolverTests
{
    private const int DepartmentId = 1;
    private const int UnitId = 10;

    private static UnitManagerResolver CreateResolver(params Employee[] employees) =>
        new(new FakeEmployeeRepository(employees));

    [Fact]
    public async Task Birimin_muduru_secilir()
    {
        var resolver = CreateResolver(
            new Employee { Id = 1, FirstName = "Birim", LastName = "Müdürü", DepartmentId = DepartmentId, UnitId = UnitId, Seniority = SeniorityLevel.Mudur, IsActive = true },
            new Employee { Id = 2, FirstName = "Departman", LastName = "GMY", DepartmentId = DepartmentId, UnitId = null, Seniority = SeniorityLevel.GenelMudurYardimcisi, IsActive = true });

        var manager = await resolver.ResolveAsync(DepartmentId, UnitId);

        // Birimde yönetici VARSA departmandaki daha kıdemliye gidilmez.
        Assert.Equal(1, manager!.Id);
    }

    [Fact]
    public async Task Birimde_yonetici_yoksa_departmana_dusulur()
    {
        var resolver = CreateResolver(
            // Birimde yalnızca uzman ve müdür yardımcısı var — ikisi de yönetici SAYILMAZ.
            new Employee { Id = 1, DepartmentId = DepartmentId, UnitId = UnitId, Seniority = SeniorityLevel.Uzman, IsActive = true },
            new Employee { Id = 2, DepartmentId = DepartmentId, UnitId = UnitId, Seniority = SeniorityLevel.MudurYardimcisi, IsActive = true },
            new Employee { Id = 3, DepartmentId = DepartmentId, UnitId = null, Seniority = SeniorityLevel.Mudur, IsActive = true });

        var manager = await resolver.ResolveAsync(DepartmentId, UnitId);

        Assert.Equal(3, manager!.Id);
    }

    [Fact]
    public async Task Pasif_yonetici_secilmez_hic_yoksa_null_doner()
    {
        var resolver = CreateResolver(
            new Employee { Id = 1, DepartmentId = DepartmentId, UnitId = UnitId, Seniority = SeniorityLevel.Mudur, IsActive = false });

        Assert.Null(await resolver.ResolveAsync(DepartmentId, UnitId));
    }

    // ── Fake ─────────────────────────────────────────────────────────────────

    private sealed class FakeEmployeeRepository(Employee[] employees) : IEmployeeRepository
    {
        public Task<IEnumerable<Employee>> GetAllAsync() =>
            Task.FromResult<IEnumerable<Employee>>(employees);

        public Task<Employee?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<int> AddAsync(Employee employee) => throw new NotImplementedException();
        public Task UpdateAsync(Employee employee) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task DeleteWithAccountAsync(int employeeId, int? userId) => throw new NotImplementedException();
        public Task<bool> ExistsByDepartmentIdAsync(int departmentId) => throw new NotImplementedException();
        public Task<bool> ExistsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<bool> ExistsByManagerIdAsync(int managerId) => throw new NotImplementedException();
        public Task<Employee?> GetByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<Employee?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<bool> IsInManagerChainAsync(int managerEmployeeId, int subordinateEmployeeId) => throw new NotImplementedException();
        public Task<IEnumerable<Employee>> GetTeamAsync(int managerEmployeeId) => throw new NotImplementedException();
    }
}
