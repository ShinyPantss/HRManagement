using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using MediatR;

namespace HRManagement.Application.Features.Departments.Commands.CreateDepartment;

public sealed class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, int>
{
    private readonly IDepartmentRepository _departmentRepository;

    public CreateDepartmentCommandHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    // Input validation CreateDepartmentCommandValidator'da; buraya gelen mesaj geçerlidir.
    public async Task<int> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = new Department
        {
            Name = request.Name.Trim(),
            Description = request.Description
        };

        return await _departmentRepository.AddAsync(department);
    }
}
