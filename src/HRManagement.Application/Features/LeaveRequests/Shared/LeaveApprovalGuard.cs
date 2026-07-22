using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.LeaveRequests.Shared;

/// <summary>
/// İki aşamalı onayın YETKİ kuralları — Approve ve Reject handler'larının ortak
/// kalbi. Tek yerde durur ki "onaylayabilen reddedebilir" simetrisi bozulmasın.
///
/// Kurallar (tasarım kararları, 2026-07-22):
///   1. aşama (Pending)   → talep sahibinin yönetici zincirinde YUKARIDA olan
///                          biri (stajyerde zincir mentordan başlar). Admin her
///                          zaman geçer — zincir boş olduğunda kilit çözücüdür.
///   2. aşama (PendingHr) → HR rolü veya Admin; 1. aşamayı onaylayanla AYNI KİŞİ
///                          OLAMAZ (iki ayrı göz), talep sahibi de olamaz.
///   Hiç kimse kendi talebini hiçbir aşamada işleyemez — rolden bağımsız.
/// </summary>
public sealed class LeaveApprovalGuard
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternRepository _internRepository;

    public LeaveApprovalGuard(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IInternRepository internRepository)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _internRepository = internRepository;
    }

    /// <summary>
    /// İşlemi yapan hesabı doğrular, talep sahibiyle aynı kişi olmadığını
    /// garanti eder ve bulunduğu aşama için yetkisini denetler.
    /// Yetkisizse ValidationException fırlatır; yetkiliyse hesabı döndürür.
    /// </summary>
    public async Task<User> EnsureCanActAsync(LeaveRequest leaveRequest, int actorUserId)
    {
        var actor = await _userRepository.GetByIdAsync(actorUserId);

        if (actor is null || !actor.IsActive)
            throw new ValidationException("İşlemi yapan hesap bulunamadı veya pasif.");

        // Talep sahibinin hesabını bul — self-onay/self-red her aşamada yasak.
        // (1. aşamada zaten yapısal olarak imkânsızdır: kimse kendi zincirinde
        // kendinden yukarıda değildir. Ama 2. aşamada gerçek bir açıktır: HR
        // uzmanının kendi talebi PendingHr'a gelmişse "HR rolü" şartını sağlar.)
        var ownerUserId = await GetOwnerUserIdAsync(leaveRequest);

        // FAIL-CLOSED: sahip çözülemiyorsa (kayıt hesaba bağlı değil) işlemi
        // reddet. Aksi hâlde "ownerUserId == actor.Id" karşılaştırması int? null
        // olduğunda daima false döner ve self-onay kilidi sessizce devre dışı kalır.
        if (ownerUserId is null)
            throw new ValidationException(
                "Talep sahibinin hesap bağı çözülemedi; işlem güvenlik gereği reddedildi.");

        if (ownerUserId == actor.Id)
            throw new ValidationException("Kendi izin talebiniz üzerinde onay/red işlemi yapamazsınız.");

        switch (leaveRequest.Status)
        {
            case LeaveStatus.Pending:
                await EnsureManagerStageAsync(leaveRequest, actor);
                break;

            case LeaveStatus.PendingHr:
                EnsureHrStage(leaveRequest, actor);
                break;

            default:
                throw new ValidationException("Bu talep, işlem bekleyen bir aşamada değil.");
        }

        return actor;
    }

    private async Task EnsureManagerStageAsync(LeaveRequest leaveRequest, User actor)
    {
        if (actor.Role == Role.Admin)
            return; // kilit çözücü: zincir boşsa/tepe kişiyse akış tıkanmasın

        // Yetki ilişkiden gelir, rolden değil: onaylayanın bir Employee kaydı
        // olmalı ve talep sahibinin zincirinde YUKARIDA olmalı.
        var actorEmployee = await _employeeRepository.GetByUserIdAsync(actor.Id);

        var authorized = false;

        if (actorEmployee is not null)
        {
            if (leaveRequest.EmployeeId is int ownerEmployeeId)
            {
                authorized = await _employeeRepository.IsInManagerChainAsync(
                    actorEmployee.Id, ownerEmployeeId);
            }
            else if (leaveRequest.InternId is int internId)
            {
                // Stajyerin zinciri mentordan başlar: mentorun kendisi veya
                // mentorun zincirinde yukarıdaki herkes yetkilidir.
                var intern = await _internRepository.GetByIdAsync(internId);

                if (intern?.MentorId is int mentorId)
                    authorized = actorEmployee.Id == mentorId
                        || await _employeeRepository.IsInManagerChainAsync(actorEmployee.Id, mentorId);
            }
        }

        if (!authorized)
            throw new ValidationException(
                "Bu talebi yönetici aşamasında işleme yetkiniz yok: talep sahibinin yönetici zincirinde olmalısınız.");
    }

    private static void EnsureHrStage(LeaveRequest leaveRequest, User actor)
    {
        if (actor.Role is not (Role.Admin or Role.HR))
            throw new ValidationException("İK aşamasını yalnızca İK yetkilisi veya Admin işleyebilir.");

        // İki ayrı göz kuralı: aynı kişi iki aşamayı da işleyemez; yoksa
        // "iki aşamalı onay" tek kişinin iki tıkına dönüşür.
        if (leaveRequest.ManagerApprovedByUserId == actor.Id)
            throw new ValidationException(
                "Yönetici onayını siz verdiniz; İK aşamasını başka bir yetkili işlemelidir.");
    }

    private async Task<int?> GetOwnerUserIdAsync(LeaveRequest leaveRequest)
    {
        if (leaveRequest.EmployeeId is int employeeId)
            return (await _employeeRepository.GetByIdAsync(employeeId))?.UserId;

        if (leaveRequest.InternId is int internId)
            return (await _internRepository.GetByIdAsync(internId))?.UserId;

        return null;
    }
}
