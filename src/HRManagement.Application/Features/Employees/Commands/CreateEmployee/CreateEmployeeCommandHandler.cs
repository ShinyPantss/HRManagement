using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, int>
{
    private readonly IEmployeeRepository _employeeRepository;

    public CreateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    // Input validation CreateEmployeeCommandValidator'da; buraya gelen mesaj geçerlidir.
    public async Task<int> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = new Employee
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            NationalId = request.NationalId,
            Email = request.Email.Trim(),
            Phone = request.Phone,
            DateOfBirth = request.BirthDate,
            HireDate = request.HireDate,
            Position = request.Position,
            DepartmentId = request.DepartmentId
        };

        return await _employeeRepository.AddAsync(employee);
    }
}
