using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetEmployeeDetail;

public sealed class GetEmployeeDetailQueryHandler
    : IRequestHandler<GetEmployeeDetailQuery, EmployeeDetailDto?>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly EmployeeVisibility _visibility;
    private readonly EmployeeDetailAssembler _assembler;

    public GetEmployeeDetailQueryHandler(
        IEmployeeRepository employeeRepository,
        EmployeeVisibility visibility,
        EmployeeDetailAssembler assembler)
    {
        _employeeRepository = employeeRepository;
        _visibility = visibility;
        _assembler = assembler;
    }

    public async Task<EmployeeDetailDto?> Handle(GetEmployeeDetailQuery request, CancellationToken cancellationToken)
    {
        // GetEmployeeById ile AYNI kural: yetki, kayıt okunmadan denetlenir —
        // yetkisiz istekte kaydın var olup olmadığı bile sızmaz (IDOR).
        await _visibility.EnsureCanViewAsync(request.RequesterUserId, request.EmployeeId);

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId);
        if (employee is null)
            return null;

        return await _assembler.BuildAsync(employee, request.RequesterUserId);
    }
}
