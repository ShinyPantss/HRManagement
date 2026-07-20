using System.ComponentModel.DataAnnotations;
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

    public async Task<int> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
            throw new ValidationException("Ad zorunludur.");

        if (string.IsNullOrWhiteSpace(request.LastName))
            throw new ValidationException("Soyad zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("E-posta zorunludur.");

        if (request.HireDate.Date < request.BirthDate.Date)
            throw new ValidationException("İşe giriş tarihi doğum tarihinden önce olamaz.");

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
