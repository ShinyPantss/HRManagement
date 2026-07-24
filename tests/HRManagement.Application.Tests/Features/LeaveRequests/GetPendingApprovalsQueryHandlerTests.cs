using HRManagement.Application.DTOs;
using HRManagement.Application.Features.LeaveRequests.Queries.GetPendingApprovals;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Features.LeaveRequests;

/// <summary>
/// "Onay Bekleyenler" yetki süzmesinin birim testleri. Bu süzme, yazma yolundaki
/// LeaveApprovalGuard ile AYNI kuralı yansıtmak zorunda — güvenlik açısından kritik:
///   Pending    → Admin her şeyi; aksi hâlde talep sahibi ekibimde / mentee'm.
///   PendingHr  → İK/Admin VE 1. aşamayı ben onaylamadıysam.
///   Her aşamada kendi talebim hariç.
/// </summary>
public class GetPendingApprovalsQueryHandlerTests
{
    [Fact]
    public async Task manager_ekibindeki_pending_i_gorur_baskasinikini_gormez()
    {
        var actor = MakeUser(10, Role.Manager);
        var team = new[] { MakeEmployee(200) };
        var candidates = new List<PendingApprovalDto>
        {
            Req(1, LeaveStatus.Pending, employeeId: 200, ownerUserId: 20), // ekipte → görünür
            Req(2, LeaveStatus.Pending, employeeId: 300, ownerUserId: 30), // ekipte değil → gizli
        };

        var result = await RunAsync(actor, MakeEmployee(100), team, candidates);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task kendi_talebini_gormez()
    {
        var actor = MakeUser(10, Role.Manager);
        var candidates = new List<PendingApprovalDto>
        {
            Req(1, LeaveStatus.Pending, employeeId: 100, ownerUserId: 10), // owner == actor
        };

        var result = await RunAsync(actor, MakeEmployee(100), [], candidates);

        Assert.Empty(result);
    }

    [Fact]
    public async Task hr_pendinghr_i_gorur_ama_1_asamayi_kendisi_onayladiysa_gormez()
    {
        var actor = MakeUser(10, Role.HR);
        var candidates = new List<PendingApprovalDto>
        {
            Req(1, LeaveStatus.PendingHr, employeeId: 200, ownerUserId: 20, managerApprovedBy: 99), // başka yönetici → görünür
            Req(2, LeaveStatus.PendingHr, employeeId: 300, ownerUserId: 30, managerApprovedBy: 10), // HR kendisi onayladı → gizli
        };

        var result = await RunAsync(actor, actorEmployee: null, [], candidates);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task hr_yonetici_asamasindaki_pending_i_gormez()
    {
        var actor = MakeUser(10, Role.HR); // ekibi yok
        var candidates = new List<PendingApprovalDto>
        {
            Req(1, LeaveStatus.Pending, employeeId: 200, ownerUserId: 20),
        };

        var result = await RunAsync(actor, actorEmployee: null, [], candidates);

        Assert.Empty(result);
    }

    [Fact]
    public async Task admin_ekibinde_olmayan_pending_i_de_gorur()
    {
        var actor = MakeUser(10, Role.Admin);
        var candidates = new List<PendingApprovalDto>
        {
            Req(1, LeaveStatus.Pending, employeeId: 999, ownerUserId: 20), // ekipte değil ama Admin (kilit çözücü)
        };

        var result = await RunAsync(actor, actorEmployee: null, [], candidates);

        Assert.Single(result);
    }

    [Fact]
    public async Task mentor_stajyer_pending_talebini_gorur_baska_mentorunkini_gormez()
    {
        var actor = MakeUser(10, Role.Manager);
        var candidates = new List<PendingApprovalDto>
        {
            Req(1, LeaveStatus.Pending, internId: 5, ownerUserId: 50, mentorId: 100), // mentoru actor → görünür
            Req(2, LeaveStatus.Pending, internId: 6, ownerUserId: 60, mentorId: 700), // başka mentor → gizli
        };

        var result = await RunAsync(actor, MakeEmployee(100), [], candidates);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private static async Task<IReadOnlyList<PendingApprovalDto>> RunAsync(
        User actor, Employee? actorEmployee, IReadOnlyList<Employee> team, List<PendingApprovalDto> candidates)
    {
        var handler = new GetPendingApprovalsQueryHandler(
            new FakeLeaveRequestRepository(candidates),
            new FakeUserRepository(new Dictionary<int, User> { [actor.Id] = actor }),
            new FakeEmployeeRepository(actor.Id, actorEmployee, [.. team]));

        return await handler.Handle(new GetPendingApprovalsQuery(actor.Id), CancellationToken.None);
    }

    private static User MakeUser(int id, Role role) => new()
    {
        Id = id,
        Role = role,
        IsActive = true,
        Username = $"u{id}",
        Email = $"u{id}@x.com",
        PasswordHash = "x"
    };

    private static Employee MakeEmployee(int id) => new()
    {
        Id = id,
        FirstName = "E",
        LastName = id.ToString(),
        Email = $"e{id}@x.com"
    };

    private static PendingApprovalDto Req(
        int id, LeaveStatus status, int? employeeId = null, int? internId = null,
        int? ownerUserId = null, int? mentorId = null, int? managerApprovedBy = null) => new()
    {
        Id = id,
        Status = status,
        EmployeeId = employeeId,
        InternId = internId,
        OwnerUserId = ownerUserId,
        MentorId = mentorId,
        ManagerApprovedByUserId = managerApprovedBy,
        SubjectName = "X",
        SubjectType = employeeId is not null ? "Çalışan" : "Stajyer",
        TypeName = "Annual"
    };

    // ── Fake'ler ───────────────────────────────────────────────────────────────

    private sealed class FakeLeaveRequestRepository(List<PendingApprovalDto> candidates) : ILeaveRequestRepository
    {
        public Task<IEnumerable<PendingApprovalDto>> GetActionableWithNamesAsync() =>
            Task.FromResult<IEnumerable<PendingApprovalDto>>(candidates);

        public Task<LeaveRequest?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetAllAsync() => throw new NotImplementedException();
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

    private sealed class FakeUserRepository(Dictionary<int, User> users) : IUserRepository
    {
        public Task<User?> GetByIdAsync(int id) => Task.FromResult(users.TryGetValue(id, out var u) ? u : null);

        public Task<User?> GetByUsernameAsync(string username) => throw new NotImplementedException();
        public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<IEnumerable<User>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(User user) => throw new NotImplementedException();
        public Task UpdateAsync(User user) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task<int> CreateForPersonAsync(User user, int? employeeId, int? internId, int? accountRequestId, int? reviewerUserId) => throw new NotImplementedException();
    }

    private sealed class FakeEmployeeRepository(int actorUserId, Employee? actorEmployee, List<Employee> team) : IEmployeeRepository
    {
        public Task<Employee?> GetByUserIdAsync(int userId) =>
            Task.FromResult(userId == actorUserId ? actorEmployee : null);

        public Task<IEnumerable<Employee>> GetTeamAsync(int managerEmployeeId) =>
            Task.FromResult<IEnumerable<Employee>>(team);

        public Task<Employee?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Employee>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(Employee employee) => throw new NotImplementedException();
        public Task UpdateAsync(Employee employee) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task DeleteWithAccountAsync(int employeeId, int? userId) => throw new NotImplementedException();
        public Task<bool> ExistsByDepartmentIdAsync(int departmentId) => throw new NotImplementedException();
        public Task<bool> ExistsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<bool> ExistsByManagerIdAsync(int managerId) => throw new NotImplementedException();
        public Task<Employee?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<bool> IsInManagerChainAsync(int managerEmployeeId, int subordinateEmployeeId) => throw new NotImplementedException();
    }
}
