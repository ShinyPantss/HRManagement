using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.Interns.Commands.UpdateMyInternTaskStatus;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Features.Interns;

/// <summary>
/// "Görevlerim" durum güncellemesinin SAHİPLİK kuralları: stajyer yalnızca
/// kendi görevini ilerletebilir; başkasının görevi için "bulunamadı" ile
/// aynı cevabı alır (görevin varlığı sızdırılmaz).
/// </summary>
public class UpdateMyInternTaskStatusCommandHandlerTests
{
    private const int InternUserId = 10;
    private const int InternId = 1;
    private const int OwnTaskId = 100;
    private const int OthersTaskId = 200;

    private static (UpdateMyInternTaskStatusCommandHandler Handler, FakeInternTaskRepository Tasks) CreateHandler()
    {
        var intern = new Intern { Id = InternId, UserId = InternUserId };

        var tasks = new FakeInternTaskRepository(new Dictionary<int, InternTask>
        {
            [OwnTaskId] = new() { Id = OwnTaskId, InternId = InternId, Status = InternTaskStatus.Pending },
            [OthersTaskId] = new() { Id = OthersTaskId, InternId = 99, Status = InternTaskStatus.Pending }
        });

        return (new UpdateMyInternTaskStatusCommandHandler(new FakeInternRepository(intern), tasks), tasks);
    }

    [Fact]
    public async Task Stajyer_kendi_gorevini_ilerletebilir()
    {
        var (handler, tasks) = CreateHandler();

        await handler.Handle(
            new UpdateMyInternTaskStatusCommand(OwnTaskId, InternUserId, (int)InternTaskStatus.Done),
            CancellationToken.None);

        Assert.Equal(InternTaskStatus.Done, tasks.Updated!.Status);
    }

    [Fact]
    public async Task Baskasinin_gorevine_dokunamaz_ve_varligi_sizdirilmaz()
    {
        var (handler, _) = CreateHandler();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new UpdateMyInternTaskStatusCommand(OthersTaskId, InternUserId, (int)InternTaskStatus.Done),
            CancellationToken.None));

        // "Yetkiniz yok" DEĞİL "bulunamadı": id yoklayarak görev varlığı öğrenilemez.
        Assert.Equal("Görev bulunamadı.", exception.Message);
    }

    [Fact]
    public async Task Stajyer_kaydi_olmayan_hesap_islem_yapamaz()
    {
        var (handler, _) = CreateHandler();

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new UpdateMyInternTaskStatusCommand(OwnTaskId, 999, (int)InternTaskStatus.Done),
            CancellationToken.None));
    }

    // ── Fake'ler ─────────────────────────────────────────────────────────────

    private sealed class FakeInternTaskRepository(Dictionary<int, InternTask> tasks) : IInternTaskRepository
    {
        public InternTask? Updated { get; private set; }

        public Task<InternTask?> GetByIdAsync(int id) =>
            Task.FromResult(tasks.TryGetValue(id, out var task) ? task : null);

        public Task UpdateAsync(InternTask task)
        {
            Updated = task;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<InternTask>> GetByInternIdAsync(int internId) => throw new NotImplementedException();
        public Task<int> AddAsync(InternTask task) => throw new NotImplementedException();
    }

    private sealed class FakeInternRepository(Intern intern) : IInternRepository
    {
        public Task<Intern?> GetByUserIdAsync(int userId) =>
            Task.FromResult<Intern?>(userId == intern.UserId ? intern : null);

        public Task<Intern?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Intern>> GetAllAsync() => throw new NotImplementedException();
        public Task<int> AddAsync(Intern i) => throw new NotImplementedException();
        public Task UpdateAsync(Intern i) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task DeleteWithAccountAsync(int internId, int? userId) => throw new NotImplementedException();
        public Task<bool> ExistsByDepartmentIdAsync(int departmentId) => throw new NotImplementedException();
        public Task<bool> ExistsByMentorIdAsync(int mentorId) => throw new NotImplementedException();
        public Task<bool> ExistsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<IEnumerable<Intern>> GetByMentorIdAsync(int mentorEmployeeId) => throw new NotImplementedException();
    }
}
