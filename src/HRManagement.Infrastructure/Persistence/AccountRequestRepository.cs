using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.Infrastructure.Persistence;

public class AccountRequestRepository : IAccountRequestRepository
{
    private readonly HRManagementDbContext _context;

    public AccountRequestRepository(HRManagementDbContext context)
    {
        _context = context;
    }

    public async Task<AccountRequest?> GetByIdAsync(int id)
    {
        return await _context.AccountRequests.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<int> AddAsync(AccountRequest request)
    {
        _context.AccountRequests.Add(request);
        await _context.SaveChangesAsync();

        return request.Id;
    }

    public async Task UpdateAsync(AccountRequest request)
    {
        var mevcut = await _context.AccountRequests.FirstOrDefaultAsync(a => a.Id == request.Id);

        if (mevcut is null)
            return;

        // Yalnızca "durum ilerletme" alanları. Talebin öznesi (EmployeeId/InternId),
        // talep eden ve önerilen rol açıldıktan sonra değişmez.
        mevcut.Status = request.Status;
        mevcut.RejectionReason = request.RejectionReason;
        mevcut.ReviewedByUserId = request.ReviewedByUserId;
        mevcut.ReviewedAt = request.ReviewedAt;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasPendingAsync(int? employeeId, int? internId)
    {
        return await _context.AccountRequests.AnyAsync(a =>
            a.Status == AccountRequestStatus.Pending
            && ((employeeId != null && a.EmployeeId == employeeId)
                || (internId != null && a.InternId == internId)));
    }

    public async Task<bool> ExistsForEmployeeAsync(int employeeId)
    {
        return await _context.AccountRequests.AnyAsync(a => a.EmployeeId == employeeId);
    }

    public async Task<bool> ExistsForInternAsync(int internId)
    {
        return await _context.AccountRequests.AnyAsync(a => a.InternId == internId);
    }

    public async Task<IEnumerable<AccountRequestDto>> GetPendingWithNamesAsync()
    {
        // Entity'lerde navigation property olmadığı için JOIN'ler AÇIK yazılır.
        // "from x in Sorgu.Where(...).DefaultIfEmpty()" kalıbı LEFT JOIN üretir;
        // DefaultIfEmpty olmadan INNER JOIN olurdu ve çalışanı olmayan (stajyer)
        // talepler listeden düşerdi.
        var query =
            from ar in _context.AccountRequests.AsNoTracking()
            where ar.Status == AccountRequestStatus.Pending

            join ru in _context.Users on ar.RequestedByUserId equals ru.Id   // INNER: talep eden hep vardır

            from e in _context.Employees.Where(x => x.Id == ar.EmployeeId).DefaultIfEmpty()
            from i in _context.Interns.Where(x => x.Id == ar.InternId).DefaultIfEmpty()

            // Departman ve birim, kişi hangisiyse ONUN üzerinden bulunur —
            // eski sorgudaki COALESCE(e.DepartmentId, i.DepartmentId) mantığı.
            from d in _context.Departments
                .Where(x => x.Id == (e != null ? e.DepartmentId : i.DepartmentId))
                .DefaultIfEmpty()
            from u in _context.Units
                .Where(x => x.Id == (e != null ? e.UnitId : i.UnitId))
                .DefaultIfEmpty()

            orderby ar.CreatedAt
            select new PendingRow
            {
                Id = ar.Id,
                EmployeeId = ar.EmployeeId,
                InternId = ar.InternId,
                SubjectName = e != null
                    ? e.FirstName + " " + e.LastName
                    : i.FirstName + " " + i.LastName,
                SubjectType = ar.EmployeeId != null ? "Çalışan" : "Stajyer",
                RequestedByUserId = ar.RequestedByUserId,
                RequestedByUsername = ru.Username,
                DepartmentName = d != null ? d.Name : null,
                UnitName = u != null ? u.Name : null,
                Seniority = e != null ? (int?)e.Seniority : null,
                SuggestedRole = ar.SuggestedRole,
                Status = ar.Status,
                Note = ar.Note,
                CreatedAt = ar.CreatedAt
            };

        var rows = await query.ToListAsync();

        // Enum → ad çevrimi BELLEKTE yapılır: ToString() SQL'e çevrilemez ve
        // Türkçe/İngilizce etiketleri veritabanına gömmek istemiyoruz.
        return rows.Select(r => new AccountRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            InternId = r.InternId,
            SubjectName = r.SubjectName,
            SubjectType = r.SubjectType,
            RequestedByUserId = r.RequestedByUserId,
            RequestedByUsername = r.RequestedByUsername,
            DepartmentName = r.DepartmentName ?? string.Empty,
            UnitName = r.UnitName,
            Seniority = r.Seniority,
            SuggestedRole = r.SuggestedRole.ToString(),
            Note = r.Note,
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt
        });
    }

    /// <summary>
    /// SQL'den okunan ham satır. Enum'lar burada hâlâ enum tipinde durur; DTO'ya
    /// çevrilirken adlarına dönüşürler.
    /// </summary>
    private sealed class PendingRow
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public int? InternId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectType { get; set; } = string.Empty;
        public int RequestedByUserId { get; set; }
        public string RequestedByUsername { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? UnitName { get; set; }
        public int? Seniority { get; set; }
        public Role SuggestedRole { get; set; }
        public AccountRequestStatus Status { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
