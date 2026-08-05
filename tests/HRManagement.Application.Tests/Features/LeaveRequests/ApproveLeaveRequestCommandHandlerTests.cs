using HRManagement.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;
using HRManagement.Application.Features.LeaveRequests.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Tests.Features.LeaveRequests;

/// <summary>
/// Yönetici onayı sonrası durum seçiminin testleri (kullanıcı kararı, 2026-07-23):
/// talep sahibi HR rolündeyse yönetici onayı YETER (doğrudan Approved, İK aşaması
/// atlanır); diğer herkes için akış iki aşamalı kalır (PendingHr).
/// </summary>
public class ApproveLeaveRequestCommandHandlerTests
{
    private const int AdminUserId = 1;
    private const int OwnerUserId = 20;
    private const int OwnerEmployeeId = 5;

    private static (ApproveLeaveRequestCommandHandler Handler, FakeLeaveRequestRepository Repo)
        CreateHandler(
            Role ownerRole,
            LeaveType type = LeaveType.Unpaid,
            LeaveStatus status = LeaveStatus.Pending,
            int usedAnnualDays = 0)
    {
        var users = new Dictionary<int, User>
        {
            [AdminUserId] = new() { Id = AdminUserId, Role = Role.Admin, IsActive = true },
            [OwnerUserId] = new() { Id = OwnerUserId, Role = ownerRole, IsActive = true }
        };

        // 3 yıllık kıdem, yıllık 14 gün → hak edilen 42, avans sınırı 14 (üst sınır 56).
        var ownerEmployee = new Employee
        {
            Id = OwnerEmployeeId,
            UserId = OwnerUserId,
            HireDate = DateTime.UtcNow.Date.AddYears(-3),
            AnnualLeaveDays = 14
        };

        // Varsayılan ücretsiz izin: bakiye yeniden denetimi tetiklenmez,
        // test yalnızca durum geçişine odaklanır.
        var leaveRequest = new LeaveRequest
        {
            Id = 42,
            EmployeeId = OwnerEmployeeId,
            Type = type,
            Status = status
        };

        var userRepository = new FakeUserRepository(users);
        var employeeRepository = new FakeEmployeeRepository(ownerEmployee);
        var leaveRepository = new FakeLeaveRequestRepository(leaveRequest, usedAnnualDays);

        var handler = new ApproveLeaveRequestCommandHandler(
            leaveRepository,
            employeeRepository,
            userRepository,
            new LeaveApprovalGuard(userRepository, employeeRepository, new FakeInternRepository()));

        return (handler, leaveRepository);
    }

    [Fact]
    public async Task HR_calisaninin_talebi_yonetici_onayiyla_dogrudan_onaylanir()
    {
        var (handler, repo) = CreateHandler(ownerRole: Role.HR);

        await handler.Handle(new ApproveLeaveRequestCommand(42, AdminUserId), CancellationToken.None);

        var updated = repo.Updated!;
        Assert.Equal(LeaveStatus.Approved, updated.Status);       // İK aşaması atlandı
        Assert.Equal(AdminUserId, updated.ManagerApprovedByUserId);
        Assert.Null(updated.HrApprovedByUserId);                  // denetim izi: İK onayı YOK
    }

    [Fact]
    public async Task Normal_calisanin_talebi_yonetici_onayiyla_IK_asamasina_gecer()
    {
        var (handler, repo) = CreateHandler(ownerRole: Role.Employee);

        await handler.Handle(new ApproveLeaveRequestCommand(42, AdminUserId), CancellationToken.None);

        Assert.Equal(LeaveStatus.PendingHr, repo.Updated!.Status);
    }

    // ── Bakiye yeniden denetimi: onay anında hâlâ yetiyor mu? ────────────────

    [Theory]
    [InlineData(LeaveStatus.Pending)]
    // Asıl açık burasıydı: yönetici aşamasını atlayan talep (tepe yönetici →
    // doğrudan PendingHr) Pending dalına hiç uğramadığı için denetimsiz onaylanıyordu.
    [InlineData(LeaveStatus.PendingHr)]
    public async Task Bakiye_asilmissa_onay_her_asamada_reddedilir(LeaveStatus status)
    {
        // Üst sınır 42 + 14 = 56 gün; kullanılan+bekleyen 60 → aşılmış.
        var (handler, repo) = CreateHandler(
            ownerRole: Role.Employee, type: LeaveType.Annual, status: status, usedAnnualDays: 60);

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
            () => handler.Handle(new ApproveLeaveRequestCommand(42, AdminUserId), CancellationToken.None));

        Assert.Null(repo.Updated);   // talep kaydedilmedi
    }

    [Theory]
    [InlineData(LeaveStatus.Pending)]
    [InlineData(LeaveStatus.PendingHr)]
    public async Task Bakiye_yetiyorsa_onay_ilerler(LeaveStatus status)
    {
        var (handler, repo) = CreateHandler(
            ownerRole: Role.Employee, type: LeaveType.Annual, status: status, usedAnnualDays: 10);

        await handler.Handle(new ApproveLeaveRequestCommand(42, AdminUserId), CancellationToken.None);

        var expected = status == LeaveStatus.Pending ? LeaveStatus.PendingHr : LeaveStatus.Approved;
        Assert.Equal(expected, repo.Updated!.Status);
    }

    // ── Fake'ler ─────────────────────────────────────────────────────────────

    private sealed class FakeLeaveRequestRepository(LeaveRequest request, int usedAnnualDays = 0)
        : ILeaveRequestRepository
    {
        public LeaveRequest? Updated { get; private set; }

        public Task<LeaveRequest?> GetByIdAsync(int id) =>
            Task.FromResult<LeaveRequest?>(id == request.Id ? request : null);

        public Task<int> GetTotalUsedAnnualDaysAsync(int employeeId) => Task.FromResult(usedAnnualDays);

        public Task UpdateAsync(LeaveRequest leaveRequest)
        {
            Updated = leaveRequest;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<LeaveRequest>> GetAllAsync() => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId) => throw new NotImplementedException();
        public Task<int> AddAsync(LeaveRequest leaveRequest) => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetByInternIdAsync(int internId) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task<bool> ExistsByEmployeeIdAsync(int employeeId) => throw new NotImplementedException();
        public Task<bool> ExistsByInternIdAsync(int internId) => throw new NotImplementedException();
        public Task<bool> HasOverlapAsync(int? employeeId, int? internId, DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<int, int>> GetUsedAnnualDaysByEmployeeAsync() => throw new NotImplementedException();
        public Task<IEnumerable<HRManagement.Application.DTOs.PendingApprovalDto>> GetActionableWithNamesAsync() => throw new NotImplementedException();
        public Task<IEnumerable<HRManagement.Application.DTOs.LeaveHistoryDto>> GetAllWithNamesAsync() => throw new NotImplementedException();
    }

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

    private sealed class FakeEmployeeRepository(Employee owner) : IEmployeeRepository
    {
        public Task<Employee?> GetByIdAsync(int id) =>
            Task.FromResult<Employee?>(id == owner.Id ? owner : null);

        // Admin onaylayıcı zincir kontrolüne girmez; guard yine de çağırırsa
        // "kayıt yok" davranışı güvenli taraftır.
        public Task<Employee?> GetByUserIdAsync(int userId) => Task.FromResult<Employee?>(null);

        public Task<IEnumerable<Employee>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(Employee employee) => throw new NotImplementedException();
        public Task UpdateAsync(Employee employee) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task DeleteWithAccountAsync(int employeeId, int? userId) => throw new NotImplementedException();
        public Task<bool> ExistsByDepartmentIdAsync(int departmentId) => throw new NotImplementedException();
        public Task<bool> ExistsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<bool> ExistsByManagerIdAsync(int managerId) => throw new NotImplementedException();
        public Task<Employee?> GetByNationalIdAsync(string nationalId) => throw new NotImplementedException();
        public Task<Employee?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<bool> IsInManagerChainAsync(int managerEmployeeId, int subordinateEmployeeId) => throw new NotImplementedException();
        public Task<IEnumerable<Employee>> GetTeamAsync(int managerEmployeeId) => throw new NotImplementedException();
    }

    private sealed class FakeInternRepository : IInternRepository
    {
        public Task<Intern?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Intern>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(Intern intern) => throw new NotImplementedException();
        public Task UpdateAsync(Intern intern) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task DeleteWithAccountAsync(int internId, int? userId) => throw new NotImplementedException();
        public Task<bool> ExistsByDepartmentIdAsync(int departmentId) => throw new NotImplementedException();
        public Task<bool> ExistsByMentorIdAsync(int mentorId) => throw new NotImplementedException();
        public Task<bool> ExistsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<Intern?> GetByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<Intern?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<IEnumerable<Intern>> GetByMentorIdAsync(int mentorEmployeeId) => throw new NotImplementedException();
    }
}
