using HRManagement.Domain.Enums;

namespace HRManagement.Domain.Entities;

public class LeaveRequest
{
    public int Id { get; set; }

    /// <summary>
    /// Talebi açan çalışan. Stajyer talebi ise null olur.
    /// EmployeeId ve InternId'den TAM OLARAK BİRİ dolu olmalıdır;
    /// bu kuralı veritabanındaki CK_LeaveRequests_Requester kısıtı garanti eder.
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>Talebi açan stajyer (§5.3.1). Çalışan talebi ise null olur.</summary>
    public int? InternId { get; set; }

    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public string? RejectionReason { get; set; }

    // ── İki aşamalı onayın izi ────────────────────────────────────────────
    // Hepsi Users'a bakar, Employees'e değil: onaylayan İK uzmanının çalışan
    // kaydı olmayabilir ama hesabı mutlaka vardır.

    /// <summary>1. aşama: yöneticinin (stajyerde mentorun) onayı.</summary>
    public int? ManagerApprovedByUserId { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }

    /// <summary>
    /// 2. aşama: İK onayı. ManagerApprovedByUserId ile AYNI kişi olamaz —
    /// aksi hâlde iki aşamalı onay tek kişinin iki tıkına dönerdi.
    /// </summary>
    public int? HrApprovedByUserId { get; set; }
    public DateTime? HrApprovedAt { get; set; }

    /// <summary>Reddeden hesap — hangi aşamada olduğu Status'ten değil buradan izlenir.</summary>
    public int? RejectedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
