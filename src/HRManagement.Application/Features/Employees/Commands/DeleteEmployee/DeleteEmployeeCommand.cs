using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.DeleteEmployee;

public sealed record DeleteEmployeeCommand(int Id) : IRequest<Unit>;
