using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.LeaveRequests.Commands.DeleteLeaveRequest;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Features.LeaveRequests;

/// <summary>
/// İptal / geri çekme kural matrisi (kullanıcı kararı, 2026-08-03):
///
///                    izin BAŞLAMADI          izin BAŞLADI
///   Pending/PendingHr → kayıt SİLİNİR          → reddedilir
///   Approved          → Cancelled'a çekilir    → reddedilir
///   Rejected/Cancelled → reddedilir             → reddedilir
///
/// Admin her talebi silebilir (yönetimsel temizlik) — tarih/durum bakılmaz.
/// </summary>
public class DeleteLeaveRequestCommandHandlerTests
{
    private const int AdminUserId = 1;
    private const int OwnerUserId = 20;
    private const int OtherUserId = 30;
    private const int OwnerEmployeeId = 5;
    private const int RequestId = 42;

    private static readonly DateTime Gelecek = DateTime.UtcNow.Date.AddDays(7);
    private static readonly DateTime Bugun = DateTime.UtcNow.Date;

    private static (DeleteLeaveRequestCommandHandler Handler, FakeLeaveRequestRepository Repo)
        CreateHandler(LeaveStatus status, DateTime startDate)
    {
        var users = new Dictionary<int, User>
        {
            [AdminUserId] = new() { Id = AdminUserId, Role = Role.Admin, IsActive = true },
            [OwnerUserId] = new() { Id = OwnerUserId, Role = Role.Employee, IsActive = true },
            [OtherUserId] = new() { Id = OtherUserId, Role = Role.Employee, IsActive = true }
        };

        var leaveRequest = new LeaveRequest
        {
            Id = RequestId,
            EmployeeId = OwnerEmployeeId,
            Type = LeaveType.Annual,
            Status = status,
            StartDate = startDate,
            EndDate = startDate.AddDays(3)
        };

        var leaveRepository = new FakeLeaveRequestRepository(leaveRequest);

        var handler = new DeleteLeaveRequestCommandHandler(
            leaveRepository,
            new FakeUserRepository(users),
            new FakeEmployeeRepository(new Employee { Id = OwnerEmployeeId, UserId = OwnerUserId }),
            new FakeInternRepository());

        return (handler, leaveRepository);
    }

    [Theory]
    [InlineData(LeaveStatus.Pending)]
    [InlineData(LeaveStatus.PendingHr)]
    public async Task onaysiz_ve_baslamamis_talep_direkt_silinir(LeaveStatus status)
    {
        var (handler, repo) = CreateHandler(status, Gelecek);

        await handler.Handle(new DeleteLeaveRequestCommand(RequestId, OwnerUserId), CancellationToken.None);

        Assert.Equal(RequestId, repo.DeletedId);   // kayıt gerçekten silindi
        Assert.Null(repo.Updated);                 // statü güncellemesi YOK
    }

    [Fact]
    public async Task onayli_ve_baslamamis_talep_silinmez_geri_cekilir()
    {
        var (handler, repo) = CreateHandler(LeaveStatus.Approved, Gelecek);

        await handler.Handle(new DeleteLeaveRequestCommand(RequestId, OwnerUserId), CancellationToken.None);

        Assert.Null(repo.DeletedId);                               // kayıt DURUYOR (denetim izi)
        Assert.Equal(LeaveStatus.Cancelled, repo.Updated!.Status); // günler bakiyeye döner
    }

    [Theory]
    [InlineData(LeaveStatus.Pending)]
    [InlineData(LeaveStatus.PendingHr)]
    [InlineData(LeaveStatus.Approved)]
    public async Task izne_girilmisse_hicbir_sey_yapilamaz(LeaveStatus status)
    {
        // Başlangıç günü BUGÜN = izne girilmiş sayılır (gün başlamıştır).
        var (handler, repo) = CreateHandler(status, Bugun);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new DeleteLeaveRequestCommand(RequestId, OwnerUserId), CancellationToken.None));

        Assert.Null(repo.DeletedId);
        Assert.Null(repo.Updated);
    }

    [Theory]
    [InlineData(LeaveStatus.Rejected)]
    [InlineData(LeaveStatus.Cancelled)]
    public async Task sonuclanmis_talep_iptal_edilemez(LeaveStatus status)
    {
        var (handler, _) = CreateHandler(status, Gelecek);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new DeleteLeaveRequestCommand(RequestId, OwnerUserId), CancellationToken.None));
    }

    [Fact]
    public async Task baskasinin_talebi_iptal_edilemez()
    {
        var (handler, repo) = CreateHandler(LeaveStatus.Pending, Gelecek);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new DeleteLeaveRequestCommand(RequestId, OtherUserId), CancellationToken.None));

        Assert.Null(repo.DeletedId);
    }

    [Fact]
    public async Task admin_baslamis_onayli_talebi_bile_silebilir()
    {
        // Yönetimsel temizlik istisnası: tarih/durum kuralları Admin'e uygulanmaz.
        var (handler, repo) = CreateHandler(LeaveStatus.Approved, Bugun);

        await handler.Handle(new DeleteLeaveRequestCommand(RequestId, AdminUserId), CancellationToken.None);

        Assert.Equal(RequestId, repo.DeletedId);
    }

    // ── Fake'ler ─────────────────────────────────────────────────────────────

    private sealed class FakeLeaveRequestRepository(LeaveRequest request) : ILeaveRequestRepository
    {
        public int? DeletedId { get; private set; }
        public LeaveRequest? Updated { get; private set; }

        public Task<LeaveRequest?> GetByIdAsync(int id) =>
            Task.FromResult<LeaveRequest?>(id == request.Id ? request : null);

        public Task DeleteAsync(int id)
        {
            DeletedId = id;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(LeaveRequest leaveRequest)
        {
            Updated = leaveRequest;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<LeaveRequest>> GetAllAsync() => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId) => throw new NotImplementedException();
        public Task<int> AddAsync(LeaveRequest leaveRequest) => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetByInternIdAsync(int internId) => throw new NotImplementedException();
        public Task<bool> ExistsByEmployeeIdAsync(int employeeId) => throw new NotImplementedException();
        public Task<bool> ExistsByInternIdAsync(int internId) => throw new NotImplementedException();
        public Task<bool> HasOverlapAsync(int? employeeId, int? internId, DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<int> GetTotalUsedAnnualDaysAsync(int employeeId) => throw new NotImplementedException();
        public Task<IEnumerable<HRManagement.Application.DTOs.PendingApprovalDto>> GetActionableWithNamesAsync() => throw new NotImplementedException();
        public Task<IEnumerable<HRManagement.Application.DTOs.LeaveHistoryDto>> GetAllWithNamesAsync() => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<int, int>> GetUsedAnnualDaysByEmployeeAsync() => throw new NotImplementedException();
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

        public Task<Employee?> GetByUserIdAsync(int userId) => Task.FromResult<Employee?>(null);

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
        public Task<IEnumerable<Intern>> GetByMentorIdAsync(int mentorEmployeeId) => throw new NotImplementedException();
    }
}
