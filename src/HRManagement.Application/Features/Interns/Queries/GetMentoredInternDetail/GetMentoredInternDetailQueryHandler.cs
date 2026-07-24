using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Interns.Shared;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetMentoredInternDetail;

public sealed class GetMentoredInternDetailQueryHandler
    : IRequestHandler<GetMentoredInternDetailQuery, MentoredInternDetailDto>
{
    private readonly MentorshipGuard _mentorshipGuard;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IInternTaskRepository _taskRepository;
    private readonly IInternNoteRepository _noteRepository;
    private readonly IUserRepository _userRepository;

    public GetMentoredInternDetailQueryHandler(
        MentorshipGuard mentorshipGuard,
        IDepartmentRepository departmentRepository,
        IInternTaskRepository taskRepository,
        IInternNoteRepository noteRepository,
        IUserRepository userRepository)
    {
        _mentorshipGuard = mentorshipGuard;
        _departmentRepository = departmentRepository;
        _taskRepository = taskRepository;
        _noteRepository = noteRepository;
        _userRepository = userRepository;
    }

    public async Task<MentoredInternDetailDto> Handle(GetMentoredInternDetailQuery request, CancellationToken cancellationToken)
    {
        // Yetki veriden önce: mentor değilse içerik hiç yüklenmez.
        var intern = await _mentorshipGuard.EnsureMentorAsync(request.InternId, request.RequesterUserId);

        var department = await _departmentRepository.GetByIdAsync(intern.DepartmentId);
        var tasks = await _taskRepository.GetByInternIdAsync(intern.Id);
        var notes = await _noteRepository.GetByInternIdAsync(intern.Id);

        return new MentoredInternDetailDto
        {
            Id = intern.Id,
            FirstName = intern.FirstName,
            LastName = intern.LastName,
            Email = intern.Email,
            University = intern.University,
            Major = intern.Major,
            Grade = intern.Grade,
            StartDate = intern.StartDate,
            EndDate = intern.EndDate,
            DepartmentName = department?.Name ?? string.Empty,
            Tasks = tasks
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new InternTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = (int)t.Status,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt
                })
                .ToList(),
            Notes = await BuildNotesAsync(notes)
        };
    }

    /// <summary>Not yazarlarının adları (yazar başına tek User sorgusu — mentor değişmiş olabilir).</summary>
    private async Task<List<InternNoteDto>> BuildNotesAsync(IEnumerable<Domain.Entities.InternNote> notes)
    {
        var authorNames = new Dictionary<int, string>();
        var result = new List<InternNoteDto>();

        foreach (var note in notes.OrderByDescending(n => n.CreatedAt))
        {
            if (!authorNames.TryGetValue(note.AuthorUserId, out var authorName))
            {
                var author = await _userRepository.GetByIdAsync(note.AuthorUserId);
                authorName = author?.Username ?? "bilinmiyor";
                authorNames[note.AuthorUserId] = authorName;
            }

            result.Add(new InternNoteDto
            {
                Id = note.Id,
                AuthorName = authorName,
                Content = note.Content,
                CreatedAt = note.CreatedAt
            });
        }

        return result;
    }
}
