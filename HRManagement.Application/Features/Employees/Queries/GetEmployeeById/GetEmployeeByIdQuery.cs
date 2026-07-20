using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetEmployeeById;

public sealed record GetEmployeeByIdQuery(int Id) : IRequest<EmployeeDto?>;
