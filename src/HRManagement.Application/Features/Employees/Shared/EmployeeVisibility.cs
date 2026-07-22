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
///   Manager   → yalnızca KENDİ EKİBİ (zincir aşağı) + kendisi
///   Employee  → yalnızca kendisi (§5.1)
///   diğer     → kimse
///
/// Yetki rolden DEĞİL ilişkiden doğar: Role=Manager olmak tek başına yetmez,
/// kişinin gerçekten o çalışanın zincirinde yukarıda olması gerekir.
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

        if (actor.Role == Role.Manager)
        {
            var team = await _employeeRepository.GetTeamAsync(actorEmployee.Id);
            return team.Prepend(actorEmployee);   // ekibi + kendisi
        }

        return [actorEmployee];   // Employee: yalnızca kendisi
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

            if (actor.Role == Role.Manager
                && await _employeeRepository.IsInManagerChainAsync(actorEmployee.Id, targetEmployeeId))
                return;   // kendi ekibinden biri
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
