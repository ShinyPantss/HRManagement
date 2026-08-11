using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly HRManagementDbContext _context;

    public UserRepository(HRManagementDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.AsNoTracking().ToListAsync();
    }

    public async Task<int> CountActiveAdminsAsync()
    {
        // CountAsync veritabanında SELECT COUNT(*) üretir — kayıtları belleğe çekip
        // saymaz. Enum karşılaştırması SQL'e int olarak çevrilir.
        return await _context.Users.CountAsync(u => u.Role == Role.Admin && u.IsActive);
    }

    public async Task<int> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user.Id;
    }

    public async Task UpdateAsync(User user)
    {
        var mevcut = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

        if (mevcut is null)
            return;

        // Username ve PasswordHash BİLİNÇLİ olarak dışarıda. Kullanıcı adı kimlik
        // anahtarıdır, sonradan değişmez; şifre ise kendi akışında (hash'lenerek)
        // güncellenir — buradan geçseydi düz metin bir değer yanlışlıkla yazılabilirdi.
        mevcut.Email = user.Email;
        mevcut.Role = user.Role;
        mevcut.IsActive = user.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await _context.Users.Where(u => u.Id == id).ExecuteDeleteAsync();
    }

    public async Task<int> CreateForPersonAsync(
        User user, int? employeeId, int? internId, int? accountRequestId, int? reviewerUserId)
    {
        // AÇIK TRANSACTION gerekiyor. Tek bir SaveChangesAsync zaten kendi
        // transaction'ına sarılır, ama burada İKİ tur gerekiyor: kişiye yazılacak
        // UserId, ancak User kaydedildikten SONRA doğuyor (identity). İki
        // SaveChanges'i tek atomik işlem yapmanın yolu transaction'ı elle açmaktır.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();          // 1. tur: Id burada oluşur

        if (employeeId is int eid)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == eid);

            if (employee is not null)
                employee.UserId = user.Id;
        }
        else if (internId is int iid)
        {
            var intern = await _context.Interns.FirstOrDefaultAsync(i => i.Id == iid);

            if (intern is not null)
                intern.UserId = user.Id;
        }

        if (accountRequestId is int reqId)
        {
            var request = await _context.AccountRequests.FirstOrDefaultAsync(a => a.Id == reqId);

            if (request is not null)
            {
                request.Status = AccountRequestStatus.Approved;
                request.ReviewedByUserId = reviewerUserId;
                request.ReviewedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();          // 2. tur: bağlama + talebi kapatma
        await transaction.CommitAsync();

        return user.Id;
    }
}
