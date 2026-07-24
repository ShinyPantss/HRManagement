using HRManagement.Application.Features.Interns.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.AddInternNote;

public sealed class AddInternNoteCommandHandler : IRequestHandler<AddInternNoteCommand, int>
{
    private readonly MentorshipGuard _mentorshipGuard;
    private readonly IInternNoteRepository _noteRepository;

    public AddInternNoteCommandHandler(MentorshipGuard mentorshipGuard, IInternNoteRepository noteRepository)
    {
        _mentorshipGuard = mentorshipGuard;
        _noteRepository = noteRepository;
    }

    public async Task<int> Handle(AddInternNoteCommand request, CancellationToken cancellationToken)
    {
        var intern = await _mentorshipGuard.EnsureMentorAsync(request.InternId, request.RequesterUserId);

        return await _noteRepository.AddAsync(new InternNote
        {
            InternId = intern.Id,
            AuthorUserId = request.RequesterUserId,
            Content = request.Content.Trim()
        });
    }
}
