using HRManagement.WebUI.Models.Api.Employees;
using HRManagement.WebUI.Models.Api.LeaveRequests;

namespace HRManagement.WebUI.Models.Home;

/// <summary>
/// Yönetici ve çalışanın ana sayfası. İK/Admin panosundan (HrDashboardResponse)
/// ayrıdır: burada şirket geneli değil, KİŞİNİN kendi tablosu vardır —
/// izin bakiyesi, açık talepleri ve (yöneticiyse) onayını bekleyenler.
///
/// Veri yeni bir uçtan gelmez; var olan /api/employees/me ve
/// /api/leaverequests/pending-approvals uçları birleştirilir.
/// </summary>
public class PersonalHomeViewModel
{
    /// <summary>Kişinin kendi çalışan detayı. Null = hesap bir çalışan kaydına bağlı değil.</summary>
    public EmployeeDetailResponse? Me { get; set; }

    /// <summary>Yalnızca onaylayabilen roller için doldurulur; diğerlerinde boş kalır.</summary>
    public List<PendingApprovalResponse> PendingApprovals { get; set; } = [];

    /// <summary>Onay kutusu bölümü çizilsin mi (rol onaylamaya yetkili mi).</summary>
    public bool CanApprove { get; set; }
}
