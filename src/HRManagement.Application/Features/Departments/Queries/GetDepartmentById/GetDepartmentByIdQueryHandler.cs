using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Mapping;
using MediatR;

namespace HRManagement.Application.Features.Departments.Queries.GetDepartmentById;

public sealed class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto?>
{
    private readonly IDepartmentRepository _departmentRepository;

    public GetDepartmentByIdQueryHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<DepartmentDto?> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.Id);
        return department is null ? null : DepartmentMapping.ToDto(department);
    }
}
