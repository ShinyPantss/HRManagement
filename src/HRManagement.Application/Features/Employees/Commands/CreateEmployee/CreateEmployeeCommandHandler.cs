using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.AccountRequests.Shared;
using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Application.Features.Units.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, int>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IInternRepository _internRepository;
    private readonly IAccountRequestRepository _accountRequestRepository;
    private readonly IUnitRepository _unitRepository;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        IInternRepository internRepository,
        IAccountRequestRepository accountRequestRepository,
        IUnitRepository unitRepository)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _internRepository = internRepository;
        _accountRequestRepository = accountRequestRepository;
        _unitRepository = unitRepository;
    }

    // Input validation CreateEmployeeCommandValidator'da.
    // Burada yalnızca veritabanına bakan İŞ KURALLARI kalır.
    public async Task<int> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        if (await _departmentRepository.GetByIdAsync(request.DepartmentId) is null)
            throw new ValidationException("Departman bulunamadı.");

        // GM ve GMY yalnızca departmana bağlıdır; birimleri olmaz (sorumlu oldukları alan).
        if (request.UnitId is not null && request.Seniority is SeniorityLevel lvl && !lvl.CanBelongToUnit())
            throw new ValidationException("Genel Müdür ve GMY yalnızca departmana bağlıdır; birim seçilemez.");

        // Seçilen birim (varsa) bu departmana ait olmalı.
        await UnitAssignment.EnsureUnitInDepartmentAsync(_unitRepository, request.UnitId, request.DepartmentId);

        // E-posta benzersizliği: DB'de UNIQUE kısıt var, ama ona takılmak 500 üretir.
        // Kuralı burada önden uygulayıp anlaşılır bir 400 mesajı veriyoruz.
        if (await _employeeRepository.GetByEmailAsync(email) is not null)
            throw new ValidationException("Bu e-posta ile kayıtlı bir çalışan zaten var.");

        if (request.ManagerId is int managerId)
        {
            var manager = await _employeeRepository.GetByIdAsync(managerId);
            if (manager is null)
                throw new ValidationException("Seçilen yönetici bulunamadı.");

            // Yönetici kademesi + kıdem + departman kuralı (GM hariç aynı departman).
            ManagerAssignment.EnsureManagerEligible(
                manager.Seniority, manager.DepartmentId, request.Seniority, request.DepartmentId);
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
            UnitId = request.UnitId,
            UserId = request.UserId,
            ManagerId = request.ManagerId,
            Seniority = request.Seniority,
            AnnualLeaveDays = request.AnnualLeaveDays
        };

        var employeeId = await _employeeRepository.AddAsync(employee);

        // Otomatik hesap talebi: çalışan eklenince HR'ın AYRICA gidip talep açmasına
        // gerek kalmasın diye Admin'e Pending talep düşer. Onay yine Admin'de kalır —
        // görev ayrılığı (HR ister, Admin verir) ve denetim izi korunur; sadece HR'ın
        // fazladan adımı gider. Kutu işaretsizse ya da kayıt zaten bir hesaba
        // bağlandıysa (UserId dolu) talep açılmaz.
        if (request.RequestLoginAccount && request.UserId is null)
            await _accountRequestRepository.AddAsync(new AccountRequest
            {
                EmployeeId = employeeId,
                RequestedByUserId = request.CreatedByUserId,
                // Rol KIDEMDEN türetilir (tek kural, AccountRoleResolver): yönetici
                // kademesi (GM/GMY/Müdür, Direktör=Müdür) → Yönetici; diğerleri → Çalışan.
                // Nihai rolü Admin onay ekranında yine değiştirebilir.
                SuggestedRole = AccountRoleResolver.ForEmployee(request.Seniority),
                Note = "Çalışan kaydı oluşturulurken otomatik açıldı.",
                Status = AccountRequestStatus.Pending
            });

        return employeeId;
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
