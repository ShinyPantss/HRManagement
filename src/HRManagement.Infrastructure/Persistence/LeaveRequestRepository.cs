using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.Infrastructure.Persistence;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    /// <summary>
    /// "Aktif" sayılan durumlar: bakiyeyi tüketen ve tarih çakışmasına giren
    /// talepler. Reddedilmiş/iptal edilmiş talepler sayılmaz — o tarihlere
    /// yeniden talep açılabilmeli.
    ///
    /// Eskiden bu üçlü dizi HasOverlapAsync, GetTotalUsedAnnualDaysAsync ve
    /// GetUsedAnnualDaysByEmployeeAsync içinde AYRI AYRI yazılıydı; koddaki not da
    /// "biri değişirse diğeri de değişmeli" diyordu. Artık tek yerde.
    /// </summary>
    private static readonly LeaveStatus[] AktifDurumlar =
    [
        LeaveStatus.Pending,
        LeaveStatus.PendingHr,
        LeaveStatus.Approved
    ];

    /// <summary>Onay ekranına düşen, henüz sonuçlanmamış durumlar.</summary>
    private static readonly LeaveStatus[] IslemBekleyenDurumlar =
    [
        LeaveStatus.Pending,
        LeaveStatus.PendingHr
    ];

    private readonly HRManagementDbContext _context;

    public LeaveRequestRepository(HRManagementDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRequest?> GetByIdAsync(int id)
    {
        return await _context.LeaveRequests.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<IEnumerable<LeaveRequest>> GetAllAsync()
    {
        return await _context.LeaveRequests.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaveRequest>> GetByInternIdAsync(int internId)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.InternId == internId)
            .ToListAsync();
    }

    public async Task<int> AddAsync(LeaveRequest leaveRequest)
    {
        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();

        return leaveRequest.Id;
    }

    public async Task UpdateAsync(LeaveRequest leaveRequest)
    {
        var mevcut = await _context.LeaveRequests.FirstOrDefaultAsync(l => l.Id == leaveRequest.Id);

        if (mevcut is null)
            return;

        // Talep sahibi (EmployeeId/InternId) BİLİNÇLİ olarak güncellenmez: bir talep
        // açıldıktan sonra sahibi değişmez, yalnızca akışı ilerler. WorkingDays de
        // dışarıda — oluşturulurken hesaplanır ve sabittir.
        mevcut.Type = leaveRequest.Type;
        mevcut.StartDate = leaveRequest.StartDate;
        mevcut.EndDate = leaveRequest.EndDate;
        mevcut.Description = leaveRequest.Description;
        mevcut.Status = leaveRequest.Status;
        mevcut.RejectionReason = leaveRequest.RejectionReason;
        mevcut.ManagerApprovedByUserId = leaveRequest.ManagerApprovedByUserId;
        mevcut.ManagerApprovedAt = leaveRequest.ManagerApprovedAt;
        mevcut.HrApprovedByUserId = leaveRequest.HrApprovedByUserId;
        mevcut.HrApprovedAt = leaveRequest.HrApprovedAt;
        mevcut.RejectedByUserId = leaveRequest.RejectedByUserId;
        mevcut.RejectedAt = leaveRequest.RejectedAt;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await _context.LeaveRequests.Where(l => l.Id == id).ExecuteDeleteAsync();
    }

    public async Task<bool> ExistsByEmployeeIdAsync(int employeeId)
    {
        return await _context.LeaveRequests.AnyAsync(l => l.EmployeeId == employeeId);
    }

    public async Task<bool> ExistsByInternIdAsync(int internId)
    {
        return await _context.LeaveRequests.AnyAsync(l => l.InternId == internId);
    }

    public async Task<bool> HasOverlapAsync(
        int? employeeId, int? internId, DateTime startDate, DateTime endDate)
    {
        // Saat bileşeni sorgudan ÖNCE atılır: ifade ağacının içinde .Date çağırmak
        // SQL'e CAST olarak çevrilir ve index kullanımını engelleyebilir.
        var baslangic = startDate.Date;
        var bitis = endDate.Date;

        // YARI AÇIK aralık kesişimi: A.Start < B.End VE A.End > B.Start.
        // Bitiş günü izin değil, işe dönüş günüdür — bu yüzden "benim bitişim
        // = onun başlangıcı" çakışma SAYILMAZ.
        return await _context.LeaveRequests.AnyAsync(l =>
            AktifDurumlar.Contains(l.Status)
            && ((employeeId != null && l.EmployeeId == employeeId)
                || (internId != null && l.InternId == internId))
            && l.StartDate < bitis
            && l.EndDate > baslangic);
    }

    public async Task<IEnumerable<PendingApprovalDto>> GetActionableWithNamesAsync()
    {
        var rows = await (
            from lr in _context.LeaveRequests.AsNoTracking()
            where IslemBekleyenDurumlar.Contains(lr.Status)

            from e in _context.Employees.Where(x => x.Id == lr.EmployeeId).DefaultIfEmpty()
            from i in _context.Interns.Where(x => x.Id == lr.InternId).DefaultIfEmpty()

            orderby lr.StartDate
            select new ActionableRow
            {
                Id = lr.Id,
                SubjectName = e != null
                    ? e.FirstName + " " + e.LastName
                    : i.FirstName + " " + i.LastName,
                SubjectType = lr.EmployeeId != null ? "Çalışan" : "Stajyer",
                Type = lr.Type,
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                WorkingDays = lr.WorkingDays,
                Status = lr.Status,
                EmployeeId = lr.EmployeeId,
                InternId = lr.InternId,
                OwnerUserId = e != null ? e.UserId : (i != null ? i.UserId : null),
                MentorId = i != null ? i.MentorId : null,
                ManagerApprovedByUserId = lr.ManagerApprovedByUserId
            }).ToListAsync();

        return rows.Select(r => new PendingApprovalDto
        {
            Id = r.Id,
            SubjectName = r.SubjectName,
            SubjectType = r.SubjectType,
            TypeName = r.Type.ToString(),
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            WorkingDays = r.WorkingDays,
            Status = r.Status,
            EmployeeId = r.EmployeeId,
            InternId = r.InternId,
            OwnerUserId = r.OwnerUserId,
            MentorId = r.MentorId,
            ManagerApprovedByUserId = r.ManagerApprovedByUserId
        }).ToList();
    }

    public async Task<IEnumerable<LeaveHistoryDto>> GetAllWithNamesAsync()
    {
        // Durum filtresi YOK: bu bir GEÇMİŞ listesidir, Approved/Rejected de gelir.
        var rows = await (
            from lr in _context.LeaveRequests.AsNoTracking()

            from e in _context.Employees.Where(x => x.Id == lr.EmployeeId).DefaultIfEmpty()
            from i in _context.Interns.Where(x => x.Id == lr.InternId).DefaultIfEmpty()

            // Departman kişiden gelir; talep ya çalışana ya stajyere ait olduğu
            // için hangisi doluysa onun departmanına bağlanır.
            from d in _context.Departments
                .Where(x => x.Id == (e != null ? e.DepartmentId : i.DepartmentId))
                .DefaultIfEmpty()

            orderby lr.StartDate descending
            select new HistoryRow
            {
                Id = lr.Id,
                EmployeeId = lr.EmployeeId,
                SubjectName = e != null
                    ? e.FirstName + " " + e.LastName
                    : i.FirstName + " " + i.LastName,
                SubjectType = lr.EmployeeId != null ? "Çalışan" : "Stajyer",
                DepartmentName = d != null ? d.Name : null,
                Type = lr.Type,
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                WorkingDays = lr.WorkingDays,
                Status = lr.Status,
                Description = lr.Description,
                RejectionReason = lr.RejectionReason,
                CreatedAt = lr.CreatedAt
            }).ToListAsync();

        return rows.Select(r => new LeaveHistoryDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            SubjectName = r.SubjectName,
            SubjectType = r.SubjectType,
            DepartmentName = r.DepartmentName,
            TypeName = r.Type.ToString(),
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            WorkingDays = r.WorkingDays,
            Status = r.Status,
            Description = r.Description,
            RejectionReason = r.RejectionReason,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<int> GetTotalUsedAnnualDaysAsync(int employeeId)
    {
        // (int?) cast'i şart: SQL'de hiç satır yoksa SUM NULL döner ve EF onu
        // int'e map edemeyip patlar. Nullable toplayıp ?? 0 demek, eski
        // COALESCE(SUM(...), 0) ifadesinin birebir karşılığı.
        return await _context.LeaveRequests
            .Where(l => l.EmployeeId == employeeId
                        && l.Type == LeaveType.Annual
                        && AktifDurumlar.Contains(l.Status))
            .SumAsync(l => (int?)l.WorkingDays) ?? 0;
    }

    public async Task<IReadOnlyDictionary<int, int>> GetUsedAnnualDaysByEmployeeAsync()
    {
        // GetTotalUsedAnnualDaysAsync ile AYNI kural, gruplanmış hâli. Tek GROUP BY
        // ile döner: çalışan başına ayrı sorgu (N+1) yerine tek gidiş-dönüş.
        // InternId'li talepler dışarıda — stajyerler yıllık izin biriktirmez.
        var rows = await _context.LeaveRequests
            .Where(l => l.EmployeeId != null
                        && l.Type == LeaveType.Annual
                        && AktifDurumlar.Contains(l.Status))
            .GroupBy(l => l.EmployeeId!.Value)
            .Select(g => new { EmployeeId = g.Key, UsedDays = g.Sum(l => l.WorkingDays) })
            .ToListAsync();

        // Hiç yıllık izin talebi olmayan çalışan sözlükte YER ALMAZ — çağıran
        // onu 0 saymalıdır (arayüz belgesinde de böyle yazıyor).
        return rows.ToDictionary(row => row.EmployeeId, row => row.UsedDays);
    }

    /// <summary>Onay ekranı sorgusunun ham satırı; enum'lar DTO'da ada çevrilir.</summary>
    private sealed class ActionableRow
    {
        public int Id { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectType { get; set; } = string.Empty;
        public LeaveType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WorkingDays { get; set; }
        public LeaveStatus Status { get; set; }
        public int? EmployeeId { get; set; }
        public int? InternId { get; set; }
        public int? OwnerUserId { get; set; }
        public int? MentorId { get; set; }
        public int? ManagerApprovedByUserId { get; set; }
    }

    /// <summary>Geçmiş listesi sorgusunun ham satırı.</summary>
    private sealed class HistoryRow
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectType { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public LeaveType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WorkingDays { get; set; }
        public LeaveStatus Status { get; set; }
        public string? Description { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
