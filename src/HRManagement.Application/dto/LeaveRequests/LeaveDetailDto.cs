using HRManagement.Domain.Enums;

namespace HRManagement.Application.DTOs;

/// <summary>
/// Tek bir izin talebinin DETAY görünümü — kişi, tarih/gün, durum ve iki aşamalı
/// onayın İZİ (kim, ne zaman onayladı/reddetti) isimlerle birlikte. <see cref="CanActNow"/>
/// giriş yapanın bu talebi ŞU AN onaylayıp reddedebileceğini söyler (buton görünürlüğü).
/// </summary>
public class LeaveDetailDto
{
    public int Id { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty; // "Çalışan" | "Stajyer"
    public string TypeName { get; set; } = string.Empty;     // izin türü (enum adı)
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WorkingDays { get; set; }
    public LeaveStatus Status { get; set; }
    public string? Description { get; set; }
    public string? MedicalReport { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }

    // ── İki aşamalı onayın izi (isimlerle) ──
    public string? ManagerApprovedByName { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }
    public string? HrApprovedByName { get; set; }
    public DateTime? HrApprovedAt { get; set; }
    public string? RejectedByName { get; set; }
    public DateTime? RejectedAt { get; set; }

    /// <summary>Giriş yapan bu talebi ŞU AN onaylayabilir/reddedebilir mi? (guard sonucu)</summary>
    public bool CanActNow { get; set; }
}
