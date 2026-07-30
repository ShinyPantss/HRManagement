namespace HRManagement.API.Models.Dashboard;

// İK/Admin ana sayfa panosu yanıtı — hepsi görüntü alanı (yetki API rol kapısında).
//
// Kurucu yerine init-only property: alan sayısı 20'yi geçtiği için pozisyonel
// argüman listesi tehlikeli hâle geldi. Yan yana iki int'in yerini değiştirmek
// derleyiciden kaçar ve panoda sessizce yanlış sayı gösterir.
public sealed class HrDashboardResponse
{
    // ── Çekirdek KPI'lar ──
    public int TotalActiveEmployees { get; init; }
    public int OnLeaveNowCount { get; init; }
    public int PendingLeaveRequests { get; init; }
    public int ActiveInterns { get; init; }

    // ── KPI kartlarının uyarı notları ──
    public int OverduePendingCount { get; init; }
    public int OldestPendingDays { get; init; }
    public int EmployeesWithoutAccount { get; init; }
    public int InternsEndingSoon { get; init; }

    // ── Eşikler ──
    // Ekrandaki metinler ("5 günden uzun", "14 gün içinde") bu değerlerden yazılır.
    // Sabiti view'a elle kopyalasaydık, Application'daki eşik değişince ekran
    // eski sayıyı yazmaya devam eder ve pano kendi kendine yalan söylerdi.
    public int OverdueThresholdDays { get; init; }
    public int UpcomingWindowDays { get; init; }
    public int InternEndingWindowDays { get; init; }

    // ── Dağılımlar ──
    public int MaleCount { get; init; }
    public int FemaleCount { get; init; }
    public int GenderUnspecifiedCount { get; init; }

    public List<SeniorityBreakdownResponse> SeniorityBreakdown { get; init; } = [];

    // ── Listeler ──
    public List<OnLeaveNowResponse> OnLeaveNow { get; init; } = [];
    public List<UpcomingLeaveResponse> UpcomingLeaves { get; init; } = [];
    public List<LeaveTrendPointResponse> MonthlyTrend { get; init; } = [];
}

public sealed class SeniorityBreakdownResponse
{
    /// <summary>SeniorityLevel sayısal değeri; null = kıdemi girilmemiş kayıt.</summary>
    public int? Seniority { get; init; }
    public int Count { get; init; }
}

public sealed class OnLeaveNowResponse
{
    public string SubjectName { get; init; } = string.Empty;
    public string SubjectType { get; init; } = string.Empty;   // Çalışan | Stajyer
    public string TypeName { get; init; } = string.Empty;      // Annual | Unpaid | Sick
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
}

public sealed class UpcomingLeaveResponse
{
    public string SubjectName { get; init; } = string.Empty;
    public string SubjectType { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int WorkingDays { get; init; }

    /// <summary>Bugünden kaç gün sonra başlıyor.</summary>
    public int DaysUntilStart { get; init; }
}

public sealed class LeaveTrendPointResponse
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int WorkingDays { get; init; }
    public int RequestCount { get; init; }
}
