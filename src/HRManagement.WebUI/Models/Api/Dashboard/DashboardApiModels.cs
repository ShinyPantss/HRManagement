namespace HRManagement.WebUI.Models.Api.Dashboard;

// API'nin Models/Dashboard tipleriyle aynı JSON şekli (Contracts projesi yok — elle senkron).
public class HrDashboardResponse
{
    // ── Çekirdek KPI'lar ──
    public int TotalActiveEmployees { get; set; }
    public int OnLeaveNowCount { get; set; }
    public int PendingLeaveRequests { get; set; }
    public int ActiveInterns { get; set; }

    // ── KPI kartlarının uyarı notları ──
    public int OverduePendingCount { get; set; }
    public int OldestPendingDays { get; set; }
    public int EmployeesWithoutAccount { get; set; }
    public int InternsEndingSoon { get; set; }

    // ── Eşikler (ekrandaki metinler bunlardan yazılır) ──
    public int OverdueThresholdDays { get; set; }
    public int UpcomingWindowDays { get; set; }
    public int InternEndingWindowDays { get; set; }

    // ── Dağılımlar ──
    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
    public int GenderUnspecifiedCount { get; set; }

    public List<SeniorityBreakdownResponse> SeniorityBreakdown { get; set; } = [];

    // ── Listeler ──
    public List<OnLeaveNowResponse> OnLeaveNow { get; set; } = [];
    public List<UpcomingLeaveResponse> UpcomingLeaves { get; set; } = [];
    public List<LeaveTrendPointResponse> MonthlyTrend { get; set; } = [];

    // ── Yıllık kişi trendi — çizgi grafik (bu yıl vs geçen yıl, 12'şer ay) ──
    public List<MonthlyPersonCountResponse> YearlyPersonTrend { get; set; } = [];
}

/// <summary>O ay izne çıkan (onaylı izni o ay başlayan) tekil kişi sayısı.</summary>
public class MonthlyPersonCountResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int PersonCount { get; set; }
}

public class SeniorityBreakdownResponse
{
    public int? Seniority { get; set; }
    public int Count { get; set; }
}

public class OnLeaveNowResponse
{
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty; // Çalışan | Stajyer
    public string TypeName { get; set; } = string.Empty;     // Annual | Unpaid | Sick
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpcomingLeaveResponse
{
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WorkingDays { get; set; }
    public int DaysUntilStart { get; set; }
}

public class LeaveTrendPointResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int WorkingDays { get; set; }
    public int RequestCount { get; set; }
}
