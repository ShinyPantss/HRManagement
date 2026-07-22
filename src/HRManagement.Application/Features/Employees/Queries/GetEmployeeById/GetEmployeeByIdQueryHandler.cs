using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Mapping;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto?>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly EmployeeVisibility _visibility;

    public GetEmployeeByIdQueryHandler(IEmployeeRepository employeeRepository, EmployeeVisibility visibility)
    {
        _employeeRepository = employeeRepository;
        _visibility = visibility;
    }

    public async Task<EmployeeDto?> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        // Yetki kaydı OKUMADAN önce denetlenir; yetkisizse kaydın varlığı bile sızmaz.
        await _visibility.EnsureCanViewAsync(request.RequesterUserId, request.Id);

        var employee = await _employeeRepository.GetByIdAsync(request.Id);
        return employee is null ? null : EmployeeMapping.ToDto(employee);
    }
}
