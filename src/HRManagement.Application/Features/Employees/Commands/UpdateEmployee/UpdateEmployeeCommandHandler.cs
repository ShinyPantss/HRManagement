using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.AccountRequests.Shared;
using HRManagement.Application.Features.Units.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IInternRepository _internRepository;
    private readonly IUnitRepository _unitRepository;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        IInternRepository internRepository,
        IUnitRepository unitRepository)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _internRepository = internRepository;
        _unitRepository = unitRepository;
    }

    // Input validation UpdateEmployeeCommandValidator'da.
    // Burada yalnızca veritabanına bakan İŞ KURALLARI kalır.
    public async Task<Unit> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.Id);

        if (employee is null)
            throw new ValidationException("Çalışan bulunamadı.");

        var email = request.Email.Trim();

        // Boşsa null saklanır: filtered UNIQUE index yalnızca DOLU olanları denetler.
        var nationalId = string.IsNullOrWhiteSpace(request.NationalId)
            ? null
            : request.NationalId.Trim();

        if (await _departmentRepository.GetByIdAsync(request.DepartmentId) is null)
            throw new ValidationException("Departman bulunamadı.");

        // GM ve GMY yalnızca departmana bağlıdır; birimleri olmaz (sorumlu oldukları alan).
        if (request.UnitId is not null && request.Seniority is SeniorityLevel lvl && !lvl.CanBelongToUnit())
            throw new ValidationException("Genel Müdür ve GMY yalnızca departmana bağlıdır; birim seçilemez.");

        // Seçilen birim (varsa) bu departmana ait olmalı.
        await UnitAssignment.EnsureUnitInDepartmentAsync(_unitRepository, request.UnitId, request.DepartmentId);

        // E-posta başka bir çalışanda mı? (kendi kaydı hariç)
        var byEmail = await _employeeRepository.GetByEmailAsync(email);
        if (byEmail is not null && byEmail.Id != employee.Id)
            throw new ValidationException("Bu e-posta ile kayıtlı başka bir çalışan var.");

        // T.C. Kimlik başka bir çalışanda mı? (kendi kaydı hariç — e-postayla aynı desen)
        if (nationalId is not null)
        {
            var byNationalId = await _employeeRepository.GetByNationalIdAsync(nationalId);
            if (byNationalId is not null && byNationalId.Id != employee.Id)
                throw new ValidationException("Bu T.C. Kimlik No ile kayıtlı başka bir çalışan var.");
        }

        if (request.ManagerId is int managerId)
            await EnsureManagerAssignableAsync(employee.Id, managerId, request.Seniority, request.DepartmentId);

        // Kıdem veya departman değişiyorsa, BU KİŞİYE BAĞLI astların bağı hâlâ
        // geçerli mi? ManagerAssignment şimdiye kadar yalnızca ASTI düzenlerken
        // çalışıyordu; yöneticinin kendisi değişince kimse bakmıyordu ve org
        // sessizce geçersiz hâle geliyordu (ör. GM'lik başkasına verilince
        // eskisine bağlı GMY'ler bir Müdür'e raporlar hâlde kalıyordu).
        if (request.Seniority != employee.Seniority || request.DepartmentId != employee.DepartmentId)
            await EnsureSubordinatesRemainValidAsync(employee.Id, request.Seniority, request.DepartmentId);

        // Hesap bağlantısı değişiyorsa yeni hesabın uygunluğunu denetle.
        if (request.UserId is int userId && userId != employee.UserId)
            await EnsureUserLinkableAsync(userId);

        // Kıdem değiştiyse ve AYNI hesap bağlı kalıyorsa, hesabın rolünü senkronla.
        // (Eski kıdemi üzerine yazmadan ÖNCE yapılmalı — employee.Seniority hâlâ eski.)
        if (employee.UserId is int linkedUserId
            && request.UserId == employee.UserId
            && request.Seniority != employee.Seniority)
        {
            await SyncLinkedAccountRoleAsync(linkedUserId, employee.Seniority, request.Seniority);
        }

        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        // BOŞ gelen T.C. "temizle" değil "DEĞİŞTİRME" demektir.
        // Gerekçe: alan artık yalnızca İK'ya gönderiliyor (EmployeeFieldVisibility),
        // dolayısıyla Admin düzenleme formunu açtığında kutu BOŞ gelir. Boşu
        // "temizle" saysaydık Admin'in her kaydedişi kayıtlı T.C.'yi sessizce
        // silerdi. Bedeli: bir kez girilmiş T.C. bu uçtan temizlenemez —
        // kimlik numarasının silinmesinin iş anlamı da yok.
        employee.NationalId = nationalId ?? employee.NationalId;
        employee.Email = email;
        employee.Phone = request.Phone;
        employee.Gender = request.Gender;
        employee.DateOfBirth = request.BirthDate;
        employee.HireDate = request.HireDate;
        employee.DepartmentId = request.DepartmentId;
        employee.UnitId = request.UnitId;
        employee.UserId = request.UserId;
        employee.ManagerId = request.ManagerId;
        employee.Seniority = request.Seniority;
        employee.AnnualLeaveDays = request.AnnualLeaveDays;
        employee.IsActive = request.IsActive;

        await _employeeRepository.UpdateAsync(employee);

        return Unit.Value;
    }

    /// <summary>
    /// Kıdem değişince bağlı hesabın rolünü türetilen role çeker — AMA yalnızca elle
    /// override EDİLMEMİŞSE. "Override edilmemiş" ölçütü: mevcut rol, ESKİ kıdemden
    /// türetilen rolle aynı. Farklıysa (ör. İK Müdürü = HR bilinçli ataması) korunur.
    /// </summary>
    private async Task SyncLinkedAccountRoleAsync(
        int userId, SeniorityLevel? oldSeniority, SeniorityLevel? newSeniority)
    {
        var oldRole = AccountRoleResolver.ForEmployee(oldSeniority);
        var newRole = AccountRoleResolver.ForEmployee(newSeniority);

        // Türetilen rol değişmiyorsa (ör. Uzman → Kıd. Uzman; ikisi de Çalışan) iş yok.
        if (oldRole == newRole)
            return;

        var user = await _userRepository.GetByIdAsync(userId);

        // Hesap yoksa ya da rol elle override edilmişse (eski türetilene eşit değil) dokunma.
        if (user is null || user.Role != oldRole)
            return;

        user.Role = newRole;
        await _userRepository.UpdateAsync(user);
    }

    /// <summary>
    /// Yöneticinin kıdemi/departmanı değişirken ASTLARINI korur.
    ///
    /// Kural tek yönlü işletilirse org sessizce bozulur: birini GM yapıp eskisini
    /// Müdür'e düşürmek, eski GM'e bağlı GMY'leri "Müdür'e raporlayan GMY" hâline
    /// getirir. Bu yalnızca ekranda yanlış görünmez — izin onay zinciri de
    /// (LeaveApprovalGuard) o geçersiz bağ üzerinden işler.
    ///
    /// Bilinçli olarak ENGELLİYORUZ, otomatik taşımıyoruz: astları sessizce başka
    /// bir yöneticiye bağlamak, İK'nın görmediği bir org değişikliği yapmak olurdu.
    /// </summary>
    private async Task EnsureSubordinatesRemainValidAsync(
        int employeeId,
        HRManagement.Domain.Enums.SeniorityLevel? newSeniority,
        int newDepartmentId)
    {
        // Doğrudan astlar: zincir aşağının yalnızca ilk halkası
        // (EmployeeVisibility'deki kardeş bulma deseninin aynısı).
        var directReports = (await _employeeRepository.GetTeamAsync(employeeId))
            .Where(e => e.ManagerId == employeeId)
            .ToList();

        if (directReports.Count == 0)
            return;

        var broken = directReports
            .Where(r => Shared.ManagerAssignment.GetIneligibilityReason(
                newSeniority, newDepartmentId, r.Seniority, r.DepartmentId) is not null)
            .Select(r => $"{r.FirstName} {r.LastName}")
            .ToList();

        if (broken.Count == 0)
            return;

        var names = string.Join(", ", broken.Take(5));
        var more = broken.Count > 5 ? $" ve {broken.Count - 5} kişi daha" : "";

        throw new ValidationException(
            $"Bu değişiklik, bu kişiye bağlı {broken.Count} çalışanın yönetici bağını geçersiz kılar " +
            $"({names}{more}). Önce onları uygun bir yöneticiye taşıyın, sonra bu kaydı güncelleyin.");
    }

    private async Task EnsureManagerAssignableAsync(
        int employeeId, int managerId,
        HRManagement.Domain.Enums.SeniorityLevel? employeeSeniority, int employeeDepartmentId)
    {
        if (managerId == employeeId)
            throw new ValidationException("Bir çalışan kendi yöneticisi olamaz.");

        var manager = await _employeeRepository.GetByIdAsync(managerId);
        if (manager is null)
            throw new ValidationException("Seçilen yönetici bulunamadı.");

        // Yönetici kademesi + kıdem + departman kuralı (GM hariç aynı departman).
        Shared.ManagerAssignment.EnsureManagerEligible(
            manager.Seniority, manager.DepartmentId, employeeSeniority, employeeDepartmentId);

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
