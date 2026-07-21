using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetAllEmployees;

public sealed class GetAllEmployeesQuery : IRequest<IEnumerable<EmployeeDto>> { }
