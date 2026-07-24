using HRManagement.Domain.Enums;

namespace HRManagement.Application.DTOs;

/// <summary>
/// "Onay Bekleyenler" ekranı satırı. Görüntü alanlarının yanında, handler'ın YETKİ
/// süzmesi için gereken alanlar da taşınır (repo → handler arası). Süzme alanları
/// API yanıtına çıkmaz.
/// </summary>
public class PendingApprovalDto
{
    public int Id { get; set; }

    // ── Görüntü ──
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty; // "Çalışan" | "Stajyer"
    public string TypeName { get; set; } = string.Empty;     // izin türü (enum adı)
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WorkingDays { get; set; }
    public LeaveStatus Status { get; set; }                  // aşama (Pending / PendingHr)

    // ── Süzme (view'a gitmez) ──
    public int? EmployeeId { get; set; }
    public int? InternId { get; set; }
    public int? OwnerUserId { get; set; }                    // kendi talebini eleme
    public int? MentorId { get; set; }                       // stajyer için mentor
    public int? ManagerApprovedByUserId { get; set; }        // İK aşamasında iki-göz kuralı
}
