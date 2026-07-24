using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.Interns.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.UpdateInternTaskStatus;

public sealed class UpdateInternTaskStatusCommandHandler
    : IRequestHandler<UpdateInternTaskStatusCommand, Unit>
{
    private readonly MentorshipGuard _mentorshipGuard;
    private readonly IInternTaskRepository _taskRepository;

    public UpdateInternTaskStatusCommandHandler(
        MentorshipGuard mentorshipGuard,
        IInternTaskRepository taskRepository)
    {
        _mentorshipGuard = mentorshipGuard;
        _taskRepository = taskRepository;
    }

    public async Task<Unit> Handle(UpdateInternTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.TaskId);

        if (task is null)
            throw new ValidationException("Görev bulunamadı.");

        // Yetki göreve değil STAJYERE bağlıdır: görevin stajyerinin mentoru olmalı.
        await _mentorshipGuard.EnsureMentorAsync(task.InternId, request.RequesterUserId);

        task.Status = (InternTaskStatus)request.NewStatus;
        await _taskRepository.UpdateAsync(task);

        return Unit.Value;
    }
}
