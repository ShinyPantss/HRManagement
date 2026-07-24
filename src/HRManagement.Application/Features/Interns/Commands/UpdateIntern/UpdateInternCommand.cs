using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.UpdateIntern;

public sealed record UpdateInternCommand(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string University,
    string Major,
    int Grade,
    DateTime StartDate,
    DateTime EndDate,
    int? MentorId,
    int DepartmentId,
    int? UnitId) : IRequest<Unit>;
