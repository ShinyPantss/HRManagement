using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Departments.Commands.DeleteDepartment;

public sealed class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, Unit>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternRepository _internRepository;

    public DeleteDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        IInternRepository internRepository)
    {
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _internRepository = internRepository;
    }

    public async Task<Unit> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.Id);

        if (department is null)
            throw new ValidationException("Departman bulunamadı.");

        // Bu kontrol olmadan silme denemesi veritabanının FK kısıtına takılır,
        // SqlException fırlar ve kullanıcı "Beklenmeyen bir hata oluştu." (500) görür.
        // Burada kuralı önden uygulayıp NE yapması gerektiğini söylüyoruz.
        // FK kısıtı yine de yerinde durur: asıl garantiyi o verir, bu mesaj UX içindir.
        if (await _employeeRepository.ExistsByDepartmentIdAsync(request.Id))
            throw new ValidationException(
                "Bu departmana bağlı çalışanlar var. Önce onları başka bir departmana taşıyın.");

        if (await _internRepository.ExistsByDepartmentIdAsync(request.Id))
            throw new ValidationException(
                "Bu departmana bağlı stajyerler var. Önce onları başka bir departmana taşıyın.");

        await _departmentRepository.DeleteAsync(request.Id);

        return Unit.Value;
    }
}
