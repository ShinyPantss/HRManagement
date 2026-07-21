using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Departments.Queries.GetDepartmentById;

public sealed record GetDepartmentByIdQuery(int Id) : IRequest<DepartmentDto?>;
