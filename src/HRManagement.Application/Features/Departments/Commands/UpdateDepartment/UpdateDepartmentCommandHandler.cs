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

    public async Task<Unit> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Departman adı zorunludur.");

        var department = await _departmentRepository.GetByIdAsync(request.Id);

        if (department is null)
            throw new ValidationException("Departman bulunamadı.");

        department.Name = request.Name.Trim();
        department.Description = request.Description;

        await _departmentRepository.UpdateAsync(department);

        return Unit.Value;
    }
}
