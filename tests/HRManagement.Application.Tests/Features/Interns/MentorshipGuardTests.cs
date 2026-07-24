using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.Interns.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Tests.Features.Interns;

/// <summary>
/// Mentorluk yetki kuralının testleri: stajyer üzerinde görev/not işlemi
/// yalnızca MENTORUN hakkıdır. Rol önemsizdir — yetki ilişkiden doğar
/// (Interns.MentorId → istekçinin çalışan kaydı).
/// </summary>
public class MentorshipGuardTests
{
    private const int MentorUserId = 10;
    private const int MentorEmployeeId = 5;
    private const int OtherUserId = 11;
    private const int InternId = 1;

    private static MentorshipGuard CreateGuard()
    {
        var intern = new Intern { Id = InternId, FirstName = "Ali", MentorId = MentorEmployeeId };

        var employeesByUserId = new Dictionary<int, Employee>
        {
            // Mentorun çalışan kaydı; diğer kullanıcının kaydı stajyerin mentoru DEĞİL.
            [MentorUserId] = new() { Id = MentorEmployeeId, UserId = MentorUserId },
            [OtherUserId] = new() { Id = 99, UserId = OtherUserId }
        };

        return new MentorshipGuard(
            new FakeInternRepository(intern),
            new FakeEmployeeRepository(employeesByUserId));
    }

    [Fact]
    public async Task Mentor_kendi_stajyerine_erisebilir()
    {
        var intern = await CreateGuard().EnsureMentorAsync(InternId, MentorUserId);

        Assert.Equal(InternId, intern.Id);
    }

    [Fact]
    public async Task Mentoru_olmayan_calisan_erisemez()
    {
        // Rolü ne olursa olsun: ilişki yoksa yetki yok.
        await Assert.ThrowsAsync<ValidationException>(
            () => CreateGuard().EnsureMentorAsync(InternId, OtherUserId));
    }

    [Fact]
    public async Task Calisan_kaydi_olmayan_hesap_erisemez()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => CreateGuard().EnsureMentorAsync(InternId, 999));
    }

    [Fact]
    public async Task Stajyer_yoksa_hata_verir()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => CreateGuard().EnsureMentorAsync(42, MentorUserId));
    }

    // ── Fake'ler ─────────────────────────────────────────────────────────────

    private sealed class FakeInternRepository(Intern intern) : IInternRepository
    {
        public Task<Intern?> GetByIdAsync(int id) =>
            Task.FromResult<Intern?>(id == intern.Id ? intern : null);

        public Task<IEnumerable<Intern>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(Intern i) => throw new NotImplementedException();
        public Task UpdateAsync(Intern i) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task DeleteWithAccountAsync(int internId, int? userId) => throw new NotImplementedException();
        public Task<bool> ExistsByDepartmentIdAsync(int departmentId) => throw new NotImplementedException();
        public Task<bool> ExistsByMentorIdAsync(int mentorId) => throw new NotImplementedException();
        public Task<bool> ExistsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<Intern?> GetByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<IEnumerable<Intern>> GetByMentorIdAsync(int mentorEmployeeId) => throw new NotImplementedException();
    }

    private sealed class FakeEmployeeRepository(Dictionary<int, Employee> employeesByUserId) : IEmployeeRepository
    {
        public Task<Employee?> GetByUserIdAsync(int userId) =>
            Task.FromResult(employeesByUserId.TryGetValue(userId, out var employee) ? employee : null);

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
        public Task<IEnumerable<Employee>> GetTeamAsync(int managerEmployeeId) => throw new NotImplementedException();
    }
}
