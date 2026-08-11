using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;   // GetDbTransaction()

namespace HRManagement.Infrastructure.Persistence;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly HRManagementDbContext _context;

    public EmployeeRepository(HRManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        return await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        return await _context.Employees.AsNoTracking().ToListAsync();
    }

    public async Task<int> AddAsync(Employee employee)
    {
        // CreatedAt yazılmaz: DB default'u (SYSUTCDATETIME) doldurur — saat tek kaynaktan.
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return employee.Id;
    }

    public async Task UpdateAsync(Employee employee)
    {
        var mevcut = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id);

        if (mevcut is null)
            return;

        mevcut.FirstName = employee.FirstName;
        mevcut.LastName = employee.LastName;
        mevcut.NationalId = employee.NationalId;
        mevcut.DateOfBirth = employee.DateOfBirth;
        mevcut.DepartmentId = employee.DepartmentId;
        mevcut.UnitId = employee.UnitId;
        mevcut.HireDate = employee.HireDate;
        mevcut.Email = employee.Email;
        mevcut.Phone = employee.Phone;
        mevcut.IsActive = employee.IsActive;
        mevcut.UserId = employee.UserId;
        mevcut.ManagerId = employee.ManagerId;
        mevcut.AnnualLeaveDays = employee.AnnualLeaveDays;
        mevcut.Seniority = employee.Seniority;
        mevcut.Gender = employee.Gender;

        // UpdatedAt burada YAZILMAZ — UpdatedAtInterceptor damgalıyor.
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await _context.Employees.Where(e => e.Id == id).ExecuteDeleteAsync();
    }

    public async Task DeleteWithAccountAsync(int employeeId, int? userId)
    {
        // ExecuteDelete/ExecuteUpdate SaveChanges'in örtük transaction'ına dahil
        // olmaz (change tracker'ı atlayıp doğrudan cümle gönderirler), o yüzden
        // üç yazmanın atomikliği için transaction elle açılır. Aynı sebeple
        // UpdatedAtInterceptor da çalışmaz; damga aşağıda elle yazılıyor.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Bu çalışana AİT hesap talepleri (o çalışan hakkında açılanlar) gider.
        await _context.AccountRequests.Where(a => a.EmployeeId == employeeId).ExecuteDeleteAsync();

        // Login hesabı: SİLİNMEZ, pasife alınır. O hesap başka talepleri
        // (RequestedBy/ReviewedBy) referanslıyor olabilir; hard-delete FK'ye
        // takılır ve denetim izini bozar. Pasif hesap giriş yapamaz.
        if (userId is int uid)
        {
            await _context.Users
                .Where(u => u.Id == uid)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.IsActive, false)
                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
        }

        await _context.Employees.Where(e => e.Id == employeeId).ExecuteDeleteAsync();

        await transaction.CommitAsync();
    }

    public async Task<bool> ExistsByDepartmentIdAsync(int departmentId)
    {
        return await _context.Employees.AnyAsync(e => e.DepartmentId == departmentId);
    }

    public async Task<bool> ExistsByUserIdAsync(int userId)
    {
        return await _context.Employees.AnyAsync(e => e.UserId == userId);
    }

    public async Task<bool> ExistsByManagerIdAsync(int managerId)
    {
        return await _context.Employees.AnyAsync(e => e.ManagerId == managerId);
    }

    public async Task<Employee?> GetByUserIdAsync(int userId)
    {
        // FirstOrDefault (Single değil): iş kuralı bir hesabı tek çalışana bağlasa da,
        // elle girilmiş mükerrer bir kayıt tüm giriş akışını 500'e çevirmemeli.
        return await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == userId);
    }

    public async Task<Employee?> GetByEmailAsync(string email)
    {
        return await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Email == email);
    }

    public async Task<Employee?> GetByNationalIdAsync(string nationalId)
    {
        return await _context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.NationalId == nationalId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ÖZYİNELEMELİ SORGULAR — burada LINQ yerine HAM SQL var, bilinçli olarak.
    //
    // EF Core özyinelemeli CTE'yi (WITH ... UNION ALL ... kendine JOIN) LINQ'ten
    // ÜRETEMEZ; böyle bir ifade karşılığı yok. Alternatifi, zinciri C# tarafında
    // döngüyle yürümek olurdu — her kademe için ayrı bir sorgu, yani klasik N+1.
    // 32 kademelik bir org şemasında tek sorgu yerine 32 gidiş-dönüş demek.
    //
    // FromSql/SqlQuery EF'in kendi kapısıdır: sorgu yine DbContext'in bağlantısı
    // ve transaction'ı üzerinden gider, parametreler otomatik parametrelenir
    // (string birleştirme YOK, SQL injection yüzeyi yok). Yani "Dapper kaldı"
    // değil — "EF, SQL'e ihtiyaç duyulan yerde SQL yazmaya izin veriyor".
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<Employee>> GetTeamAsync(int managerEmployeeId)
    {
        // Zinciri AŞAĞI yürüten CTE: önce doğrudan astlar, sonra onların astları...
        // Depth < 32 döngü/aşırı derinlik sigortasıdır.
        //
        // FromSqlInterpolated'ın ardına LINQ operatörü EKLENMEZ: eklenirse EF
        // sorguyu bir alt sorguya sarar ("SELECT ... FROM (<sql>) AS t") ve
        // SQL Server WITH ile başlayan bir ifadeyi orada kabul etmez.
        return await _context.Employees
            .FromSqlInterpolated($@"
                WITH Team AS
                (
                    SELECT Id, 1 AS Depth
                    FROM Employees
                    WHERE ManagerId = {managerEmployeeId}

                    UNION ALL

                    SELECT e.Id, t.Depth + 1
                    FROM Employees e
                    JOIN Team t ON e.ManagerId = t.Id
                    WHERE t.Depth < 32
                )
                SELECT em.*
                FROM Employees em
                JOIN Team t ON t.Id = em.Id")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> IsInManagerChainAsync(int managerEmployeeId, int subordinateEmployeeId)
    {
        // Asttan başlayıp ManagerId'leri YUKARI izleyen CTE. Depth < 32 koruması:
        // A→B→A gibi bir veri hatası (döngü) sorguyu sonsuza sürüklemesin.
        //
        // Entity değil skaler döndüğü için FromSql kullanılamıyor; DbContext'in
        // kendi bağlantısı üzerinden komut çalıştırılıyor. Bağlantı EF'e ait
        // olduğu için açık bir transaction varsa sorgu ona dahil olur.
        var connection = _context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = @"
            WITH Chain AS
            (
                SELECT ManagerId, 1 AS Depth
                FROM Employees
                WHERE Id = @SubordinateId

                UNION ALL

                SELECT e.ManagerId, c.Depth + 1
                FROM Employees e
                JOIN Chain c ON e.Id = c.ManagerId
                WHERE c.Depth < 32
            )
            SELECT CASE WHEN EXISTS
                (SELECT 1 FROM Chain WHERE ManagerId = @ManagerId)
            THEN 1 ELSE 0 END;";

        command.Parameters.Add(new SqlParameter("@SubordinateId", subordinateEmployeeId));
        command.Parameters.Add(new SqlParameter("@ManagerId", managerEmployeeId));

        // Bağlantıyı EF yönetir; burada yalnızca kapalıysa açılır, kapatılmaz —
        // kapatmak, aynı istekteki sonraki EF sorgularını bozardı.
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();

        var sonuc = await command.ExecuteScalarAsync();

        return Convert.ToInt32(sonuc) == 1;
    }
}
