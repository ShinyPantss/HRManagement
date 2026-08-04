namespace HRManagement.Domain.Enums;

/// <summary>
/// İzin talebinin İKİ AŞAMALI onay akışındaki durumu.
///
///   Beklemede ──yönetici onaylar──▶ İK onayı bekliyor ──İK onaylar──▶ Onaylandı
///        │                                 │                             │
///        └──── yönetici reddeder ──────────┴── İK reddeder ────▶ Reddedildi
///                                                                        │
///                                    sahibi, izin BAŞLAMADAN geri çeker ─┴─▶ Cancelled
///
/// Cancelled yalnızca ONAYLI talepten geçilebilen uç durumdur: onaysız talebi
/// geri çekmek kaydı SİLER (rezerve edilmiş hak yok, iz bırakması gerekmez);
/// onaylı talep ise denetim izi için silinmez, Cancelled'a çekilir ve gün
/// hesaplarından (kullanılan bakiye, çakışma, takvim) düşer.
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
    Rejected = 4,     // Reddedildi (hangi aşamada olduğu RejectedByUserId'den anlaşılır)
    Cancelled = 5     // Onaylı talep, izin başlamadan sahibi tarafından geri çekildi
}
