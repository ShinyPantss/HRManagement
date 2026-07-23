using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, int>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IInternRepository _internRepository;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        IInternRepository internRepository)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _internRepository = internRepository;
    }

    // Input validation CreateEmployeeCommandValidator'da.
    // Burada yalnızca veritabanına bakan İŞ KURALLARI kalır.
    public async Task<int> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        if (await _departmentRepository.GetByIdAsync(request.DepartmentId) is null)
            throw new ValidationException("Departman bulunamadı.");

        // E-posta benzersizliği: DB'de UNIQUE kısıt var, ama ona takılmak 500 üretir.
        // Kuralı burada önden uygulayıp anlaşılır bir 400 mesajı veriyoruz.
        if (await _employeeRepository.GetByEmailAsync(email) is not null)
            throw new ValidationException("Bu e-posta ile kayıtlı bir çalışan zaten var.");

        if (request.ManagerId is int managerId)
        {
            var manager = await _employeeRepository.GetByIdAsync(managerId);
            if (manager is null)
                throw new ValidationException("Seçilen yönetici bulunamadı.");

            // Yönetici, çalışandan kıdemce yüksek olmalı (Uzman, Müdür'e yönetici olamaz).
            ManagerAssignment.EnsureManagerOutranks(manager.Seniority, request.Seniority);
        }

        if (request.UserId is int userId)
            await EnsureUserLinkableAsync(userId);

        var employee = new Employee
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            NationalId = request.NationalId,
            Email = email,
            Phone = request.Phone,
            DateOfBirth = request.BirthDate,
            HireDate = request.HireDate,
            DepartmentId = request.DepartmentId,
            UserId = request.UserId,
            ManagerId = request.ManagerId,
            Seniority = request.Seniority,
            AnnualLeaveDays = request.AnnualLeaveDays
        };

        return await _employeeRepository.AddAsync(employee);
    }

    // Bir hesap yalnızca TEK kişiye bağlanabilir: aksi hâlde "giriş yapan kim?"
    // sorusunun iki cevabı olur ve izin/yetki akışı belirsizleşir.
    private async Task EnsureUserLinkableAsync(int userId)
    {
        if (await _userRepository.GetByIdAsync(userId) is null)
            throw new ValidationException("Bağlanacak kullanıcı hesabı bulunamadı.");

        if (await _employeeRepository.ExistsByUserIdAsync(userId))
            throw new ValidationException("Bu kullanıcı hesabı zaten başka bir çalışana bağlı.");

        if (await _internRepository.ExistsByUserIdAsync(userId))
            throw new ValidationException("Bu kullanıcı hesabı bir stajyere bağlı.");
    }
}
