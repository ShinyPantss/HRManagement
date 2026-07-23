using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IInternRepository _internRepository;

    public UpdateEmployeeCommandHandler(
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

    // Input validation UpdateEmployeeCommandValidator'da.
    // Burada yalnızca veritabanına bakan İŞ KURALLARI kalır.
    public async Task<Unit> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.Id);

        if (employee is null)
            throw new ValidationException("Çalışan bulunamadı.");

        var email = request.Email.Trim();

        if (await _departmentRepository.GetByIdAsync(request.DepartmentId) is null)
            throw new ValidationException("Departman bulunamadı.");

        // E-posta başka bir çalışanda mı? (kendi kaydı hariç)
        var byEmail = await _employeeRepository.GetByEmailAsync(email);
        if (byEmail is not null && byEmail.Id != employee.Id)
            throw new ValidationException("Bu e-posta ile kayıtlı başka bir çalışan var.");

        if (request.ManagerId is int managerId)
            await EnsureManagerAssignableAsync(employee.Id, managerId, request.Seniority);

        // Hesap bağlantısı değişiyorsa yeni hesabın uygunluğunu denetle.
        if (request.UserId is int userId && userId != employee.UserId)
            await EnsureUserLinkableAsync(userId);

        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.NationalId = request.NationalId;
        employee.Email = email;
        employee.Phone = request.Phone;
        employee.DateOfBirth = request.BirthDate;
        employee.HireDate = request.HireDate;
        employee.DepartmentId = request.DepartmentId;
        employee.UserId = request.UserId;
        employee.ManagerId = request.ManagerId;
        employee.Seniority = request.Seniority;
        employee.AnnualLeaveDays = request.AnnualLeaveDays;
        employee.IsActive = request.IsActive;

        await _employeeRepository.UpdateAsync(employee);

        return Unit.Value;
    }

    private async Task EnsureManagerAssignableAsync(
        int employeeId, int managerId, HRManagement.Domain.Enums.SeniorityLevel? employeeSeniority)
    {
        if (managerId == employeeId)
            throw new ValidationException("Bir çalışan kendi yöneticisi olamaz.");

        var manager = await _employeeRepository.GetByIdAsync(managerId);
        if (manager is null)
            throw new ValidationException("Seçilen yönetici bulunamadı.");

        // Yönetici, çalışandan kıdemce yüksek olmalı (Uzman, Müdür'e yönetici olamaz).
        Shared.ManagerAssignment.EnsureManagerOutranks(manager.Seniority, employeeSeniority);

        // Döngü önleme: yeni yönetici bu çalışanın ALTINDA ise (çalışan, adayın
        // zincirinde yukarıdaysa) bağ kurulunca A→B→A döngüsü oluşur ve onay
        // zinciri sorgusu anlamını yitirir.
        if (await _employeeRepository.IsInManagerChainAsync(employeeId, managerId))
            throw new ValidationException(
                "Bu atama bir yönetici döngüsü oluşturur: seçilen kişi bu çalışanın ekibinde (altında) yer alıyor.");
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
