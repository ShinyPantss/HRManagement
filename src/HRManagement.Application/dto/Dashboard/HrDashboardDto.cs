namespace HRManagement.Application.DTOs;

/// <summary>
/// İK/Admin ana sayfa panosunun tek seferde ihtiyaç duyduğu özet metrikler.
/// Tüm sayılar AKTİF çalışanlar üzerinden hesaplanır (pasif kayıtlar sayıma girmez);
/// "şu an izinde" ve "bekleyen izin" çalışan + stajyer taleplerini birlikte kapsar.
///
/// Panonun yönü: "şirket nasıl" değil "bugün ne yapmalıyım". Bu yüzden statik kadro
/// dağılımı (departman headcount) kaldırıldı — o bilgi Organizasyon ekranında zaten
/// daha zengin duruyor ve günlük bakılan bir şey değil. Yerine AKSİYON gerektiren
/// sayılar ve zaman-duyarlı listeler geldi.
/// </summary>
public class HrDashboardDto
{
    // ── Çekirdek KPI'lar ──
    public int TotalActiveEmployees { get; set; }
    public int OnLeaveNowCount { get; set; }      // bugün onaylı izni süren kişi sayısı
    public int PendingLeaveRequests { get; set; } // Pending + PendingHr
    public int ActiveInterns { get; set; }        // stajı bitmemiş (EndDate >= bugün)

    // ── KPI kartlarının uyarı notları ───────────────────────────────────────
    // KPI kartları çıplak sayı yerine durum anlatır: "20 bekleyen talep" değil
    // "3 tanesi 5+ gündür bekliyor". Sayı sıfırsa kart olumlu not gösterir.

    /// <summary>Onay eşiğini aşacak kadar uzun süredir bekleyen talep sayısı.</summary>
    public int OverduePendingCount { get; set; }

    /// <summary>Bekleyenler arasında en eskisinin kaç gündür beklediği.</summary>
    public int OldestPendingDays { get; set; }

    /// <summary>Giriş hesabı olmayan aktif çalışan sayısı (sisteme giremezler).</summary>
    public int EmployeesWithoutAccount { get; set; }

    /// <summary>Stajı yakında bitecek stajyer sayısı — uzatma/çıkış kararı gerekir.</summary>
    public int InternsEndingSoon { get; set; }

    // ── Cinsiyet dağılımı (aktif çalışanlar) ──
    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
    public int GenderUnspecifiedCount { get; set; }   // eski kayıt: cinsiyet boş

    // ── Kadro yapısı: kıdem piramidi (aktif çalışanlar) ──
    public List<SeniorityBreakdownDto> SeniorityBreakdown { get; set; } = [];

    // ── Şu an izinde olanlar (kim, ne zaman dönüyor) ──
    public List<OnLeaveNowDto> OnLeaveNow { get; set; } = [];

    // ── Yaklaşan izinler (önümüzdeki pencere) — kapasite planlaması ──
    public List<UpcomingLeaveDto> UpcomingLeaves { get; set; } = [];

    // ── Son aylardaki izin kullanımı — mevsimsellik ──
    public List<LeaveTrendPointDto> MonthlyTrend { get; set; } = [];

    // ── Yıllık kişi trendi — çizgi grafik (bu yıl vs geçen yıl, 12'şer ay) ──
    public List<MonthlyPersonCountDto> YearlyPersonTrend { get; set; } = [];
}

/// <summary>O ay izne ÇIKAN (onaylı izni o ay başlayan) tekil kişi sayısı.</summary>
public class MonthlyPersonCountDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int PersonCount { get; set; }
}

public class SeniorityBreakdownDto
{
    /// <summary>SeniorityLevel sayısal değeri; null = kıdemi girilmemiş kayıt.</summary>
    public int? Seniority { get; set; }
    public int Count { get; set; }
}

public class OnLeaveNowDto
{
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty; // Çalışan | Stajyer
    public string TypeName { get; set; } = string.Empty;     // izin türü (enum adı)
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }                    // dönüş tarihi (bu tarihe kadar izinli)
}

public class UpcomingLeaveDto
{
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WorkingDays { get; set; }

    /// <summary>Bugünden kaç gün sonra başlıyor — "3 gün sonra" diye gösterilir.</summary>
    public int DaysUntilStart { get; set; }
}

public class LeaveTrendPointDto
{
    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>O ay BAŞLAYAN onaylı izinlerin iş günü toplamı.</summary>
    public int WorkingDays { get; set; }
    public int RequestCount { get; set; }
}