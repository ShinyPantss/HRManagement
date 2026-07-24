using HRManagement.Application.Features.Interns.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.AddInternTask;

public sealed class AddInternTaskCommandHandler : IRequestHandler<AddInternTaskCommand, int>
{
    private readonly MentorshipGuard _mentorshipGuard;
    private readonly IInternTaskRepository _taskRepository;

    public AddInternTaskCommandHandler(MentorshipGuard mentorshipGuard, IInternTaskRepository taskRepository)
    {
        _mentorshipGuard = mentorshipGuard;
        _taskRepository = taskRepository;
    }

    public async Task<int> Handle(AddInternTaskCommand request, CancellationToken cancellationToken)
    {
        var intern = await _mentorshipGuard.EnsureMentorAsync(request.InternId, request.RequesterUserId);

        return await _taskRepository.AddAsync(new InternTask
        {
            InternId = intern.Id,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            DueDate = request.DueDate,
            CreatedByUserId = request.RequesterUserId
            // Status: entity varsayılanı Pending — atanan görev başlanmamış doğar.
        });
    }
}
