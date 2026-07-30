using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Features.Employees;

/// <summary>
/// Genişletilmiş görünürlük kurallarının testleri (kullanıcı kararı, 2026-07-24):
/// kişi BİR ÜSTÜNÜ ve ekip arkadaşlarını görür; üstünün üstünü (GM) GÖREMEZ —
/// organizasyonda zincirleme gezinti yoktur.
///
/// Test organizasyonu: GM(1) → Müdür(2) → [E1(3), E2(4)]
/// </summary>
public class EmployeeVisibilityTests
{
    private const int GmEmployeeId = 1;
    private const int ManagerEmployeeId = 2;
    private const int E1EmployeeId = 3;
    private const int E2EmployeeId = 4;

    private const int GmUserId = 10;
    private const int ManagerUserId = 20;
    private const int E1UserId = 30;

    private static EmployeeVisibility CreateVisibility()
    {
        var gm = new Employee { Id = GmEmployeeId, FirstName = "Genel", LastName = "Müdür", UserId = GmUserId, ManagerId = null };
        var manager = new Employee { Id = ManagerEmployeeId, FirstName = "Orta", LastName = "Müdür", UserId = ManagerUserId, ManagerId = GmEmployeeId };
        var e1 = new Employee { Id = E1EmployeeId, FirstName = "Bir", LastName = "Çalışan", UserId = E1UserId, ManagerId = ManagerEmployeeId };
        var e2 = new Employee { Id = E2EmployeeId, FirstName = "İki", LastName = "Çalışan", UserId = 40, ManagerId = ManagerEmployeeId };

        var users = new Dictionary<int, User>
        {
            [GmUserId] = new() { Id = GmUserId, Role = Role.Manager, IsActive = true },
            [ManagerUserId] = new() { Id = ManagerUserId, Role = Role.Manager, IsActive = true },
            [E1UserId] = new() { Id = E1UserId, Role = Role.Employee, IsActive = true }
        };

        return new EmployeeVisibility(
            new FakeUserRepository(users),
            new FakeEmployeeRepository(gm, manager, e1, e2));
    }

    // ── Liste (GetVisibleAsync) ──────────────────────────────────────────────

    [Fact]
    public async Task Employee_yoneticisini_ve_ekip_arkadaslarini_gorur_GMyi_gormez()
    {
        var visible = (await CreateVisibility().GetVisibleAsync(E1UserId)).Select(e => e.Id).ToList();

        Assert.Contains(ManagerEmployeeId, visible);   // bir üst yönetici
        Assert.Contains(E1EmployeeId, visible);        // kendisi
        Assert.Contains(E2EmployeeId, visible);        // ekip arkadaşı
        Assert.DoesNotContain(GmEmployeeId, visible);  // üstünün üstü GÖRÜNMEZ
    }

    [Fact]
    public async Task Manager_bir_ustunu_ve_ekibini_gorur()
    {
        var visible = (await CreateVisibility().GetVisibleAsync(ManagerUserId)).Select(e => e.Id).ToList();

        Assert.Contains(GmEmployeeId, visible);        // bir üstü (GM)
        Assert.Contains(ManagerEmployeeId, visible);   // kendisi
        Assert.Contains(E1EmployeeId, visible);        // ekibi
        Assert.Contains(E2EmployeeId, visible);
    }

    // ── Detay (EnsureCanViewAsync): liste ile aynı sınırlar ──────────────────

    [Fact]
    public async Task Employee_yoneticisinin_ve_kardesinin_detayini_acabilir()
    {
        var visibility = CreateVisibility();

        await visibility.EnsureCanViewAsync(E1UserId, ManagerEmployeeId);   // bir üst
        await visibility.EnsureCanViewAsync(E1UserId, E2EmployeeId);        // kardeş
    }

    [Fact]
    public async Task Employee_GMnin_detayina_ulasamaz()
    {
        // Zincirleme gezinti kilidi: yöneticisinin detayında GM'nin adı görünse
        // de o sayfaya GEÇEMEZ — bir üstten öteye yol yok.
        await Assert.ThrowsAsync<ValidationException>(
            () => CreateVisibility().EnsureCanViewAsync(E1UserId, GmEmployeeId));
    }

    [Fact]
    public async Task Manager_bir_ustunun_detayini_acabilir()
    {
        await CreateVisibility().EnsureCanViewAsync(ManagerUserId, GmEmployeeId);
    }

    // ── Fake'ler ─────────────────────────────────────────────────────────────

    private sealed class FakeUserRepository(Dictionary<int, User> users) : IUserRepository
    {
        public Task<User?> GetByIdAsync(int id) =>
            Task.FromResult(users.TryGetValue(id, out var user) ? user : null);

        public Task<User?> GetByUsernameAsync(string username) => throw new NotImplementedException();
        public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<IEnumerable<User>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(User user) => throw new NotImplementedException();
        public Task UpdateAsync(User user) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task<int> CountActiveAdminsAsync() => throw new NotImplementedException();
        public Task<int> CreateForPersonAsync(User user, int? employeeId, int? internId, int? accountRequestId, int? reviewerUserId)
            => throw new NotImplementedException();
    }

    private sealed class FakeEmployeeRepository(params Employee[] employees) : IEmployeeRepository
    {
        public Task<Employee?> GetByIdAsync(int id) =>
            Task.FromResult(employees.FirstOrDefault(e => e.Id == id));

        public Task<Employee?> GetByUserIdAsync(int userId) =>
            Task.FromResult(employees.FirstOrDefault(e => e.UserId == userId));

        // Zincir aşağı: doğrudan ve dolaylı astlar (küçük org — özyineleme basit).
        public Task<IEnumerable<Employee>> GetTeamAsync(int managerEmployeeId)
        {
            var result = new List<Employee>();
            void AddReports(int managerId)
            {
                foreach (var e in employees.Where(e => e.ManagerId == managerId))
                {
                    result.Add(e);
                    AddReports(e.Id);
                }
            }
            AddReports(managerEmployeeId);
            return Task.FromResult<IEnumerable<Employee>>(result);
        }

        public async Task<bool> IsInManagerChainAsync(int managerEmployeeId, int subordinateEmployeeId) =>
            (await GetTeamAsync(managerEmployeeId)).Any(e => e.Id == subordinateEmployeeId);

        public Task<IEnumerable<Employee>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(Employee employee) => throw new NotImplementedException();
        public Task UpdateAsync(Employee employee) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task DeleteWithAccountAsync(int employeeId, int? userId) => throw new NotImplementedException();
        public Task<bool> ExistsByDepartmentIdAsync(int departmentId) => throw new NotImplementedException();
        public Task<bool> ExistsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<bool> ExistsByManagerIdAsync(int managerId) => throw new NotImplementedException();
        public Task<Employee?> GetByEmailAsync(string email) => throw new NotImplementedException();
    }
}
