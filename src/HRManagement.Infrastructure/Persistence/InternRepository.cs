using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.Infrastructure.Persistence;

public class InternRepository : IInternRepository
{
    private readonly HRManagementDbContext _context;

    public InternRepository(HRManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Intern?> GetByIdAsync(int id)
    {
        return await _context.Interns.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Intern>> GetAllAsync()
    {
        return await _context.Interns.AsNoTracking().ToListAsync();
    }

    public async Task<int> AddAsync(Intern intern)
    {
        _context.Interns.Add(intern);
        await _context.SaveChangesAsync();

        return intern.Id;
    }

    public async Task UpdateAsync(Intern intern)
    {
        var mevcut = await _context.Interns.FirstOrDefaultAsync(i => i.Id == intern.Id);

        if (mevcut is null)
            return;

        mevcut.FirstName = intern.FirstName;
        mevcut.LastName = intern.LastName;
        mevcut.Email = intern.Email;
        mevcut.University = intern.University;
        mevcut.Major = intern.Major;
        mevcut.Grade = intern.Grade;
        mevcut.StartDate = intern.StartDate;
        mevcut.EndDate = intern.EndDate;
        mevcut.MentorId = intern.MentorId;
        mevcut.DepartmentId = intern.DepartmentId;
        mevcut.UnitId = intern.UnitId;
        mevcut.UserId = intern.UserId;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await _context.Interns.Where(i => i.Id == id).ExecuteDeleteAsync();

    }

    public async Task DeleteWithAccountAsync(int internId, int? userId)
    {
        // ExecuteDelete/ExecuteUpdate change tracker'dan GEÇMEZ: doğrudan tek bir
        // DELETE/UPDATE cümlesi gönderirler, entity'leri belleğe çekmezler. Hızlı
        // olmalarının bedeli, SaveChanges'in verdiği örtük transaction'a da
        // dahil olmamalarıdır — dördünü atomik yapmak için transaction elle açılır.
        //
        // Aynı sebeple UpdatedAtInterceptor da bu çağrılarda ÇALIŞMAZ; damgayı
        // aşağıda elle yazıyoruz.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Stajyerin izin talepleri ve hesap talepleri. Çalışandan farklı olarak
        // stajyerin "pasife alma" seçeneği yok (Intern'de IsActive yok), o yüzden
        // izin geçmişi de cascade edilir.
        await _context.LeaveRequests.Where(l => l.InternId == internId).ExecuteDeleteAsync();
        await _context.AccountRequests.Where(a => a.InternId == internId).ExecuteDeleteAsync();

        // Login hesabı: SİLİNMEZ, pasife alınır — başka talepleri (RequestedBy/
        // ReviewedBy) referanslıyor olabilir; hard-delete FK'ye takılır ve
        // denetim izini bozar. Pasif hesap giriş yapamaz.
        if (userId is int uid)
        {
            await _context.Users
                .Where(u => u.Id == uid)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.IsActive, false)
                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
        }

        await _context.Interns.Where(i => i.Id == internId).ExecuteDeleteAsync();

        await transaction.CommitAsync();
    }

    // AnyAsync veritabanında EXISTS üretir — eski "SELECT CASE WHEN EXISTS(...)"
    // cümlelerinin birebir karşılığı, ilk eşleşmede durur.
    public async Task<bool> ExistsByDepartmentIdAsync(int departmentId)
    {
        return await _context.Interns.AnyAsync(i => i.DepartmentId == departmentId);
    }

    public async Task<bool> ExistsByMentorIdAsync(int mentorId)
    {
        return await _context.Interns.AnyAsync(i => i.MentorId == mentorId);
    }

    public async Task<bool> ExistsByUserIdAsync(int userId)
    {
        return await _context.Interns.AnyAsync(i => i.UserId == userId);
    }

    public async Task<Intern?> GetByUserIdAsync(int userId)
    {
        return await _context.Interns.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
    }

    public async Task<Intern?> GetByEmailAsync(string email)
    {
        // FirstOrDefault (Single değil): UQ_Interns_Email kısıtı eklenmeden önce
        // girilmiş mükerrer kayıtlar bu sorguyu patlatmasın.
        return await _context.Interns.AsNoTracking().FirstOrDefaultAsync(i => i.Email == email);
    }

    public async Task<IEnumerable<Intern>> GetByMentorIdAsync(int mentorEmployeeId)
    {
        return await _context.Interns
            .AsNoTracking()
            .Where(i => i.MentorId == mentorEmployeeId)
            .ToListAsync();
    }
}
