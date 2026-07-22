using HRManagement.Domain.Entities;

namespace HRManagement.Application.Interfaces;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(int id);
    Task<IEnumerable<LeaveRequest>> GetAllAsync();
    Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId);
    Task<int> AddAsync(LeaveRequest leaveRequest);
    Task<IEnumerable<LeaveRequest>> GetByInternIdAsync(int internId);
    Task UpdateAsync(LeaveRequest leaveRequest);
    Task DeleteAsync(int id);

    // Silme öncesi bağımlılık kontrolü.
    Task<bool> ExistsByEmployeeIdAsync(int employeeId);

    /// <summary>
    /// Tarih çakışması iş kuralı: aynı kişinin (çalışan VEYA stajyer) aktif
    /// (Pending/PendingHr/Approved) bir talebiyle aralık kesişiyor mu?
    /// Reddedilmiş talepler sayılmaz — o tarihlere yeniden talep açılabilmeli.
    /// </summary>
    Task<bool> HasOverlapAsync(int? employeeId, int? internId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Hak dönemi içinde kullanılan + REZERVE (bekleyen) yıllık izin günleri.
    /// Bekleyenler dahildir: dört ayrı bekleyen talep, ayrı ayrı kontrolü geçip
    /// toplamda hakkı katlayamasın diye her talep yerini baştan "rezerve eder".
    /// </summary>
    Task<int> GetUsedAnnualDaysAsync(int employeeId, DateTime periodStart, DateTime periodEndExclusive);
}           