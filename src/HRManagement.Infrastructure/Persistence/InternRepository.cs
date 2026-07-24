using Dapper;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Infrastructure.Persistence;

public class InternRepository : IInternRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public InternRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Intern?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Interns WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Intern>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Intern>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Interns";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Intern>(sql);
    }

    public async Task<int> AddAsync(Intern intern)
    {
        const string sql = @"
            INSERT INTO Interns (FirstName, LastName, Email, University, Major, Grade, StartDate, EndDate, MentorId, DepartmentId, UnitId, UserId)
            VALUES (@FirstName, @LastName, @Email, @University, @Major, @Grade, @StartDate, @EndDate, @MentorId, @DepartmentId, @UnitId, @UserId);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, intern);
    }

    public async Task UpdateAsync(Intern intern)
    {
        const string sql = @"
            UPDATE Interns SET
                FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email,
                University = @University,
                Major = @Major,
                Grade = @Grade,
                StartDate = @StartDate,
                EndDate = @EndDate,
                MentorId = @MentorId,
                DepartmentId = @DepartmentId,
                UnitId = @UnitId,
                UserId = @UserId,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, intern);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Interns WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeleteWithAccountAsync(int internId, int? userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Stajyerin izin talepleri ve hesap talepleri. Çalışandan farklı olarak
            // stajyerin "pasife alma" seçeneği yok (Intern'de IsActive yok), o yüzden
            // izin talepleri de cascade edilir.
            await connection.ExecuteAsync(
                "DELETE FROM LeaveRequests WHERE InternId = @Id", new { Id = internId }, transaction);

            await connection.ExecuteAsync(
                "DELETE FROM AccountRequests WHERE InternId = @Id", new { Id = internId }, transaction);

            // Login hesabı: SİLİNMEZ, pasife alınır (başka talepleri referanslıyor olabilir).
            if (userId is int uid)
                await connection.ExecuteAsync(
                    "UPDATE Users SET IsActive = 0, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
                    new { Id = uid }, transaction);

            await connection.ExecuteAsync(
                "DELETE FROM Interns WHERE Id = @Id", new { Id = internId }, transaction);

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
                (SELECT 1 FROM Interns WHERE DepartmentId = @DepartmentId)
            THEN 1 ELSE 0 END";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { DepartmentId = departmentId });
    }

    public async Task<bool> ExistsByMentorIdAsync(int mentorId)
    {
        const string sql = @"
            SELECT CASE WHEN EXISTS
                (SELECT 1 FROM Interns WHERE MentorId = @MentorId)
            THEN 1 ELSE 0 END";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { MentorId = mentorId });
    }

    public async Task<bool> ExistsByUserIdAsync(int userId)
    {
        const string sql = @"
            SELECT CASE WHEN EXISTS
                (SELECT 1 FROM Interns WHERE UserId = @UserId)
            THEN 1 ELSE 0 END";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { UserId = userId });
    }

    public async Task<Intern?> GetByUserIdAsync(int userId)
    {
        const string sql = "SELECT * FROM Interns WHERE UserId = @UserId";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Intern>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<Intern>> GetByMentorIdAsync(int mentorEmployeeId)
    {
        const string sql = "SELECT * FROM Interns WHERE MentorId = @MentorId";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Intern>(sql, new { MentorId = mentorEmployeeId });
    }
}