namespace HRManagement.Domain.Enums;

/// <summary>
/// İzin talebinin İKİ AŞAMALI onay akışındaki durumu.
///
///   Beklemede ──yönetici onaylar──▶ İK onayı bekliyor ──İK onaylar──▶ Onaylandı
///        │                                 │
///        └──── yönetici reddeder ──────────┴── İK reddeder ────▶ Reddedildi
///
/// DİKKAT: Bu sayılar veritabanında LeaveRequests.Status sütununda saklanır.
/// Numaralar değiştirilirse mevcut kayıtlar sessizce başka anlama gelir.
/// Değiştirmek gerekirse 04_hr_module.sql'deki gibi bir veri taşıma şarttır.
/// </summary>
public enum LeaveStatus
{
    Pending = 1,      // Yöneticisinin onayı bekleniyor
    PendingHr = 2,    // Yönetici onayladı, İK onayı bekleniyor
    Approved = 3,     // Her iki onay tamamlandı
    Rejected = 4      // Reddedildi (hangi aşamada olduğu RejectedByUserId'den anlaşılır)
}
