using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetAllEmployees;

public sealed record GetAllEmployeesQuery : IRequest<IEnumerable<EmployeeDto>>;
