using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.Employees.Shared;

/// <summary>
/// "Kim hangi çalışanı görebilir" kuralı — liste ve detay sorgularının ortak
/// kalbi. Tek yerde durur ki liste ile detay birbirinden sapmasın (listede
/// görünmeyen birinin detayına Id yazarak ulaşılamasın).
///
///   Admin, HR → herkes
///   Manager   → BİR ÜST yöneticisi + kendisi + KENDİ EKİBİ (zincir aşağı)
///   Employee  → BİR ÜST yöneticisi + kendisi + EKİP ARKADAŞLARI
///               (aynı yöneticiye bağlı olanlar; kullanıcı kararı 2026-07-24)
///   diğer     → kimse
///
/// Görüş alanı bilinçli olarak SINIRLI: kişi bir üstünü görür ama üstünün
/// üstünü (örn. GM'yi) GÖREMEZ — organizasyonda zincirleme gezinti yoktur.
/// Yetki rolden DEĞİL ilişkiden doğar.
/// </summary>
public sealed class EmployeeVisibility
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeVisibility(IUserRepository userRepository, IEmployeeRepository employeeRepository)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
    }

    /// <summary>İsteği yapanın görebileceği çalışanlar.</summary>
    public async Task<IEnumerable<Employee>> GetVisibleAsync(int requesterUserId)
    {
        var actor = await GetActiveUserAsync(requesterUserId);

        if (actor.Role is Role.Admin or Role.HR)
            return await _employeeRepository.GetAllAsync();

        var actorEmployee = await _employeeRepository.GetByUserIdAsync(actor.Id);

        if (actorEmployee is null)
            return [];   // hesabı bir çalışan kaydına bağlı değil → görecek kimse yok

        var visible = new List<Employee>();

        // Bir üst yönetici (her iki rol için): "Ekibim" ekranının Yöneticim bölümü.
        if (actorEmployee.ManagerId is int managerId)
        {
            var manager = await _employeeRepository.GetByIdAsync(managerId);
            if (manager is not null)
                visible.Add(manager);
        }

        visible.Add(actorEmployee);

        if (actor.Role == Role.Manager)
        {
            // Ekibi: zincir aşağı herkes.
            visible.AddRange(await _employeeRepository.GetTeamAsync(actorEmployee.Id));
        }
        else if (actorEmployee.ManagerId is int sameManagerId)
        {
            // Ekip arkadaşları: aynı yöneticiye DOĞRUDAN bağlı olanlar.
            // GetTeamAsync zinciri komple döner; kardeş = ManagerId'si aynı olanlar.
            var siblings = (await _employeeRepository.GetTeamAsync(sameManagerId))
                .Where(e => e.ManagerId == sameManagerId && e.Id != actorEmployee.Id);
            visible.AddRange(siblings);
        }

        return visible;
    }

    /// <summary>
    /// Tek bir çalışanı görme yetkisi. Yetkisizse ValidationException fırlatır.
    /// Liste ile AYNI kuralı kullanır — aksi hâlde listede gizlenen kayda
    /// detay adresinden ulaşılabilirdi (IDOR).
    /// </summary>
    public async Task EnsureCanViewAsync(int requesterUserId, int targetEmployeeId)
    {
        var actor = await GetActiveUserAsync(requesterUserId);

        if (actor.Role is Role.Admin or Role.HR)
            return;

        var actorEmployee = await _employeeRepository.GetByUserIdAsync(actor.Id);

        if (actorEmployee is not null)
        {
            if (actorEmployee.Id == targetEmployeeId)
                return;   // kendi kaydı

            if (actorEmployee.ManagerId == targetEmployeeId)
                return;   // bir üst yöneticisi (üstünün üstü BURADAN geçemez)

            if (actor.Role == Role.Manager
                && await _employeeRepository.IsInManagerChainAsync(actorEmployee.Id, targetEmployeeId))
                return;   // kendi ekibinden biri (zincir aşağı)

            if (actor.Role == Role.Employee && actorEmployee.ManagerId is int myManagerId)
            {
                var target = await _employeeRepository.GetByIdAsync(targetEmployeeId);
                if (target?.ManagerId == myManagerId)
                    return;   // ekip arkadaşı (aynı yöneticiye bağlı)
            }
        }

        throw new ValidationException("Bu çalışanın bilgilerini görüntüleme yetkiniz yok.");
    }

    private async Task<User> GetActiveUserAsync(int userId)
    {
        var actor = await _userRepository.GetByIdAsync(userId);

        if (actor is null || !actor.IsActive)
            throw new ValidationException("İşlemi yapan hesap bulunamadı veya pasif.");

        return actor;
    }
}
