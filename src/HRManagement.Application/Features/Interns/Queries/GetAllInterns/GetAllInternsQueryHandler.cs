using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Mapping;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetAllInterns;

public sealed class GetAllInternsQueryHandler : IRequestHandler<GetAllInternsQuery, IEnumerable<InternDto>>
{
    private readonly IInternRepository _internRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public GetAllInternsQueryHandler(
        IInternRepository internRepository,
        IEmployeeRepository employeeRepository)
    {
        _internRepository = internRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<InternDto>> Handle(GetAllInternsQuery request, CancellationToken cancellationToken)
    {
        var interns = (await _internRepository.GetAllAsync()).ToList();

        // Mentor adları (§5.4 liste "Mentor bilgileri" ister): stajyer başına
        // sorgu yerine çalışanlar TEK seferde çekilip bellekte eşlenir.
        var mentorNames = (await _employeeRepository.GetAllAsync())
            .ToDictionary(e => e.Id, e => $"{e.FirstName} {e.LastName}");

        return interns.Select(intern =>
        {
            var dto = InternMapping.ToDto(intern);
            if (intern.MentorId is int mentorId && mentorNames.TryGetValue(mentorId, out var name))
                dto.MentorFullName = name;
            return dto;
        });
    }
}
