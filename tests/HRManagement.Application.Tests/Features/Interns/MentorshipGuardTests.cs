using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.Interns.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

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
    private const int HrUserId = 20;
    private const int AdminUserId = 21;
    private const int InternId = 1;

    private static MentorshipGuard CreateGuard()
    {
        var intern = new Intern { Id = InternId, FirstName = "Ali", MentorId = MentorEmployeeId };

        var employeesByUserId = new Dictionary<int, Employee>
        {
            // Mentorun çalışan kaydı; diğer kullanıcının kaydı stajyerin mentoru DEĞİL.
            // HR/Admin'in çalışan kaydı YOK (görüntüleme yetkisi rolden gelir).
            [MentorUserId] = new() { Id = MentorEmployeeId, UserId = MentorUserId },
            [OtherUserId] = new() { Id = 99, UserId = OtherUserId }
        };

        var usersById = new Dictionary<int, User>
        {
            [MentorUserId] = new() { Id = MentorUserId, Role = Role.Manager, IsActive = true },
            [OtherUserId] = new() { Id = OtherUserId, Role = Role.Employee, IsActive = true },
            [HrUserId] = new() { Id = HrUserId, Role = Role.HR, IsActive = true },
            [AdminUserId] = new() { Id = AdminUserId, Role = Role.Admin, IsActive = true }
        };

        return new MentorshipGuard(
            new FakeInternRepository(intern),
            new FakeEmployeeRepository(employeesByUserId),
            new FakeUserRepository(usersById));
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

    // ── Görüntüleme: mentor VEYA HR/Admin; yazma yetkisi vermez ──────────────

    [Fact]
    public async Task HR_detayi_gorebilir_ama_gorev_not_ekleyemez()
    {
        var guard = CreateGuard();

        // Salt-okur gözlemci: görüntüleme geçer...
        var intern = await guard.EnsureCanViewAsync(InternId, HrUserId);
        Assert.Equal(InternId, intern.Id);

        // ...ama yazma kuralı hâlâ mentor ister.
        await Assert.ThrowsAsync<ValidationException>(
            () => guard.EnsureMentorAsync(InternId, HrUserId));
    }

    // ── EnsureCanViewAsync: mentor VEYA HR/Admin görüntüleyebilir ─────────────

    [Fact]
    public async Task Mentor_detayi_gorebilir()
    {
        var intern = await CreateGuard().EnsureCanViewAsync(InternId, MentorUserId);
        Assert.Equal(InternId, intern.Id);
    }

    [Fact]
    public async Task Hr_mentoru_olmadan_da_detayi_gorebilir()
    {
        var intern = await CreateGuard().EnsureCanViewAsync(InternId, HrUserId);
        Assert.Equal(InternId, intern.Id);
    }

    [Fact]
    public async Task Admin_detayi_gorebilir()
    {
        var intern = await CreateGuard().EnsureCanViewAsync(InternId, AdminUserId);
        Assert.Equal(InternId, intern.Id);
    }

    [Fact]
    public async Task Ilgisiz_calisan_detayi_goremez()
    {
        // Employee rolünde ve mentoru değil → görüntüleyemez.
        await Assert.ThrowsAsync<ValidationException>(
            () => CreateGuard().EnsureCanViewAsync(InternId, OtherUserId));
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

    private sealed class FakeUserRepository(Dictionary<int, User> usersById) : IUserRepository
    {
        public Task<User?> GetByIdAsync(int id) =>
            Task.FromResult(usersById.TryGetValue(id, out var u) ? u : null);

        public Task<User?> GetByUsernameAsync(string username) => throw new NotImplementedException();
        public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<IEnumerable<User>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(User user) => throw new NotImplementedException();
        public Task UpdateAsync(User user) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task<int> CreateForPersonAsync(User user, int? employeeId, int? internId, int? accountRequestId, int? reviewerUserId) => throw new NotImplementedException();
    }
}
