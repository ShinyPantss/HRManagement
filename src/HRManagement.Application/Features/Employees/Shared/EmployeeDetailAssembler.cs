using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Services;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.Employees.Shared;

/// <summary>
/// Detay DTO'sunu ilişkili verilerden derler ve hassas alanları İSTEĞİ YAPANA
/// göre kırpar. İki query paylaşır (Id ile detay + "benim profilim") — kırpma
/// kuralları tek yerde dursun diye EmployeeVisibility deseniyle ayrı sınıftır.
///
/// Kırpma kuralları (kullanıcı kararı, 2026-07-23):
///   T.C. Kimlik  → yalnızca HR. Admin dahil kimse göremez.
///   İzin açıklaması → kişinin kendisi, HR ve Admin; Manager göremez
///                     (bakiye ve tarihler onay için yeter, gerekçe mahremdir).
///   Notlar (§5.2) → HR/Admin/Manager görür; kişinin KENDİSİ hangi rolde olursa
///                   olsun kendi notlarını GÖREMEZ (değerlendirme mahiyetinde).
///                   Null = göremez, boş liste = görebilir ama not yok.
/// </summary>
public sealed class EmployeeDetailAssembler
{
    // Ekranda listeyi boğmamak için son N talep gösterilir; bakiye TOPLAMI ise
    // her zaman tüm geçmişten hesaplanır (GetTotalUsedAnnualDaysAsync).
    private const int RecentLeaveRequestCount = 10;

    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitRepository _unitRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IInternRepository _internRepository;
    private readonly IEmployeeNoteRepository _noteRepository;

    public EmployeeDetailAssembler(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IUnitRepository unitRepository,
        ILeaveRequestRepository leaveRequestRepository,
        IInternRepository internRepository,
        IEmployeeNoteRepository noteRepository)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _unitRepository = unitRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _internRepository = internRepository;
        _noteRepository = noteRepository;
    }

    /// <summary>Görme YETKİSİ çağırandan önce denetlenmiş olmalı (EmployeeVisibility).</summary>
    public async Task<EmployeeDetailDto> BuildAsync(Employee employee, int requesterUserId)
    {
        // Kırpma kararı için istekçinin rolü ve "kendi kaydı mı" bilgisi gerekir.
        // Rol JWT claim'inden değil DB'den okunur — claim bayatlayabilir.
        var requester = await _userRepository.GetByIdAsync(requesterUserId);
        var isSelf = employee.UserId is int uid && uid == requesterUserId;

        var canSeeNationalId = requester?.Role == Role.HR;
        var canSeeLeaveDescription = isSelf || requester?.Role is Role.HR or Role.Admin;

        // Manager artık hem zincirinin AŞAĞISINI hem bir ÜSTÜNÜ açabilir
        // (genişletilmiş görünürlük); zincir yöneticisi olup olmadığı hassas
        // alan kararlarında ayrıca denetlenir — patronunun notu/izni görünmez.
        var isChainManager = false;
        if (!isSelf && requester?.Role == Role.Manager)
        {
            var requesterEmployee = await _employeeRepository.GetByUserIdAsync(requesterUserId);
            isChainManager = requesterEmployee is not null
                && await _employeeRepository.IsInManagerChainAsync(requesterEmployee.Id, employee.Id);
        }

        var canSeeNotes = !isSelf && (requester?.Role is Role.HR or Role.Admin || isChainManager);

        // İzin bakiyesi/geçmişi: kendisi, İK/Admin ve onay verecek zincir
        // yöneticisi. Ekip arkadaşları ve astlar (üstüne bakan) GÖREMEZ.
        var canSeeLeave = isSelf || requester?.Role is Role.HR or Role.Admin || isChainManager;

        var department = await _departmentRepository.GetByIdAsync(employee.DepartmentId);

        // Birim (varsa) — pozisyon gösteriminde departmanı önceler.
        var unit = employee.UnitId is int unitId
            ? await _unitRepository.GetByIdAsync(unitId)
            : null;

        Employee? manager = employee.ManagerId is int managerId
            ? await _employeeRepository.GetByIdAsync(managerId)
            : null;

        // Bakiye — CreateLeaveRequest'teki kümülatif modelle aynı hesap.
        // canSeeLeave değilse veri HİÇ yüklenmez; sıfır/boş gider, WebUI paneli gizler.
        var today = DateTime.UtcNow.Date;
        var accrued = 0;
        var used = 0;
        IEnumerable<Domain.Entities.LeaveRequest> leaveRequests = [];

        if (canSeeLeave)
        {
            accrued = LeaveEntitlement.AccruedEntitlement(employee.HireDate, today, employee.AnnualLeaveDays);
            used = await _leaveRequestRepository.GetTotalUsedAnnualDaysAsync(employee.Id);
            leaveRequests = await _leaveRequestRepository.GetByEmployeeIdAsync(employee.Id);
        }

        // GetTeamAsync zincirin TAMAMINI döner (astların astları dahil); profilde
        // yalnızca doğrudan bağlı olanları gösteriyoruz.
        var team = await _employeeRepository.GetTeamAsync(employee.Id);

        var mentoredInterns = await _internRepository.GetByMentorIdAsync(employee.Id);

        var notes = canSeeNotes ? await BuildNotesAsync(employee.Id) : null;

        return new EmployeeDetailDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            NationalId = canSeeNationalId ? employee.NationalId : null,
            Email = employee.Email,
            Phone = employee.Phone,
            BirthDate = employee.DateOfBirth,
            HireDate = employee.HireDate,
            Gender = (int?)employee.Gender,
            Seniority = (int?)employee.Seniority,
            IsActive = employee.IsActive,
            DepartmentId = employee.DepartmentId,
            DepartmentName = department?.Name ?? string.Empty,
            UnitId = employee.UnitId,
            UnitName = unit?.Name,
            ManagerId = employee.ManagerId,
            ManagerFullName = manager is null ? null : $"{manager.FirstName} {manager.LastName}",
            CanSeeLeave = canSeeLeave,
            AccruedLeaveDays = accrued,
            UsedLeaveDays = used,
            RemainingLeaveDays = accrued - used,   // avans izinde eksiye düşebilir
            RecentLeaveRequests = leaveRequests
                .OrderByDescending(l => l.StartDate)
                .Take(RecentLeaveRequestCount)
                .Select(l => new EmployeeDetailLeaveRequestDto
                {
                    Id = l.Id,
                    Type = l.Type,
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    TotalDays = l.WorkingDays,
                    Status = l.Status,
                    Description = canSeeLeaveDescription ? l.Description : null
                })
                .ToList(),
            DirectReports = team
                .Where(t => t.ManagerId == employee.Id)
                .OrderBy(t => t.Seniority).ThenBy(t => t.FirstName)
                .Select(t => new EmployeeDetailTeamMemberDto
                {
                    Id = t.Id,
                    FullName = $"{t.FirstName} {t.LastName}",
                    Seniority = (int?)t.Seniority
                })
                .ToList(),
            MentoredInterns = mentoredInterns
                .OrderByDescending(i => i.StartDate)
                .Select(i => new EmployeeDetailInternDto
                {
                    Id = i.Id,
                    FullName = $"{i.FirstName} {i.LastName}",
                    University = i.University,
                    StartDate = i.StartDate,
                    EndDate = i.EndDate
                })
                .ToList(),
            Notes = notes
        };
    }

    /// <summary>Notları yazar adlarıyla birlikte derler (yazar başına tek User sorgusu).</summary>
    private async Task<List<EmployeeDetailNoteDto>> BuildNotesAsync(int employeeId)
    {
        var notes = await _noteRepository.GetByEmployeeIdAsync(employeeId);
        var authorNames = new Dictionary<int, string>();
        var result = new List<EmployeeDetailNoteDto>();

        foreach (var note in notes.OrderByDescending(n => n.CreatedAt))
        {
            if (!authorNames.TryGetValue(note.AuthorUserId, out var authorName))
            {
                authorName = await ResolveAuthorNameAsync(note.AuthorUserId);
                authorNames[note.AuthorUserId] = authorName;
            }

            result.Add(new EmployeeDetailNoteDto
            {
                Id = note.Id,
                AuthorName = authorName,
                Content = note.Content,
                CreatedAt = note.CreatedAt
            });
        }

        return result;
    }

    /// <summary>
    /// Not yazarının GÖRÜNEN adı: hesap bir çalışan kaydına bağlıysa ad-soyad
    /// ("HPY10534" gibi kullanıcı adı değil); değilse (örn. çalışan kaydı olmayan
    /// HR hesabı) kullanıcı adına düşülür.
    /// </summary>
    private async Task<string> ResolveAuthorNameAsync(int authorUserId)
    {
        var authorEmployee = await _employeeRepository.GetByUserIdAsync(authorUserId);
        if (authorEmployee is not null)
            return $"{authorEmployee.FirstName} {authorEmployee.LastName}";

        var author = await _userRepository.GetByIdAsync(authorUserId);
        return author?.Username ?? "bilinmiyor";
    }
}
