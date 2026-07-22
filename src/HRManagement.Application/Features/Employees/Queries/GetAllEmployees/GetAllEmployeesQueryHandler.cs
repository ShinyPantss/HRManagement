using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Application.Mapping;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetAllEmployees;

public sealed class GetAllEmployeesQueryHandler : IRequestHandler<GetAllEmployeesQuery, IEnumerable<EmployeeDto>>
{
    private readonly EmployeeVisibility _visibility;

    public GetAllEmployeesQueryHandler(EmployeeVisibility visibility)
    {
        _visibility = visibility;
    }

    public async Task<IEnumerable<EmployeeDto>> Handle(GetAllEmployeesQuery request, CancellationToken cancellationToken)
    {
        // Filtreleme sorgunun İÇİNDE: yetkisiz kayıt bu katmandan yukarı hiç çıkmaz.
        var employees = await _visibility.GetVisibleAsync(request.RequesterUserId);
        return employees.Select(EmployeeMapping.ToDto);
    }
}
