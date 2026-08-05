using System.ComponentModel.DataAnnotations;
using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Interns.Commands.CreateIntern;
using HRManagement.Application.Features.Interns.Commands.UpdateIntern;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Tests.Features.Interns;

/// <summary>
/// Stajyer e-posta benzersizliği — çalışan tarafındaki kuralın birebir aynısı.
///
/// Çalışanda kural üç katmanla korunuyordu (DB kısıtı + handler ön kontrolü +
/// güncellemede "kendi kaydı hariç"); stajyerde HİÇBİRİ yoktu. E-posta hesap
/// açma akışının kimlik anahtarı olduğu için mükerrer kayıt "bu adres kime ait?"
/// sorusunu belirsizleştiriyordu.
/// </summary>
public class InternEmailUniquenessTests
{
    private const string MevcutEmail = "mevcut@example.com";
    private const int MevcutInternId = 1;
    private const int DigerInternId = 2;

    // ── Ekleme ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ayni_eposta_ile_ikinci_stajyer_eklenemez()
    {
        var handler = CreateHandler();

        // Baştaki/sondaki boşluk kuralı atlatmamalı: handler Trim'liyor.
        var command = ValidCreateCommand() with { Email = "  " + MevcutEmail + "  " };

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Kullanilmamis_eposta_ile_stajyer_eklenebilir()
    {
        var handler = CreateHandler();

        var id = await handler.Handle(
            ValidCreateCommand() with { Email = "yeni@example.com" }, CancellationToken.None);

        Assert.Equal(99, id);
    }

    // ── Güncelleme ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Guncellemede_baskasinin_epostasi_alinamaz()
    {
        var handler = CreateUpdateHandler();

        // DigerInternId, MevcutInternId'nin adresini almaya çalışıyor.
        var command = ValidUpdateCommand() with { Id = DigerInternId, Email = MevcutEmail };

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Guncellemede_kisi_kendi_epostasini_koruyabilir()
    {
        // "Kendi kaydı hariç" olmasaydı hiç kimse kendi kaydını e-postasına
        // dokunmadan güncelleyemezdi — kural kendi kendini kilitlerdi.
        var handler = CreateUpdateHandler();

        await handler.Handle(
            ValidUpdateCommand() with { Id = MevcutInternId, Email = MevcutEmail },
            CancellationToken.None);
    }

    // ── Kurulum ──────────────────────────────────────────────────────────────

    private static CreateInternCommandHandler CreateHandler() => new(
        new FakeInternRepository(), new FakeAccountRequestRepository(), new FakeUnitRepository());

    private static UpdateInternCommandHandler CreateUpdateHandler() => new(
        new FakeInternRepository(), new FakeUnitRepository());

    private static CreateInternCommand ValidCreateCommand() => new(
        FirstName: "Ayşe",
        LastName: "Yılmaz",
        Email: "yeni@example.com",
        University: "Örnek Üniversitesi",
        Major: "Bilgisayar Mühendisliği",
        Grade: 3,
        StartDate: new DateTime(2026, 6, 1),
        EndDate: new DateTime(2026, 9, 1),
        MentorId: null,
        DepartmentId: 1,
        UnitId: null,
        CreatedByUserId: 1,
        RequestLoginAccount: false);

    private static UpdateInternCommand ValidUpdateCommand() => new(
        Id: DigerInternId,
        FirstName: "Ayşe",
        LastName: "Yılmaz",
        Email: "yeni@example.com",
        University: "Örnek Üniversitesi",
        Major: "Bilgisayar Mühendisliği",
        Grade: 3,
        StartDate: new DateTime(2026, 6, 1),
        EndDate: new DateTime(2026, 9, 1),
        MentorId: null,
        DepartmentId: 1,
        UnitId: null);

    // ── Fake'ler ─────────────────────────────────────────────────────────────

    /// <summary>Tek kayıt: MevcutInternId, MevcutEmail adresiyle.</summary>
    private sealed class FakeInternRepository : IInternRepository
    {
        private readonly Intern _mevcut = new()
        {
            Id = MevcutInternId,
            Email = MevcutEmail,
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 9, 1)
        };

        private readonly Intern _diger = new()
        {
            Id = DigerInternId,
            Email = "diger@example.com",
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 9, 1)
        };

        public Task<Intern?> GetByEmailAsync(string email) =>
            Task.FromResult<Intern?>(email == MevcutEmail ? _mevcut : null);

        public Task<Intern?> GetByIdAsync(int id) =>
            Task.FromResult<Intern?>(id == MevcutInternId ? _mevcut : id == DigerInternId ? _diger : null);

        public Task<int> AddAsync(Intern intern) => Task.FromResult(99);
        public Task UpdateAsync(Intern intern) => Task.CompletedTask;

        public Task<IEnumerable<Intern>> GetAllAsync() => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task DeleteWithAccountAsync(int internId, int? userId) => throw new NotImplementedException();
        public Task<bool> ExistsByDepartmentIdAsync(int departmentId) => throw new NotImplementedException();
        public Task<bool> ExistsByMentorIdAsync(int mentorId) => throw new NotImplementedException();
        public Task<bool> ExistsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<Intern?> GetByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<IEnumerable<Intern>> GetByMentorIdAsync(int mentorEmployeeId) => throw new NotImplementedException();
    }

    private sealed class FakeAccountRequestRepository : IAccountRequestRepository
    {
        public Task<int> AddAsync(AccountRequest request) => Task.FromResult(1);

        public Task<AccountRequest?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task UpdateAsync(AccountRequest request) => throw new NotImplementedException();
        public Task<bool> HasPendingAsync(int? employeeId, int? internId) => throw new NotImplementedException();
        public Task<bool> ExistsForEmployeeAsync(int employeeId) => throw new NotImplementedException();
        public Task<bool> ExistsForInternAsync(int internId) => throw new NotImplementedException();
        public Task<IEnumerable<AccountRequestDto>> GetPendingWithNamesAsync() => throw new NotImplementedException();
    }

    // Testlerde birim seçilmiyor (UnitId null); birim-departman kuralı tetiklenmez.
    private sealed class FakeUnitRepository : IUnitRepository
    {
        public Task<IEnumerable<Domain.Entities.Unit>> GetAllAsync() => throw new NotImplementedException();
        public Task<Domain.Entities.Unit?> GetByIdAsync(int id) => throw new NotImplementedException();
    }
}
