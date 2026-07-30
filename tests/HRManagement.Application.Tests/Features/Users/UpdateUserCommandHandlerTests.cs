using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.Users.Commands.UpdateUser;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Features.Users;

/// <summary>
/// Hesap güncellemenin KİLİTLENME kurallarını doğrular. İkisi de aynı felakete
/// karşı durur: sistemde giriş yapabilen Admin kalmazsa hesap yönetimine kimse
/// giremez ve uygulama kendi içinden onarılamaz.
///
///   • Kimse KENDİ rolünü/erişimini değiştiremez → işlem başka bir Admin'e düşer.
///   • Son AKTİF Admin korunur → sayı sıfıra inemez.
/// </summary>
public class UpdateUserCommandHandlerTests
{
    private const int AdminUserId = 1;
    private const int OtherAdminUserId = 2;
    private const int EmployeeUserId = 3;

    // ── Kendi erişimini değiştirme ───────────────────────────────────────────

    [Fact]
    public async Task Kisi_kendi_rolunu_degistiremez()
    {
        var handler = CreateHandler(activeAdminCount: 5);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(
                Command(AdminUserId, Role.Employee, isActive: true, currentUserId: AdminUserId),
                CancellationToken.None));

        Assert.Equal("Kendi rolünüzü değiştiremezsiniz.", exception.Message);
    }

    [Fact]
    public async Task Kisi_kendi_hesabini_pasife_alamaz()
    {
        var handler = CreateHandler(activeAdminCount: 5);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(
                Command(AdminUserId, Role.Admin, isActive: false, currentUserId: AdminUserId),
                CancellationToken.None));

        Assert.Equal("Kendi hesabınızı pasife alamazsınız.", exception.Message);
    }

    [Fact]
    public async Task Kisi_kendi_epostasini_degistirebilir()
    {
        var repository = CreateRepository(activeAdminCount: 1);
        var handler = new UpdateUserCommandHandler(repository);

        await handler.Handle(
            new UpdateUserCommand(AdminUserId, "yeni@sirket.com", Role.Admin, true, AdminUserId),
            CancellationToken.None);

        // Rol ve erişim korunurken e-posta güncellenmeli — kilit yalnızca YETKİYE.
        Assert.Equal("yeni@sirket.com", repository.Updated!.Email);
        Assert.Equal(Role.Admin, repository.Updated.Role);
        Assert.True(repository.Updated.IsActive);
    }

    // ── Son yöneticinin korunması ────────────────────────────────────────────

    [Fact]
    public async Task Son_aktif_admin_pasife_alinamaz()
    {
        var handler = CreateHandler(activeAdminCount: 1);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(
                // Başka bir Admin işlemi yapıyor ki "kendi hesabı" kuralına takılmasın.
                Command(AdminUserId, Role.Admin, isActive: false, currentUserId: OtherAdminUserId),
                CancellationToken.None));

        Assert.Contains("son aktif yönetici", exception.Message);
    }

    [Fact]
    public async Task Son_aktif_adminin_rolu_dusurulemez()
    {
        var handler = CreateHandler(activeAdminCount: 1);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(
                Command(AdminUserId, Role.HR, isActive: true, currentUserId: OtherAdminUserId),
                CancellationToken.None));

        Assert.Contains("son aktif yönetici", exception.Message);
    }

    [Fact]
    public async Task Ikinci_admin_varken_biri_pasife_alinabilir()
    {
        var repository = CreateRepository(activeAdminCount: 2);
        var handler = new UpdateUserCommandHandler(repository);

        await handler.Handle(
            Command(AdminUserId, Role.Admin, isActive: false, currentUserId: OtherAdminUserId),
            CancellationToken.None);

        Assert.False(repository.Updated!.IsActive);
    }

    [Fact]
    public async Task Admin_olmayan_hesap_icin_yonetici_sayimi_yapilmaz()
    {
        // Sayım yalnızca bir Admin'i admin'likten çıkaran değişikliklerde anlamlı;
        // gereksiz çağrı repository'ye yük bindirir ve kuralı bulanıklaştırır.
        var repository = new FakeUserRepository(
            new User { Id = EmployeeUserId, Email = "e@sirket.com", Role = Role.Employee, IsActive = true },
            activeAdminCount: 1);

        await new UpdateUserCommandHandler(repository).Handle(
            Command(EmployeeUserId, Role.Manager, isActive: true, currentUserId: OtherAdminUserId),
            CancellationToken.None);

        Assert.False(repository.CountWasCalled);
        Assert.Equal(Role.Manager, repository.Updated!.Role);
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private static UpdateUserCommand Command(int id, Role role, bool isActive, int currentUserId) =>
        new(id, "admin@sirket.com", role, isActive, currentUserId);

    private static UpdateUserCommandHandler CreateHandler(int activeAdminCount) =>
        new(CreateRepository(activeAdminCount));

    private static FakeUserRepository CreateRepository(int activeAdminCount) =>
        new(new User
        {
            Id = AdminUserId,
            Email = "admin@sirket.com",
            Role = Role.Admin,
            IsActive = true
        }, activeAdminCount);

    private sealed class FakeUserRepository(User user, int activeAdminCount) : IUserRepository
    {
        public User? Updated { get; private set; }
        public bool CountWasCalled { get; private set; }

        public Task<User?> GetByIdAsync(int id) =>
            Task.FromResult<User?>(id == user.Id ? user : null);

        // E-posta serbest: çakışma kuralı bu testlerin konusu değil.
        public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);

        public Task<int> CountActiveAdminsAsync()
        {
            CountWasCalled = true;
            return Task.FromResult(activeAdminCount);
        }

        public Task UpdateAsync(User updated)
        {
            Updated = updated;
            return Task.CompletedTask;
        }

        public Task<User?> GetByUsernameAsync(string username) => throw new NotImplementedException();
        public Task<IEnumerable<User>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(User user) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task<int> CreateForPersonAsync(User user, int? employeeId, int? internId, int? accountRequestId, int? reviewerUserId)
            => throw new NotImplementedException();
    }
}
