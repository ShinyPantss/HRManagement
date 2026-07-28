using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.UpdateMyInternTaskStatus;

public sealed class UpdateMyInternTaskStatusCommandHandler
    : IRequestHandler<UpdateMyInternTaskStatusCommand, Unit>
{
    private readonly IInternRepository _internRepository;
    private readonly IInternTaskRepository _taskRepository;

    public UpdateMyInternTaskStatusCommandHandler(
        IInternRepository internRepository,
        IInternTaskRepository taskRepository)
    {
        _internRepository = internRepository;
        _taskRepository = taskRepository;
    }

    public async Task<Unit> Handle(UpdateMyInternTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var intern = await _internRepository.GetByUserIdAsync(request.RequesterUserId);

        if (intern is null)
            throw new ValidationException("Hesabınız bir stajyer kaydına bağlı değil.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId);

        // SAHİPLİK kuralı: görev bu stajyerin değilse varlığı da sızdırılmaz —
        // iki durumda da aynı mesaj (başkasının görev id'sini yoklayarak
        // "var/yok" bilgisi toplanamasın).
        if (task is null || task.InternId != intern.Id)
            throw new ValidationException("Görev bulunamadı.");

        task.Status = (InternTaskStatus)request.NewStatus;
        await _taskRepository.UpdateAsync(task);

        return Unit.Value;
    }
}
