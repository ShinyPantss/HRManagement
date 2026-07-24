using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetMyEmployeeDetail;

public sealed class GetMyEmployeeDetailQueryHandler
    : IRequestHandler<GetMyEmployeeDetailQuery, EmployeeDetailDto?>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly EmployeeDetailAssembler _assembler;

    public GetMyEmployeeDetailQueryHandler(
        IEmployeeRepository employeeRepository,
        EmployeeDetailAssembler assembler)
    {
        _employeeRepository = employeeRepository;
        _assembler = assembler;
    }

    public async Task<EmployeeDetailDto?> Handle(GetMyEmployeeDetailQuery request, CancellationToken cancellationToken)
    {
        // EmployeeVisibility kontrolü YOK: kayıt zaten istekçinin UserId'sinden
        // çözülüyor — kişinin kendi kaydını görmesi her rolde serbesttir.
        var employee = await _employeeRepository.GetByUserIdAsync(request.RequesterUserId);
        if (employee is null)
            return null;

        return await _assembler.BuildAsync(employee, request.RequesterUserId);
    }
}
