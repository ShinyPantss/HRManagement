using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.Employees.Commands.AddEmployeeNote;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Features.Employees;

/// <summary>
/// Not ekleme yetki kurallarının testleri (§5.2 "HR veya Yönetici girer"):
/// HR herkese; Manager yalnızca kendi zincirindeki ekibine (kendi kaydına da
/// değil); Admin dahil diğer roller ekleyemez.
/// </summary>
public class AddEmployeeNoteCommandHandlerTests
{
    private const int HrUserId = 10;
    private const int AdminUserId = 11;
    private const int ManagerUserId = 12;
    private const int TargetEmployeeId = 1;
    private const int ManagerEmployeeId = 2;

    private static (AddEmployeeNoteCommandHandler Handler, FakeEmployeeNoteRepository Notes) CreateHandler(
        bool managerHasTargetInChain)
    {
        var users = new Dictionary<int, User>
        {
            [HrUserId] = new() { Id = HrUserId, Role = Role.HR, IsActive = true },
            [AdminUserId] = new() { Id = AdminUserId, Role = Role.Admin, IsActive = true },
            [ManagerUserId] = new() { Id = ManagerUserId, Role = Role.Manager, IsActive = true }
        };

        var target = new Employee { Id = TargetEmployeeId, FirstName = "Ayşe", LastName = "Yılmaz" };
        var managerEmployee = new Employee { Id = ManagerEmployeeId, UserId = ManagerUserId };

        var noteRepository = new FakeEmployeeNoteRepository();
        var handler = new AddEmployeeNoteCommandHandler(
            noteRepository,
            new FakeEmployeeRepository(target, managerEmployee, managerHasTargetInChain),
            new FakeUserRepository(users));

        return (handler, noteRepository);
    }

    [Fact]
    public async Task HR_herkese_not_ekleyebilir()
    {
        var (handler, notes) = CreateHandler(managerHasTargetInChain: false);

        await handler.Handle(
            new AddEmployeeNoteCommand(TargetEmployeeId, HrUserId, "  Performansı iyi.  "),
            CancellationToken.None);

        var saved = Assert.Single(notes.Added);
        Assert.Equal(TargetEmployeeId, saved.EmployeeId);
        Assert.Equal(HrUserId, saved.AuthorUserId);          // yazar TOKEN'dan gelen hesap
        Assert.Equal("Performansı iyi.", saved.Content);     // içerik trim'lenir
    }

    [Fact]
    public async Task Admin_not_ekleyemez()
    {
        // Doküman "HR veya Yönetici" der; Admin sistem rolüdür (TC kararıyla aynı çizgi).
        var (handler, _) = CreateHandler(managerHasTargetInChain: false);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new AddEmployeeNoteCommand(TargetEmployeeId, AdminUserId, "Not"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Manager_kendi_ekibine_not_ekleyebilir()
    {
        var (handler, notes) = CreateHandler(managerHasTargetInChain: true);

        await handler.Handle(
            new AddEmployeeNoteCommand(TargetEmployeeId, ManagerUserId, "Haftalık geri bildirim."),
            CancellationToken.None);

        Assert.Single(notes.Added);
    }

    [Fact]
    public async Task Manager_ekibi_disindakine_not_ekleyemez()
    {
        // Yetki rolden değil İLİŞKİDEN doğar: Manager rolü tek başına yetmez.
        var (handler, _) = CreateHandler(managerHasTargetInChain: false);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new AddEmployeeNoteCommand(TargetEmployeeId, ManagerUserId, "Not"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Calisan_yoksa_hata_verir()
    {
        var (handler, _) = CreateHandler(managerHasTargetInChain: false);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new AddEmployeeNoteCommand(999, HrUserId, "Not"),
            CancellationToken.None));
    }

    // ── Fake'ler ─────────────────────────────────────────────────────────────

    private sealed class FakeEmployeeNoteRepository : IEmployeeNoteRepository
    {
        public List<EmployeeNote> Added { get; } = [];

        public Task<int> AddAsync(EmployeeNote note)
        {
            Added.Add(note);
            return Task.FromResult(1);
        }

        public Task<IEnumerable<EmployeeNote>> GetByEmployeeIdAsync(int employeeId) =>
            throw new NotImplementedException();
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
        public Task<int> CreateForPersonAsync(User user, int? employeeId, int? internId, int? accountRequestId, int? reviewerUserId)
            => throw new NotImplementedException();
    }

    private sealed class FakeEmployeeRepository(
        Employee target, Employee managerEmployee, bool managerHasTargetInChain) : IEmployeeRepository
    {
        public Task<Employee?> GetByIdAsync(int id) =>
            Task.FromResult<Employee?>(id == target.Id ? target : null);

        public Task<Employee?> GetByUserIdAsync(int userId) =>
            Task.FromResult<Employee?>(userId == managerEmployee.UserId ? managerEmployee : null);

        public Task<bool> IsInManagerChainAsync(int managerEmployeeId, int subordinateEmployeeId) =>
            Task.FromResult(managerHasTargetInChain);

        public Task<IEnumerable<Employee>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(Employee employee) => throw new NotImplementedException();
        public Task UpdateAsync(Employee employee) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task DeleteWithAccountAsync(int employeeId, int? userId) => throw new NotImplementedException();
        public Task<bool> ExistsByDepartmentIdAsync(int departmentId) => throw new NotImplementedException();
        public Task<bool> ExistsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<bool> ExistsByManagerIdAsync(int managerId) => throw new NotImplementedException();
        public Task<Employee?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<IEnumerable<Employee>> GetTeamAsync(int managerEmployeeId) => throw new NotImplementedException();
    }
}
