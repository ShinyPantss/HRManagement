using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.Infrastructure.Persistence;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly HRManagementDbContext _context;

    public DepartmentRepository(HRManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Department?> GetByIdAsync(int id)
    {
        return await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<Department>> GetAllAsync()
    {
        return await _context.Departments
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> AddAsync(Department department)
    {
        // CreatedAt yazılmaz: konfigürasyonda ValueGeneratedOnAdd olduğu için EF
        // sütunu INSERT'e hiç koymaz, veritabanı default'u (SYSUTCDATETIME) doldurur
        // ve EF üretilen değeri geri okur. Eski elle SQL'deki davranışın aynısı.
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        // SCOPE_IDENTITY() elle okunmuyor: identity değeri SaveChanges sonrası
        // entity'ye EF tarafından yazılmış olur.
        return department.Id;
    }

    public async Task UpdateAsync(Department department)
    {
        // ÖNCE ÇEK, ÜSTÜNE YAZ. Alternatifi olan Update(entity) BÜTÜN sütunları
        // yazardı; bu desende hangi alanların güncellenebilir olduğu burada
        // AÇIKÇA görünür ve EF yalnızca gerçekten değişenleri UPDATE'e koyar.
        // UpdatedAt'i biz yazmıyoruz — UpdatedAtInterceptor damgalıyor.
        var mevcut = await _context.Departments.FirstOrDefaultAsync(d => d.Id == department.Id);

        // Kayıt yoksa sessizce çıkılır: eski "UPDATE ... WHERE Id = @Id" cümlesi de
        // 0 satır etkileyip hata vermiyordu, davranış korunuyor.
        if (mevcut is null)
            return;

        mevcut.Name = department.Name;
        mevcut.Description = department.Description;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        // ExecuteDeleteAsync tek bir DELETE cümlesi üretir — önce SELECT edip
        // entity'yi belleğe almaz. Eski Dapper kodunun birebir karşılığı, hem de
        // bir gidiş-dönüş daha ucuz.
        await _context.Departments
            .Where(d => d.Id == id)
            .ExecuteDeleteAsync();
    }
}
