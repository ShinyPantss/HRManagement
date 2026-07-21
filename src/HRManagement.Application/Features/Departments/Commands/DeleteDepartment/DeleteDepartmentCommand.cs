using MediatR;

namespace HRManagement.Application.Features.Departments.Commands.DeleteDepartment;

public sealed record DeleteDepartmentCommand(int Id) : IRequest<Unit>;
