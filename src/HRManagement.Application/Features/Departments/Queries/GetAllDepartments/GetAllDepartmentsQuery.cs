using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Departments.Queries.GetAllDepartments;

public sealed record GetAllDepartmentsQuery : IRequest<IEnumerable<DepartmentDto>>;
