namespace HRManagement.WebUI.Models.LeaveRequests;

/// <summary>
/// Yöneticinin "Ekip İzin Takvimi" ekranı. Veri TEK bir uçtan gelmez:
/// görünür ekip /api/employees'ten, her kişinin izinleri
/// /api/leaverequests/employee/{id}'den alınıp burada birleştirilir.
///
/// Maliyeti dürüstçe: ekip başına bir HTTP çağrısı (paralel atılır).
/// Ekip büyüdükçe bu ölçeklenmez; kalıcı çözüm tek sorguda takvim döndüren
/// bir API ucudur (bkz. TeamTooLarge).
/// </summary>
public class TeamCalendarViewModel
{
    public DateTime StartDate { get; set; }
    public int DayCount { get; set; }

    public List<DateTime> Days { get; set; } = [];
    public List<TeamCalendarRow> Rows { get; set; } = [];

    /// <summary>
    /// Ekip, sayfa başına çağrı sayısının makul kalacağı sınırı aştı mı.
    /// Aşarsa takvim ÇİZİLMEZ — sessizce kırpmak yerine durumu söyleriz.
    /// </summary>
    public bool TeamTooLarge { get; set; }

    /// <summary>Sınır aşıldığında gerçek ekip büyüklüğü (mesajda gösterilir).</summary>
    public int TeamSize { get; set; }

    /// <summary>Aynı gün 2+ kişinin izinli olduğu günler — çakışma uyarısı için.</summary>
    public List<DateTime> ClashDays { get; set; } = [];
}

public class TeamCalendarRow
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>Kişinin kendisi mi (yöneticinin kendi satırı ayrı işaretlenir).</summary>
    public bool IsSelf { get; set; }

    public List<TeamCalendarCell> Cells { get; set; } = [];

    /// <summary>Aralıktaki toplam izinli iş günü.</summary>
    public int LeaveDays { get; set; }
}

public class TeamCalendarCell
{
    public DateTime Date { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsToday { get; set; }

    /// <summary>null = o gün izinli değil. Doluysa izin türünün enum adı.</summary>
    public string? Type { get; set; }

    /// <summary>İzin durumu: Approved / Pending / PendingHr.</summary>
    public string? Status { get; set; }
}
