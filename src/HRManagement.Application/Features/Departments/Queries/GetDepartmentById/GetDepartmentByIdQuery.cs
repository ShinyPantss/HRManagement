using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Departments.Queries.GetDepartmentById;

public sealed class GetDepartmentByIdQuery : IRequest<DepartmentDto?>
{
    public GetDepartmentByIdQuery(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
