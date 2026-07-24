using HRManagement.Domain.Enums;

namespace HRManagement.Application.DTOs;

/// <summary>
/// Detay/profil sayfasının tek seferde ihtiyaç duyduğu her şey. EmployeeDto'dan
/// farkı: ilişkili verileri (departman adı, yönetici, izin bakiyesi, ekip,
/// mentorluk) de taşır ve hassas alanları İSTEĞİ YAPANA GÖRE kırpılmış gelir —
/// kırpmayı EmployeeDetailAssembler yapar, buraya ne konduysa gösterilebilir demektir.
/// </summary>
public class EmployeeDetailDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Yalnızca HR dolu görür; Admin dahil herkese null gider (kullanıcı kararı).</summary>
    public string? NationalId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime BirthDate { get; set; }
    public DateTime HireDate { get; set; }
    public int? Seniority { get; set; }   // SeniorityLevel enum'ının sayısal karşılığı
    public bool IsActive { get; set; }

    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public int? ManagerId { get; set; }
    public string? ManagerFullName { get; set; }

    // Kümülatif izin bakiyesi — CreateLeaveRequest'teki hesapla AYNI kaynak
    // (LeaveEntitlement + GetTotalUsedAnnualDaysAsync), sayı ekranda da tutsun diye.
    public int AccruedLeaveDays { get; set; }
    public int UsedLeaveDays { get; set; }
    public int RemainingLeaveDays { get; set; }

    public List<EmployeeDetailLeaveRequestDto> RecentLeaveRequests { get; set; } = [];
    public List<EmployeeDetailTeamMemberDto> DirectReports { get; set; } = [];
    public List<EmployeeDetailInternDto> MentoredInterns { get; set; } = [];

    /// <summary>
    /// Çalışana ait notlar (§5.2). NULL = istekçi notları GÖREMEZ (panel hiç
    /// çizilmez; kişi kendi notlarını görmez) — boş liste = görebilir ama not yok.
    /// </summary>
    public List<EmployeeDetailNoteDto>? Notes { get; set; }
}

public class EmployeeDetailLeaveRequestDto
{
    public int Id { get; set; }
    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }   // iş günü
    public LeaveStatus Status { get; set; }

    /// <summary>Manager'a null gider: bakiyeyi görür, gerekçeyi görmez (kullanıcı kararı).</summary>
    public string? Description { get; set; }
}

public class EmployeeDetailTeamMemberDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? Seniority { get; set; }
}

public class EmployeeDetailInternDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class EmployeeDetailNoteDto
{
    public int Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;   // yazarın kullanıcı adı
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
