using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;

    public UpdateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Unit> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
            throw new ValidationException("Ad zorunludur.");

        if (string.IsNullOrWhiteSpace(request.LastName))
            throw new ValidationException("Soyad zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("E-posta zorunludur.");

        if (request.HireDate.Date < request.BirthDate.Date)
            throw new ValidationException("İşe giriş tarihi doğum tarihinden önce olamaz.");

        var employee = await _employeeRepository.GetByIdAsync(request.Id);

        if (employee is null)
            throw new ValidationException("Çalışan bulunamadı.");

        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.NationalId = request.NationalId;
        employee.Email = request.Email.Trim();
        employee.Phone = request.Phone;
        employee.DateOfBirth = request.BirthDate;
        employee.HireDate = request.HireDate;
        employee.Position = request.Position;
        employee.DepartmentId = request.DepartmentId;
        employee.IsActive = request.IsActive;

        await _employeeRepository.UpdateAsync(employee);

        return Unit.Value;
    }
}
