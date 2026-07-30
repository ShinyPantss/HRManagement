using HRManagement.Domain.Enums;

namespace HRManagement.Application.DTOs;

/// <summary>
/// "İzin Geçmişi" ekranı satırı — HR/Admin'in TÜM izinleri (her durumda) tek listede
/// gözlemlemesi için. Salt görüntü: onay/işlem burada değil, "Onay Bekleyenler"dedir.
/// Yetki süzme alanı taşımaz; liste zaten yalnızca HR/Admin'e sunulur.
/// </summary>
public class LeaveHistoryDto
{
    public int Id { get; set; }

    /// <summary>
    /// Talebi açan çalışan (stajyer talebinde null). Panodaki "kişi başına birikmiş
    /// izin" gibi hesaplar izni KİŞİYE bağlamayı gerektirir; ada göre eşleştirmek
    /// adaşlarda yanılırdı.
    /// </summary>
    public int? EmployeeId { get; set; }

    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty; // "Çalışan" | "Stajyer"

    /// <summary>
    /// Kişinin departmanı — rapordaki kırılımın dayanağı. Satırda taşınır ki
    /// istemci ayrıca çalışan listesi çekip AD üzerinden eşleştirmek zorunda
    /// kalmasın (adaşlarda yanılırdı). Departmanı yoksa null.
    /// </summary>
    public string? DepartmentName { get; set; }
    public string TypeName { get; set; } = string.Empty;     // izin türü (enum adı)
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WorkingDays { get; set; }
    public LeaveStatus Status { get; set; }                  // Pending / PendingHr / Approved / Rejected
    public string? Description { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
