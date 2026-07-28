namespace HRManagement.Application.DTOs;

/// <summary>
/// İK/Admin ana sayfa panosunun tek seferde ihtiyaç duyduğu özet metrikler.
/// Tüm sayılar AKTİF çalışanlar üzerinden hesaplanır (pasif kayıtlar sayıma girmez);
/// "şu an izinde" ve "bekleyen izin" çalışan + stajyer taleplerini birlikte kapsar.
/// </summary>
public class HrDashboardDto
{
    // ── Çekirdek KPI'lar ──
    public int TotalActiveEmployees { get; set; }
    public int OnLeaveNowCount { get; set; }      // bugün onaylı izni süren kişi sayısı
    public int PendingLeaveRequests { get; set; } // Pending + PendingHr
    public int ActiveInterns { get; set; }        // stajı bitmemiş (EndDate >= bugün)

    // ── Cinsiyet dağılımı (aktif çalışanlar) ──
    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
    public int GenderUnspecifiedCount { get; set; }   // eski kayıt: cinsiyet boş

    // ── Departmana göre aktif çalışan dağılımı (çoktan aza) ──
    public List<DepartmentHeadcountDto> DepartmentHeadcounts { get; set; } = [];

    // ── Şu an izinde olanlar (kim, ne zaman dönüyor) ──
    public List<OnLeaveNowDto> OnLeaveNow { get; set; } = [];
}

public class DepartmentHeadcountDto
{
    public string DepartmentName { get; set; } = string.Empty;
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
