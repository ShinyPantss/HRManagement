using HRManagement.Application.Features.LeaveRequests.Commands.CreateLeaveRequest;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Features.LeaveRequests;

/// <summary>
/// Talebin BAŞLANGIÇ DURUMU seçiminin testleri. Yönetici aşamasını atlayan
/// üç hâl vardır (kullanıcı kararları):
///   - Hastalık izni → hasta insan yönetici onayı bekleyemez (2026-07-23)
///   - Zincirin tepesindeki kişi (ManagerId yok = GM) → onaylayacak üstü yok,
///     talep doğrudan İK'ya düşer, Admin'e hiç gitmez (2026-08-03)
///   - MENTORU OLMAYAN stajyer → aynı gerekçe; atlanmasaydı talep hiç kimsenin
///     onaylayamadığı ve İK listesinde görünmeyen bir Pending'de kilitlenirdi
/// Diğer herkes normal iki aşamalı akışa (Pending) girer.
/// </summary>
public class CreateLeaveRequestCommandHandlerTests
{
    private const int RequesterUserId = 20;
    private const int RequesterEmployeeId = 5;
    private const int InternUserId = 21;
    private const int InternId = 7;

    private static readonly DateTime Baslangic = DateTime.UtcNow.Date.AddDays(14);
    private static readonly DateTime Isbasi = DateTime.UtcNow.Date.AddDays(17);

    private static (CreateLeaveRequestCommandHandler Handler, FakeLeaveRequestRepository Repo)
        CreateHandler(int? managerId)
    {
        var employee = new Employee
        {
            Id = RequesterEmployeeId,
            UserId = RequesterUserId,
            ManagerId = managerId,
            IsActive = true,
            HireDate = DateTime.UtcNow.Date.AddYears(-3)
        };

        var leaveRepository = new FakeLeaveRequestRepository();

        var handler = new CreateLeaveRequestCommandHandler(
            leaveRepository,
            new FakeEmployeeRepository(employee),
            new FakeInternRepository());

        return (handler, leaveRepository);
    }

    /// <summary>Talep sahibi STAJYER: çalışan kaydı yok, kimlik stajyer tarafına çözülür.</summary>
    private static (CreateLeaveRequestCommandHandler Handler, FakeLeaveRequestRepository Repo)
        CreateInternHandler(int? mentorId)
    {
        var intern = new Intern
        {
            Id = InternId,
            UserId = InternUserId,
            MentorId = mentorId,
            StartDate = DateTime.UtcNow.Date.AddMonths(-2),
            EndDate = DateTime.UtcNow.Date.AddMonths(2)
        };

        var leaveRepository = new FakeLeaveRequestRepository();

        var handler = new CreateLeaveRequestCommandHandler(
            leaveRepository,
            new FakeEmployeeRepository(employee: null),
            new FakeInternRepository(intern));

        return (handler, leaveRepository);
    }

    // Ücretsiz izin: bakiye kontrolü tetiklenmez, test yalnızca durum seçimine odaklanır.
    private static CreateLeaveRequestCommand UnpaidCommand() => new(
        RequesterUserId, LeaveType.Unpaid, Baslangic, Isbasi,
        Description: null, MedicalReport: null);

    // Stajyer yıllık izin hakkı biriktirmez; ücretsiz izin onun tek yoludur.
    private static CreateLeaveRequestCommand InternUnpaidCommand() =>
        UnpaidCommand() with { RequesterUserId = InternUserId };

    [Fact]
    public async Task yoneticisi_olan_calisanin_talebi_yonetici_onayina_duser()
    {
        var (handler, repo) = CreateHandler(managerId: 3);

        await handler.Handle(UnpaidCommand(), CancellationToken.None);

        Assert.Equal(LeaveStatus.Pending, repo.Added!.Status);
    }

    [Fact]
    public async Task zincirin_tepesindeki_kisinin_talebi_dogrudan_IK_asamasina_duser()
    {
        // GM: ManagerId yok. Yönetici aşaması atlanır — Admin'e hiç gitmez,
        // tek onay İK'dan gelir.
        var (handler, repo) = CreateHandler(managerId: null);

        await handler.Handle(UnpaidCommand(), CancellationToken.None);

        Assert.Equal(LeaveStatus.PendingHr, repo.Added!.Status);
    }

    [Fact]
    public async Task hastalik_izni_yonetici_asamasini_atlar()
    {
        var (handler, repo) = CreateHandler(managerId: 3);

        var command = UnpaidCommand() with
        {
            Type = LeaveType.Sick,
            MedicalReport = "Rapor no 123"
        };

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(LeaveStatus.PendingHr, repo.Added!.Status);
    }

    [Fact]
    public async Task mentoru_olan_stajyerin_talebi_mentor_onayina_duser()
    {
        var (handler, repo) = CreateInternHandler(mentorId: RequesterEmployeeId);

        await handler.Handle(InternUnpaidCommand(), CancellationToken.None);

        Assert.Equal(LeaveStatus.Pending, repo.Added!.Status);
    }

    [Fact]
    public async Task mentoru_olmayan_stajyerin_talebi_dogrudan_IK_asamasina_duser()
    {
        // MentorId zorunlu değil (ne validator'da ne veritabanında). Pending
        // doğsaydı onaylayacak mentor olmadığı için talep hem reddedilir hem de
        // İK'nın "Onay Bekleyenler" listesinde görünmezdi — kilitlenirdi.
        var (handler, repo) = CreateInternHandler(mentorId: null);

        await handler.Handle(InternUnpaidCommand(), CancellationToken.None);

        Assert.Equal(LeaveStatus.PendingHr, repo.Added!.Status);
    }

    // ── Fake'ler ─────────────────────────────────────────────────────────────

    private sealed class FakeLeaveRequestRepository : ILeaveRequestRepository
    {
        public LeaveRequest? Added { get; private set; }

        public Task<int> AddAsync(LeaveRequest leaveRequest)
        {
            Added = leaveRequest;
            return Task.FromResult(1);
        }

        public Task<bool> HasOverlapAsync(int? employeeId, int? internId, DateTime startDate, DateTime endDate) =>
            Task.FromResult(false);

        public Task<int> GetTotalUsedAnnualDaysAsync(int employeeId) => Task.FromResult(0);

        public Task<LeaveRequest?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetAllAsync() => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId) => throw new NotImplementedException();
        public Task<IEnumerable<LeaveRequest>> GetByInternIdAsync(int internId) => throw new NotImplementedException();
        public Task UpdateAsync(LeaveRequest leaveRequest) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task<bool> ExistsByEmployeeIdAsync(int employeeId) => throw new NotImplementedException();
        public Task<bool> ExistsByInternIdAsync(int internId) => throw new NotImplementedException();
        public Task<IEnumerable<HRManagement.Application.DTOs.PendingApprovalDto>> GetActionableWithNamesAsync() => throw new NotImplementedException();
        public Task<IEnumerable<HRManagement.Application.DTOs.LeaveHistoryDto>> GetAllWithNamesAsync() => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<int, int>> GetUsedAnnualDaysByEmployeeAsync() => throw new NotImplementedException();
    }

    // employee null = hesap bir çalışan kaydına bağlı değil (stajyer senaryosu).
    private sealed class FakeEmployeeRepository(Employee? employee) : IEmployeeRepository
    {
        public Task<Employee?> GetByUserIdAsync(int userId) =>
            Task.FromResult(employee is not null && userId == employee.UserId ? employee : null);

        public Task<Employee?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Employee>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(Employee e) => throw new NotImplementedException();
        public Task UpdateAsync(Employee e) => throw new NotImplementedException();
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

    // intern null = çalışan senaryosu; kimlik çalışana çözüldüğü için buraya hiç inilmez.
    private sealed class FakeInternRepository(Intern? intern = null) : IInternRepository
    {
        public Task<Intern?> GetByUserIdAsync(int userId) =>
            Task.FromResult(intern is not null && userId == intern.UserId ? intern : null);

        public Task<Intern?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Intern>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(Intern intern) => throw new NotImplementedException();
        public Task UpdateAsync(Intern intern) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task DeleteWithAccountAsync(int internId, int? userId) => throw new NotImplementedException();
        public Task<bool> ExistsByDepartmentIdAsync(int departmentId) => throw new NotImplementedException();
        public Task<bool> ExistsByMentorIdAsync(int mentorId) => throw new NotImplementedException();
        public Task<bool> ExistsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<Intern?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<IEnumerable<Intern>> GetByMentorIdAsync(int mentorEmployeeId) => throw new NotImplementedException();
    }
}
