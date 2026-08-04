using HRManagement.Application.DTOs;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(int id);
    Task<IEnumerable<LeaveRequest>> GetAllAsync();

    /// <summary>
    /// İşlem BEKLEYEN tüm talepler (Pending + PendingHr), kişi adı + tip + mentor +
    /// aşama alanlarıyla. "Onay Bekleyenler" ekranı bu listeyi kişiye göre süzer;
    /// süzme yetki mantığı LeaveApprovalGuard ile aynıdır (handler'da).
    /// </summary>
    Task<IEnumerable<PendingApprovalDto>> GetActionableWithNamesAsync();

    /// <summary>
    /// TÜM izin talepleri (her durumda) + kişi adı/tip/tür. "İzin Geçmişi" ekranı
    /// (HR/Admin) bunu tek listede gösterir. Sıralama: en yeni başlangıç önce.
    /// </summary>
    Task<IEnumerable<LeaveHistoryDto>> GetAllWithNamesAsync();

    Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId);
    Task<int> AddAsync(LeaveRequest leaveRequest);
    Task<IEnumerable<LeaveRequest>> GetByInternIdAsync(int internId);
    Task UpdateAsync(LeaveRequest leaveRequest);
    Task DeleteAsync(int id);

    // Silme öncesi bağımlılık kontrolü.
    Task<bool> ExistsByEmployeeIdAsync(int employeeId);
    Task<bool> ExistsByInternIdAsync(int internId);

    /// <summary>
    /// Tarih çakışması iş kuralı: aynı kişinin (çalışan VEYA stajyer) aktif
    /// (Pending/PendingHr/Approved) bir talebiyle aralık kesişiyor mu?
    /// Reddedilmiş talepler sayılmaz — o tarihlere yeniden talep açılabilmeli.
    /// </summary>
    Task<bool> HasOverlapAsync(int? employeeId, int? internId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Bu çalışanın ŞİMDİYE KADAR kullandığı + REZERVE ettiği (bekleyen) tüm
    /// yıllık izin günleri. Kümülatif bakiye modeli: dönem penceresi yok, tüm
    /// Annual talepler (Pending/PendingHr/Approved) toplanır. Bekleyenler dahildir
    /// ki dört ayrı bekleyen talep, ayrı ayrı kontrolü geçip hakkı katlamasın.
    /// </summary>
    Task<int> GetTotalUsedAnnualDaysAsync(int employeeId);

    /// <summary>
    /// <see cref="GetTotalUsedAnnualDaysAsync"/>'in TOPLU hâli: tüm çalışanların
    /// kullandığı + rezerve ettiği yıllık izin günleri, çalışan Id'sine göre.
    /// Aynı kuralı uygular (kümülatif, bekleyenler dahil, reddedilenler hariç).
    ///
    /// Neden ayrı bir metot: çalışan listesi ekranı herkesin bakiyesine ihtiyaç
    /// duyuyor. Tek tek çağırmak N+1 üretirdi (300 çalışan = 300 sorgu); bu metot
    /// tek GROUP BY ile döner.
    ///
    /// Hiç yıllık izin talebi olmayan çalışan sözlükte YER ALMAZ — çağıran onu
    /// 0 saymalıdır.
    /// </summary>
    Task<IReadOnlyDictionary<int, int>> GetUsedAnnualDaysByEmployeeAsync();
}           