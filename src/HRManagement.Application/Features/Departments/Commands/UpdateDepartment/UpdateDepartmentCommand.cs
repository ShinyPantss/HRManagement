using MediatR;

namespace HRManagement.Application.Features.Departments.Commands.UpdateDepartment;

public sealed record UpdateDepartmentCommand(int Id, string Name, string? Description) : IRequest<Unit>;
