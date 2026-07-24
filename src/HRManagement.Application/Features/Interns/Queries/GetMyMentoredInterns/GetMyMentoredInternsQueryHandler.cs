using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Mapping;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetMyMentoredInterns;

public sealed class GetMyMentoredInternsQueryHandler
    : IRequestHandler<GetMyMentoredInternsQuery, IEnumerable<InternDto>>
{
    private readonly IInternRepository _internRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public GetMyMentoredInternsQueryHandler(
        IInternRepository internRepository,
        IEmployeeRepository employeeRepository)
    {
        _internRepository = internRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<InternDto>> Handle(GetMyMentoredInternsQuery request, CancellationToken cancellationToken)
    {
        // Mentorluk User'a değil EMPLOYEE kaydına bağlıdır (Interns.MentorId → Employees.Id).
        var mentorEmployee = await _employeeRepository.GetByUserIdAsync(request.RequesterUserId);

        if (mentorEmployee is null)
            return [];   // çalışan kaydı yok → mentorluk da yok

        var interns = await _internRepository.GetByMentorIdAsync(mentorEmployee.Id);
        return interns.Select(InternMapping.ToDto);
    }
}
