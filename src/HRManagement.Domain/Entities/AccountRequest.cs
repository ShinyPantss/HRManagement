using HRManagement.Domain.Enums;

namespace HRManagement.Domain.Entities;

/// <summary>
/// "Şu kişiye giriş hesabı açılsın" talebi. HR oluşturur, Admin işler.
/// Denetim izini taşır: kim talep etti, kim/ne zaman onayladı-reddetti.
///
/// EmployeeId / InternId'den TAM OLARAK BİRİ dolu (DB'de CHECK kısıtı).
/// Şifre BURADA TUTULMAZ — Admin onaylarken belirler; bekleyen bir satırda
/// şifre durması güvenlik açığı olurdu.
/// </summary>
public class AccountRequest
{
    public int Id { get; set; }

    public int? EmployeeId { get; set; }
    public int? InternId { get; set; }

    /// <summary>Talebi açan hesap (denetim: "kim girdi").</summary>
    public int RequestedByUserId { get; set; }

    /// <summary>HR'ın önerdiği rol. Admin onaylarken değiştirebilir.</summary>
    public Role SuggestedRole { get; set; }

    public string? Note { get; set; }

    public AccountRequestStatus Status { get; set; } = AccountRequestStatus.Pending;
    public string? RejectionReason { get; set; }

    /// <summary>Talebi işleyen Admin ve zamanı.</summary>
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
