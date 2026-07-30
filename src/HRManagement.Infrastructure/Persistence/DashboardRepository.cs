using System.Data;
using Dapper;
using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;

namespace HRManagement.Infrastructure.Persistence;

/// <summary>
/// Panoyu tek stored procedure çağrısıyla doldurur (dbo.usp_HrDashboard_Get).
/// Projedeki ilk SP kullanımı; diğer repository'ler düz SQL yazmaya devam eder.
///
/// SP tercih edildi çünkü pano beş ayrı sorgunun sonucunu birlikte istiyor:
/// tek çağrı hem round-trip'i hem tutarsız anlık görüntü riskini ortadan kaldırır.
/// </summary>
public class DashboardRepository : IDashboardRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DashboardRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HrDashboardDto> GetHrDashboardAsync(HrDashboardParameters parameters)
    {
        // Enum → int çevrimi BURADA yapılır. Application arayüzü enum taşımaz,
        // SP de sayıları gömmez: değer tek yerde, Domain enum'ında tanımlı kalır.
        var sqlParameters = new
        {
            parameters.Today,
            StatusPending = (int)LeaveStatus.Pending,
            StatusPendingHr = (int)LeaveStatus.PendingHr,
            StatusApproved = (int)LeaveStatus.Approved,
            GenderMale = (int)Gender.Male,
            GenderFemale = (int)Gender.Female,
            parameters.OverdueDays,
            parameters.UpcomingWindowDays,
            parameters.InternEndingWindowDays,
            parameters.TrendMonths
        };

        using var connection = _connectionFactory.CreateConnection();

        using var multi = await connection.QueryMultipleAsync(
            "dbo.usp_HrDashboard_Get", sqlParameters, commandType: CommandType.StoredProcedure);

        // OKUMA SIRASI SP'DEKİ SELECT SIRASIYLA AYNI OLMAK ZORUNDA.
        // Kayarsa derleyici uyarmaz, ekranda sessizce yanlış veri görünür.
        // Araya koşul/erken çıkış eklenmemeli — sıra hep bu beş satırda okunur.
        var summary = await multi.ReadSingleAsync<SummaryRow>();               // 1) Özet
        var seniority = (await multi.ReadAsync<SeniorityRow>()).ToList();      // 2) Kıdem
        var onLeave = (await multi.ReadAsync<LeaveRow>()).ToList();            // 3) Şu an izinde
        var upcoming = (await multi.ReadAsync<UpcomingRow>()).ToList();        // 4) Yaklaşan
        var trend = (await multi.ReadAsync<TrendRow>()).ToList();              // 5) Trend

        return new HrDashboardDto
        {
            TotalActiveEmployees = summary.TotalActiveEmployees,
            OnLeaveNowCount = summary.OnLeaveNowCount,
            PendingLeaveRequests = summary.PendingLeaveRequests,
            ActiveInterns = summary.ActiveInterns,

            OverduePendingCount = summary.OverduePendingCount,
            OldestPendingDays = summary.OldestPendingDays,
            EmployeesWithoutAccount = summary.EmployeesWithoutAccount,
            InternsEndingSoon = summary.InternsEndingSoon,

            MaleCount = summary.MaleCount,
            FemaleCount = summary.FemaleCount,
            GenderUnspecifiedCount = summary.GenderUnspecifiedCount,

            SeniorityBreakdown = seniority
                .Select(s => new SeniorityBreakdownDto { Seniority = s.Seniority, Count = s.Count })
                .ToList(),

            OnLeaveNow = onLeave
                .Select(r => new OnLeaveNowDto
                {
                    SubjectName = r.SubjectName,
                    SubjectType = SubjectTypeOf(r.IsIntern),
                    TypeName = TypeNameOf(r.LeaveTypeId),
                    StartDate = r.StartDate,
                    EndDate = r.EndDate
                })
                .ToList(),

            UpcomingLeaves = upcoming
                .Select(r => new UpcomingLeaveDto
                {
                    SubjectName = r.SubjectName,
                    SubjectType = SubjectTypeOf(r.IsIntern),
                    TypeName = TypeNameOf(r.LeaveTypeId),
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    WorkingDays = r.WorkingDays,
                    DaysUntilStart = r.DaysUntilStart
                })
                .ToList(),

            MonthlyTrend = trend
                .Select(t => new LeaveTrendPointDto
                {
                    Year = t.Year,
                    Month = t.Month,
                    WorkingDays = t.WorkingDays,
                    RequestCount = t.RequestCount
                })
                .ToList()
        };
    }

    // SP metin üretmez, ham değer döner; adlandırma burada yapılır. Böylece
    // enum adları ve Türkçe etiketler tek yerde (C#'ta) kalır — SP'ye gömülse
    // enum değişikliğinde sessizce eskir.
    private static string TypeNameOf(int leaveTypeId) => ((LeaveType)leaveTypeId).ToString();

    private static string SubjectTypeOf(bool isIntern) => isIntern ? "Stajyer" : "Çalışan";

    // ── SP result set'lerinin ham karşılıkları ──────────────────────────────
    // Dapper sütun adlarına göre eşler; adlar SP'deki takma adlarla birebir.

    private sealed class SummaryRow
    {
        public int TotalActiveEmployees { get; set; }
        public int EmployeesWithoutAccount { get; set; }
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public int GenderUnspecifiedCount { get; set; }
        public int ActiveInterns { get; set; }
        public int InternsEndingSoon { get; set; }
        public int OnLeaveNowCount { get; set; }
        public int PendingLeaveRequests { get; set; }
        public int OverduePendingCount { get; set; }
        public int OldestPendingDays { get; set; }
    }

    private sealed class SeniorityRow
    {
        public int? Seniority { get; set; }
        public int Count { get; set; }
    }

    private sealed class LeaveRow
    {
        public string SubjectName { get; set; } = string.Empty;
        public bool IsIntern { get; set; }
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    private sealed class UpcomingRow
    {
        public string SubjectName { get; set; } = string.Empty;
        public bool IsIntern { get; set; }
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WorkingDays { get; set; }
        public int DaysUntilStart { get; set; }
    }

    private sealed class TrendRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int WorkingDays { get; set; }
        public int RequestCount { get; set; }
    }
}
