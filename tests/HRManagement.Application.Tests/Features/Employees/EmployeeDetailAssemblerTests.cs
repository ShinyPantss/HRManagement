using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Features.Employees;

/// <summary>
/// Detay DTO'sundaki HASSAS ALAN KIRPMA kurallarının testleri (kullanıcı kararı,
/// 2026-07-23): T.C. Kimlik yalnızca HR'a gider (Admin dahil kimseye değil);
/// izin açıklamasını kişinin kendisi/HR/Admin görür, Manager göremez.
///
/// Mock kütüphanesi bilinçli olarak yok (proje kuralı: yeni NuGet sorulmadan
/// eklenmez); ihtiyaç duyulan metotları dolduran el yapımı fake'ler kullanılır.
/// </summary>
public class EmployeeDetailAssemblerTests
{
    private const int HrUserId = 10;
    private const int AdminUserId = 11;
    private const int ManagerUserId = 12;
    private const int OwnerUserId = 13;

    private static Employee Target => new()
    {
        Id = 1,
        FirstName = "Ayşe",
        LastName = "Yılmaz",
        NationalId = "12345678901",
        Email = "ayse@sirket.com",
        DateOfBirth = new DateTime(1995, 5, 5),
        HireDate = new DateTime(2020, 1, 1),
        DepartmentId = 1,
        UserId = OwnerUserId,
        ManagerId = null,
        IsActive = true
    };

    private static EmployeeDetailAssembler CreateAssembler()
    {
        var users = new Dictionary<int, User>
        {
            [HrUserId] = new() { Id = HrUserId, Username = "hr.uzman", Role = Role.HR, IsActive = true },
            [AdminUserId] = new() { Id = AdminUserId, Username = "admin", Role = Role.Admin, IsActive = true },
            [ManagerUserId] = new() { Id = ManagerUserId, Username = "yonetici", Role = Role.Manager, IsActive = true },
            [OwnerUserId] = new() { Id = OwnerUserId, Username = "ayse", Role = Role.Employee, IsActive = true }
        };

        var leaveRequests = new List<LeaveRequest>
        {
            new()
            {
                Id = 100,
                EmployeeId = 1,
                Type = LeaveType.Annual,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 6, 5),
                WorkingDays = 5,
                Status = LeaveStatus.Approved,
                Description = "Aile ziyareti"   // mahrem gerekçe — kırpma testlerinin konusu
            }
        };

        var notes = new List<EmployeeNote>
        {
            new()
            {
                Id = 200,
                EmployeeId = 1,
                AuthorUserId = HrUserId,
                Content = "Uyum süreci sorunsuz geçti.",
                CreatedAt = new DateTime(2026, 7, 1)
            }
        };

        return new EmployeeDetailAssembler(
            new FakeUserRepository(users),
            new FakeEmployeeRepository(),
            new FakeDepartmentRepository(new Department { Id = 1, Name = "IT" }),
            new FakeLeaveRequestRepository(leaveRequests, totalUsedAnnualDays: 5),
            new FakeInternRepository(),
            new FakeEmployeeNoteRepository(notes));
    }

    // ── T.C. Kimlik: yalnızca HR ─────────────────────────────────────────────

    [Fact]
    public async Task NationalId_HR_gorur()
    {
        var dto = await CreateAssembler().BuildAsync(Target, HrUserId);

        Assert.Equal("12345678901", dto.NationalId);
    }

    [Fact]
    public async Task NationalId_Admin_dahi_goremez()
    {
        // Admin sistemi yönetir ama kişisel veriye ihtiyacı yok — HR'dan bile
        // dar bir kural olduğu için regresyona karşı ayrıca test ediliyor.
        var dto = await CreateAssembler().BuildAsync(Target, AdminUserId);

        Assert.Null(dto.NationalId);
    }

    [Fact]
    public async Task NationalId_kisinin_kendisi_goremez()
    {
        var dto = await CreateAssembler().BuildAsync(Target, OwnerUserId);

        Assert.Null(dto.NationalId);
    }

    // ── İzin açıklaması: kendisi/HR/Admin evet, Manager hayır ────────────────

    [Fact]
    public async Task IzinAciklamasini_Manager_goremez()
    {
        var dto = await CreateAssembler().BuildAsync(Target, ManagerUserId);

        var leave = Assert.Single(dto.RecentLeaveRequests);
        Assert.Null(leave.Description);          // gerekçe mahrem
        Assert.Equal(5, leave.TotalDays);        // ama talep bilgisi görünür (onay için gerekli)
    }

    [Fact]
    public async Task IzinAciklamasini_kisinin_kendisi_gorur()
    {
        var dto = await CreateAssembler().BuildAsync(Target, OwnerUserId);

        Assert.Equal("Aile ziyareti", Assert.Single(dto.RecentLeaveRequests).Description);
    }

    [Fact]
    public async Task IzinAciklamasini_HR_gorur()
    {
        var dto = await CreateAssembler().BuildAsync(Target, HrUserId);

        Assert.Equal("Aile ziyareti", Assert.Single(dto.RecentLeaveRequests).Description);
    }

    // ── Notlar (§5.2): kişinin kendisi ASLA göremez, HR/Admin/Manager görür ──

    [Fact]
    public async Task Notlari_kisinin_kendisi_goremez()
    {
        var dto = await CreateAssembler().BuildAsync(Target, OwnerUserId);

        // null = panel hiç çizilmez (boş liste DEĞİL — o "görebilir ama not yok" demek).
        Assert.Null(dto.Notes);
    }

    [Fact]
    public async Task Notlari_HR_yazar_adiyla_gorur()
    {
        var dto = await CreateAssembler().BuildAsync(Target, HrUserId);

        var note = Assert.Single(dto.Notes!);
        Assert.Equal("Uyum süreci sorunsuz geçti.", note.Content);
        Assert.Equal("hr.uzman", note.AuthorName);
    }

    [Fact]
    public async Task Notlari_Admin_ve_Manager_gorur()
    {
        Assert.NotNull((await CreateAssembler().BuildAsync(Target, AdminUserId)).Notes);
        Assert.NotNull((await CreateAssembler().BuildAsync(Target, ManagerUserId)).Notes);
    }

    // ── Bakiye: LeaveEntitlement ile tutarlı ─────────────────────────────────

    [Fact]
    public async Task Bakiye_hak_edilenden_kullanilani_duser()
    {
        // HireDate 2020-01-01, bugün 2026 → 6 tam yıl: 5×14 + 1×20 = 90 hak.
        // Fake repo 5 gün kullanılmış diyor → kalan 85.
        var dto = await CreateAssembler().BuildAsync(Target, HrUserId);

        Assert.Equal(dto.AccruedLeaveDays - dto.UsedLeaveDays, dto.RemainingLeaveDays);
        Assert.Equal(5, dto.UsedLeaveDays);
    }

    // ── Fake'ler: yalnızca assembler'ın dokunduğu metotlar gerçek davranır ───

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
        public Task<int> CreateForPersonAsync(User user, int? employeeId, int? internId, int? accountRequestId, int? reviewerUserId)
            => throw new NotImplementedException();
    }

    private sealed class FakeEmployeeRepository : IEmployeeRepository
    {
        public Task<Employee?> GetByIdAsync(int id) => Task.FromResult<Employee?>(null);
        public Task<IEnumerable<Employee>> GetTeamAsync(int managerEmployeeId) =>
            Task.FromResult<IEnumerable<Employee>>([]);

        public Task<IEnumerable<Employee>> GetAllAsync() => throw new NotImplementedException();
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
    }

    private sealed class FakeDepartmentRepository(Department department) : IDepartmentRepository
    {
        public Task<Department?> GetByIdAsync(int id) => Task.FromResult<Department?>(department);

        public Task<IEnumerable<Department>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(Department d) => throw new NotImplementedException();
        public Task UpdateAsync(Department d) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
    }

    private sealed class FakeLeaveRequestRepository(List<LeaveRequest> requests, int totalUsedAnnualDays)
        : ILeaveRequestRepository
    {
        public Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId) =>
            Task.FromResult<IEnumerable<LeaveRequest>>(requests);
        public Task<int> GetTotalUsedAnnualDaysAsync(int employeeId) =>
            Task.FromResult(totalUsedAnnualDays);

        public Task<LeaveRequest?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(LeaveRequest leaveRequest) => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetByInternIdAsync(int internId) => throw new NotImplementedException();
        public Task UpdateAsync(LeaveRequest leaveRequest) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task<bool> ExistsByEmployeeIdAsync(int employeeId) => throw new NotImplementedException();
        public Task<bool> ExistsByInternIdAsync(int internId) => throw new NotImplementedException();
        public Task<bool> HasOverlapAsync(int? employeeId, int? internId, DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<IEnumerable<HRManagement.Application.DTOs.PendingApprovalDto>> GetActionableWithNamesAsync() => throw new NotImplementedException();
    }

    private sealed class FakeEmployeeNoteRepository(List<EmployeeNote> notes) : IEmployeeNoteRepository
    {
        public Task<IEnumerable<EmployeeNote>> GetByEmployeeIdAsync(int employeeId) =>
            Task.FromResult<IEnumerable<EmployeeNote>>(notes);

        public Task<int> AddAsync(EmployeeNote note) => throw new NotImplementedException();
    }

    private sealed class FakeInternRepository : IInternRepository
    {
        public Task<IEnumerable<Intern>> GetByMentorIdAsync(int mentorEmployeeId) =>
            Task.FromResult<IEnumerable<Intern>>([]);

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
    }
}
