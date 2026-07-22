namespace HRManagement.Domain.Enums;

/// <summary>
/// Hesap açma talebinin durumu. HR talep eder (Pending), Admin işler.
/// Bu sayılar veritabanında AccountRequests.Status sütununda saklanır.
/// </summary>
public enum AccountRequestStatus
{
    Pending = 1,    // HR talep etti, Admin onayı bekleniyor
    Approved = 2,   // Admin onayladı, hesap açıldı
    Rejected = 3    // Admin reddetti (gerekçe RejectionReason'da)
}
