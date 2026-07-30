namespace HRManagement.WebUI.Models.LeaveRequests;

/// <summary>
/// İzin raporunun HESAPLANMIŞ hâli. View sayı üretmez, yalnızca gösterir:
/// aynı metriğin ekranda ve CSV'de farklı çıkması bu sayede imkânsız olur.
///
/// Girdi, zaman filtresi UYGULANDIKTAN sonraki listedir; dolayısıyla her sayı
/// başlıktaki aralığın içini anlatır.
/// </summary>
public class LeaveReportViewModel
{
    public LeaveRequestListViewModel Source { get; init; } = new();

    public bool IsAllView => Source.IsAllView;
    public string RangeText => Source.RangeText;
    public int TotalRequests { get; init; }
    public int TotalDays { get; init; }

    /// <summary>İzin KULLANAN kişi sayısı — kadro değil. "Kişi başı" ortalamaların paydası.</summary>
    public int PeopleCount { get; init; }

    public int ApprovedCount { get; init; }
    public int ApprovedDays { get; init; }
    public int PendingCount { get; init; }
    public int RejectedCount { get; init; }

    public int LongestLeaveDays { get; init; }
    public string? LongestLeaveOwner { get; init; }

    public List<TypeSlice> ByType { get; init; } = [];
    public List<StatusSlice> ByStatus { get; init; } = [];
    public List<GroupSlice> ByDepartment { get; init; } = [];
    public List<GroupSlice> TopPeople { get; init; } = [];
    public List<MonthSlice> ByMonth { get; init; } = [];

    // ── Türetilmiş oranlar ───────────────────────────────────────────────────
    // Payda sıfırken 0 dönerler: rapor "NaN" ya da "∞" göstermemeli.

    /// <summary>Sonuçlanmış taleplerin kaçı onaylandı. Süreçtekiler paydaya girmez —
    /// henüz karar verilmemiş bir talebi "onaylanmadı" saymak oranı yanlış düşürürdü.</summary>
    public int ApprovalRate =>
        ApprovedCount + RejectedCount == 0
            ? 0
            : (int)Math.Round(100.0 * ApprovedCount / (ApprovedCount + RejectedCount));

    public double AverageDaysPerRequest =>
        TotalRequests == 0 ? 0 : Math.Round((double)TotalDays / TotalRequests, 1);

    public double AverageDaysPerPerson =>
        PeopleCount == 0 ? 0 : Math.Round((double)TotalDays / PeopleCount, 1);

    /// <summary>Hastalık izninin toplam gün içindeki payı — devamsızlık göstergesi.</summary>
    public int SickSharePercent
    {
        get
        {
            if (TotalDays == 0) return 0;
            var sickDays = ByType.FirstOrDefault(t => t.Type == "Sick")?.Days ?? 0;
            return (int)Math.Round(100.0 * sickDays / TotalDays);
        }
    }

    public MonthSlice? BusiestMonth =>
        ByMonth.Where(m => m.Days > 0).OrderByDescending(m => m.Days).FirstOrDefault();

    public bool HasData => TotalRequests > 0;

    // ── Satır tipleri ────────────────────────────────────────────────────────

    public class TypeSlice
    {
        public string Type { get; init; } = "";
        public string Label { get; init; } = "";
        public string BadgeClass { get; init; } = "";
        public int Count { get; init; }
        public int Days { get; init; }
        public int PercentOfDays { get; init; }
    }

    public class StatusSlice
    {
        public string Status { get; init; } = "";
        public string Label { get; init; } = "";
        public string BadgeClass { get; init; } = "";
        public int Count { get; init; }
        public int PercentOfCount { get; init; }
    }

    /// <summary>Departman veya kişi kırılımı — ikisi de aynı şekli taşır.</summary>
    public class GroupSlice
    {
        public string Name { get; init; } = "";
        public string? Note { get; init; }
        public int PeopleCount { get; init; }
        public int RequestCount { get; init; }
        public int Days { get; init; }
        public int PercentOfMax { get; init; }

        public double AveragePerPerson =>
            PeopleCount == 0 ? 0 : Math.Round((double)Days / PeopleCount, 1);
    }

    public class MonthSlice
    {
        public int Year { get; init; }
        public int Month { get; init; }
        public string Label { get; init; } = "";
        public int RequestCount { get; init; }
        public int Days { get; init; }
        public int PercentOfMax { get; init; }
    }
}
