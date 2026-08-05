using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Employees.Queries.GetAllEmployees;
using HRManagement.Application.Features.Employees.Queries.GetEmployeeById;
using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Features.Employees;

/// <summary>
/// T.C. Kimlik yalnızca İK'ya görünür (kullanıcı kararı, 2026-07-23).
///
/// Kural detay yolunda doğru işliyordu ama LİSTE ve TEKİL sorgu yolları
/// kırpmadan hiç geçmiyordu: Manager ekibinin, Employee ekip arkadaşlarının
/// T.C.'sini okuyabiliyordu. Testler üç yolun da AYNI kaynağı
/// (EmployeeFieldVisibility) kullandığını sabitler.
/// </summary>
public class EmployeeNationalIdVisibilityTests
{
    private const string E1NationalId = "12345678901";

    private const int ManagerEmployeeId = 2;
    private const int E1EmployeeId = 3;

    private const int HrUserId = 10;
    private const int AdminUserId = 11;
    private const int ManagerUserId = 20;
    private const int E1UserId = 30;

    // ── Liste yolu ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Listede_TC_yalnizca_IKya_gider()
    {
        var result = await GetAllAsync(HrUserId);

        Assert.Equal(E1NationalId, Find(result, E1EmployeeId).NationalId);
    }

    [Theory]
    [InlineData(AdminUserId)]    // Admin de göremez: kural saf İK kuralıdır
    [InlineData(ManagerUserId)]
    [InlineData(E1UserId)]       // kendi kaydı bile olsa göremez
    public async Task Listede_IK_disindaki_roller_TC_gormez(int requesterUserId)
    {
        var result = await GetAllAsync(requesterUserId);

        Assert.All(result, dto => Assert.Null(dto.NationalId));
    }

    // ── Tekil sorgu yolu ─────────────────────────────────────────────────────

    [Fact]
    public async Task Tekil_sorguda_TC_yalnizca_IKya_gider()
    {
        var dto = await GetByIdAsync(E1EmployeeId, HrUserId);

        Assert.Equal(E1NationalId, dto!.NationalId);
    }

    [Fact]
    public async Task Tekil_sorguda_yonetici_astinin_TCsini_gormez()
    {
        var dto = await GetByIdAsync(E1EmployeeId, ManagerUserId);

        Assert.NotNull(dto);              // kaydı görebiliyor
        Assert.Null(dto!.NationalId);     // ama T.C. kırpılmış
    }

    // ── Kural sınıfı ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(Role.HR, true)]
    [InlineData(Role.Admin, false)]
    [InlineData(Role.Manager, false)]
    [InlineData(Role.Employee, false)]
    [InlineData(Role.Intern, false)]
    public void Kural_yalnizca_IKya_izin_verir(Role role, bool expected)
    {
        var requester = new User { Id = 1, Role = role, IsActive = true };

        Assert.Equal(expected, EmployeeFieldVisibility.CanSeeNationalId(requester));
    }

    [Fact]
    public void Bilinmeyen_istekci_TC_goremez()
    {
        Assert.False(EmployeeFieldVisibility.CanSeeNationalId(null));
    }

    // ── Kurulum ──────────────────────────────────────────────────────────────

    private static EmployeeDto Find(IEnumerable<EmployeeDto> employees, int id) =>
        employees.Single(e => e.Id == id);

    private static async Task<List<EmployeeDto>> GetAllAsync(int requesterUserId)
    {
        var (users, employees) = CreateRepositories();
        var handler = new GetAllEmployeesQueryHandler(
            new EmployeeVisibility(users, employees), users, new FakeLeaveRequestRepository());

        var result = await handler.Handle(new GetAllEmployeesQuery(requesterUserId), CancellationToken.None);
        return result.ToList();
    }

    private static Task<EmployeeDto?> GetByIdAsync(int employeeId, int requesterUserId)
    {
        var (users, employees) = CreateRepositories();
        var handler = new GetEmployeeByIdQueryHandler(
            employees, users, new EmployeeVisibility(users, employees));

        return handler.Handle(new GetEmployeeByIdQuery(employeeId, requesterUserId), CancellationToken.None);
    }

    private static (FakeUserRepository Users, FakeEmployeeRepository Employees) CreateRepositories()
    {
        var manager = new Employee
        {
            Id = ManagerEmployeeId, FirstName = "Orta", LastName = "Müdür",
            UserId = ManagerUserId, ManagerId = null, NationalId = "99999999999"
        };
        var e1 = new Employee
        {
            Id = E1EmployeeId, FirstName = "Bir", LastName = "Çalışan",
            UserId = E1UserId, ManagerId = ManagerEmployeeId, NationalId = E1NationalId
        };

        var users = new Dictionary<int, User>
        {
            [HrUserId] = new() { Id = HrUserId, Role = Role.HR, IsActive = true },
            [AdminUserId] = new() { Id = AdminUserId, Role = Role.Admin, IsActive = true },
            [ManagerUserId] = new() { Id = ManagerUserId, Role = Role.Manager, IsActive = true },
            [E1UserId] = new() { Id = E1UserId, Role = Role.Employee, IsActive = true }
        };

        return (new FakeUserRepository(users), new FakeEmployeeRepository(manager, e1));
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

        public Task<IEnumerable<Employee>> GetAllAsync() =>
            Task.FromResult<IEnumerable<Employee>>(employees);

        public Task<IEnumerable<Employee>> GetTeamAsync(int managerEmployeeId) =>
            Task.FromResult<IEnumerable<Employee>>(
                employees.Where(e => e.ManagerId == managerEmployeeId).ToList());

        public async Task<bool> IsInManagerChainAsync(int managerEmployeeId, int subordinateEmployeeId) =>
            (await GetTeamAsync(managerEmployeeId)).Any(e => e.Id == subordinateEmployeeId);

        public Task<int> AddAsync(Employee employee) => throw new NotImplementedException();
        public Task UpdateAsync(Employee employee) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task DeleteWithAccountAsync(int employeeId, int? userId) => throw new NotImplementedException();
        public Task<bool> ExistsByDepartmentIdAsync(int departmentId) => throw new NotImplementedException();
        public Task<bool> ExistsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<bool> ExistsByManagerIdAsync(int managerId) => throw new NotImplementedException();
        public Task<Employee?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<Employee?> GetByNationalIdAsync(string nationalId) => throw new NotImplementedException();
    }

    /// <summary>Liste handler'ı İK/Admin için izin bakiyesi de doldurur; boş dönmesi yeter.</summary>
    private sealed class FakeLeaveRequestRepository : ILeaveRequestRepository
    {
        public Task<IReadOnlyDictionary<int, int>> GetUsedAnnualDaysByEmployeeAsync() =>
            Task.FromResult<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>());

        public Task<LeaveRequest?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetAllAsync() => throw new NotImplementedException();
        public Task<IEnumerable<PendingApprovalDto>> GetActionableWithNamesAsync() => throw new NotImplementedException();
        public Task<IEnumerable<LeaveHistoryDto>> GetAllWithNamesAsync() => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId) => throw new NotImplementedException();
        public Task<int> AddAsync(LeaveRequest leaveRequest) => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetByInternIdAsync(int internId) => throw new NotImplementedException();
        public Task UpdateAsync(LeaveRequest leaveRequest) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task<bool> ExistsByEmployeeIdAsync(int employeeId) => throw new NotImplementedException();
        public Task<bool> ExistsByInternIdAsync(int internId) => throw new NotImplementedException();
        public Task<bool> HasOverlapAsync(int? employeeId, int? internId, DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<int> GetTotalUsedAnnualDaysAsync(int employeeId) => throw new NotImplementedException();
    }
}
