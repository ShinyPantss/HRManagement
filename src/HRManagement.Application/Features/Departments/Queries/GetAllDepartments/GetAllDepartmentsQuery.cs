using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Departments.Queries.GetAllDepartments;

public sealed class GetAllDepartmentsQuery : IRequest<IEnumerable<DepartmentDto>> { }
