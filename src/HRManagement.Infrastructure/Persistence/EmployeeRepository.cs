using Dapper;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Infrastructure.Persistence;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public EmployeeRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Employees WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<Employee>(sql, new { Id = id });
    }
    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Employees";

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryAsync<Employee>(sql);
    }

    public async Task<int> AddAsync(Employee employee)
    {
        // CreatedAt yazılmaz: DB default'u (SYSUTCDATETIME) doldurur — saat tek kaynaktan.
        const string sql = @"
            INSERT INTO Employees (FirstName, LastName, NationalId, DateOfBirth, DepartmentId, UnitId, HireDate, Email, Phone, IsActive, UserId, ManagerId, AnnualLeaveDays, Seniority)
            VALUES (@FirstName, @LastName, @NationalId, @DateOfBirth, @DepartmentId, @UnitId, @HireDate, @Email, @Phone, @IsActive, @UserId, @ManagerId, @AnnualLeaveDays, @Seniority);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleAsync<int>(sql, employee);
    }

    public async Task UpdateAsync(Employee employee)
    {
        // UpdatedAt uygulamadan değil DB saatinden yazılır (UTC) — istemci saatine güvenilmez.
        const string sql = @"
            UPDATE Employees SET
                FirstName = @FirstName,
                LastName = @LastName,
                NationalId = @NationalId,
                DateOfBirth = @DateOfBirth,
                DepartmentId = @DepartmentId,
                UnitId = @UnitId,
                HireDate = @HireDate,
                Email = @Email,
                Phone = @Phone,
                IsActive = @IsActive,
                UserId = @UserId,
                ManagerId = @ManagerId,
                AnnualLeaveDays = @AnnualLeaveDays,
                Seniority = @Seniority,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(sql, employee);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Employees WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeleteWithAccountAsync(int employeeId, int? userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Bu çalışana AİT hesap talepleri (o çalışan hakkında açılanlar) gider.
            await connection.ExecuteAsync(
                "DELETE FROM AccountRequests WHERE EmployeeId = @Id",
                new { Id = employeeId }, transaction);

            // Login hesabı: SİLİNMEZ, pasife alınır. O hesap başka talepleri
            // (RequestedBy/ReviewedBy) referanslıyor olabilir; hard-delete FK'ye
            // takılır ve denetim izini bozar. Pasif hesap giriş yapamaz.
            if (userId is int uid)
                await connection.ExecuteAsync(
                    "UPDATE Users SET IsActive = 0, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
                    new { Id = uid }, transaction);

            await connection.ExecuteAsync(
                "DELETE FROM Employees WHERE Id = @Id",
                new { Id = employeeId }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ExistsByDepartmentIdAsync(int departmentId)
    {
        const string sql = @"
            SELECT CASE WHEN EXISTS
                (SELECT 1 FROM Employees WHERE DepartmentId = @DepartmentId)
            THEN 1 ELSE 0 END";

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<bool>(sql, new { DepartmentId = departmentId });
    }

    public async Task<bool> ExistsByUserIdAsync(int userId)
    {
        const string sql = @"
            SELECT CASE WHEN EXISTS
                (SELECT 1 FROM Employees WHERE UserId = @UserId)
            THEN 1 ELSE 0 END";

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<bool>(sql, new { UserId = userId });
    }

    public async Task<bool> ExistsByManagerIdAsync(int managerId)
    {
        const string sql = @"
            SELECT CASE WHEN EXISTS
                (SELECT 1 FROM Employees WHERE ManagerId = @ManagerId)
            THEN 1 ELSE 0 END";

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<bool>(sql, new { ManagerId = managerId });
    }

    public async Task<Employee?> GetByUserIdAsync(int userId)
    {
        // FirstOrDefault (Single değil): iş kuralı bir hesabı tek çalışana bağlasa da,
        // elle girilmiş mükerrer bir kayıt tüm giriş akışını 500'e çevirmemeli.
        const string sql = "SELECT * FROM Employees WHERE UserId = @UserId";

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { UserId = userId });
    }

    public async Task<Employee?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM Employees WHERE Email = @Email";

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { Email = email });
    }

    public async Task<IEnumerable<Employee>> GetTeamAsync(int managerEmployeeId)
    {
        // Zinciri AŞAĞI yürüten özyinelemeli CTE: önce doğrudan astlar, sonra
        // onların astları... Depth < 32 döngü/aşırı derinlik sigortasıdır
        // (IsInManagerChainAsync ile aynı gerekçe, ters yön).
        const string sql = @"
            WITH Team AS
            (
                SELECT Id, 1 AS Depth
                FROM Employees
                WHERE ManagerId = @ManagerId

                UNION ALL

                SELECT e.Id, t.Depth + 1
                FROM Employees e
                JOIN Team t ON e.ManagerId = t.Id
                WHERE t.Depth < 32
            )
            SELECT em.*
            FROM Employees em
            JOIN Team t ON t.Id = em.Id;";

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryAsync<Employee>(sql, new { ManagerId = managerEmployeeId });
    }

    public async Task<bool> IsInManagerChainAsync(int managerEmployeeId, int subordinateEmployeeId)
    {
        // Asttan başlayıp ManagerId'leri yukarı doğru izleyen özyinelemeli CTE.
        // Depth < 32 koruması: A→B→A gibi bir veri hatası (döngü) sorguyu sonsuza
        // sürüklemesin. 32 kademeden derin org şeması zaten veri hatasıdır.
        const string sql = @"
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

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<bool>(
            sql, new { ManagerId = managerEmployeeId, SubordinateId = subordinateEmployeeId });
    }
}