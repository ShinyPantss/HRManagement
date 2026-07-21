using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Departments.Commands.UpdateDepartment;

public sealed class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, Unit>
{
    private readonly IDepartmentRepository _departmentRepository;

    public UpdateDepartmentCommandHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    // Input validation UpdateDepartmentCommandValidator'da.
    // Burada yalnızca veritabanına bakan İŞ KURALI kalır.
    public async Task<Unit> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.Id);

        if (department is null)
            throw new ValidationException("Departman bulunamadı.");

        department.Name = request.Name.Trim();
        department.Description = request.Description;

        await _departmentRepository.UpdateAsync(department);

        return Unit.Value;
    }
}
