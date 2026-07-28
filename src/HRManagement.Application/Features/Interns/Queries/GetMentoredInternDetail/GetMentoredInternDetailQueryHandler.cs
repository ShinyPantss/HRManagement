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
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitRepository _unitRepository;
    private readonly UnitManagerResolver _unitManagerResolver;

    public GetMentoredInternDetailQueryHandler(
        MentorshipGuard mentorshipGuard,
        IDepartmentRepository departmentRepository,
        IInternTaskRepository taskRepository,
        IInternNoteRepository noteRepository,
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IUnitRepository unitRepository,
        UnitManagerResolver unitManagerResolver)
    {
        _mentorshipGuard = mentorshipGuard;
        _departmentRepository = departmentRepository;
        _taskRepository = taskRepository;
        _noteRepository = noteRepository;
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _unitRepository = unitRepository;
        _unitManagerResolver = unitManagerResolver;
    }

    public async Task<MentoredInternDetailDto> Handle(GetMentoredInternDetailQuery request, CancellationToken cancellationToken)
    {
        // Yetki veriden önce: mentor VEYA HR/Admin değilse içerik hiç yüklenmez.
        // (HR/Admin salt-okur gözlemler; görev/not ekleme yazma handler'larında mentor-only kalır.)
        var intern = await _mentorshipGuard.EnsureCanViewAsync(request.InternId, request.RequesterUserId);

        var department = await _departmentRepository.GetByIdAsync(intern.DepartmentId);
        var tasks = await _taskRepository.GetByInternIdAsync(intern.Id);
        var notes = await _noteRepository.GetByInternIdAsync(intern.Id);

        // Mentor adı — özellikle HR/Admin salt-okur bakarken sorumluyu göstermek için.
        var mentor = intern.MentorId is int mentorId
            ? await _employeeRepository.GetByIdAsync(mentorId)
            : null;

        var unit = intern.UnitId is int unitId
            ? await _unitRepository.GetByIdAsync(unitId)
            : null;

        // Yönetici mentor'dan AYRI: birim/departman hiyerarşisinden türetilir.
        var unitManager = await _unitManagerResolver.ResolveAsync(intern.DepartmentId, intern.UnitId);

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
            MentorFullName = mentor is null ? null : $"{mentor.FirstName} {mentor.LastName}",
            MentorEmployeeId = mentor?.Id,
            UnitName = unit?.Name,
            ManagerFullName = unitManager is null ? null : $"{unitManager.FirstName} {unitManager.LastName}",
            ManagerEmployeeId = unitManager?.Id,
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

    /// <summary>Not yazarlarının adları (yazar başına tek sorgu — mentor değişmiş olabilir).</summary>
    private async Task<List<InternNoteDto>> BuildNotesAsync(IEnumerable<Domain.Entities.InternNote> notes)
    {
        var authorNames = new Dictionary<int, string>();
        var result = new List<InternNoteDto>();

        foreach (var note in notes.OrderByDescending(n => n.CreatedAt))
        {
            if (!authorNames.TryGetValue(note.AuthorUserId, out var authorName))
            {
                authorName = await ResolveAuthorNameAsync(note.AuthorUserId);
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

    /// <summary>
    /// Not yazarının GÖRÜNEN adı: hesap bir çalışan kaydına bağlıysa ad-soyad
    /// ("HPY10534" gibi kullanıcı adı değil); değilse kullanıcı adına düşülür.
    /// </summary>
    private async Task<string> ResolveAuthorNameAsync(int authorUserId)
    {
        var authorEmployee = await _employeeRepository.GetByUserIdAsync(authorUserId);
        if (authorEmployee is not null)
            return $"{authorEmployee.FirstName} {authorEmployee.LastName}";

        var author = await _userRepository.GetByIdAsync(authorUserId);
        return author?.Username ?? "bilinmiyor";
    }
}
