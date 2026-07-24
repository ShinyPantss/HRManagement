using Dapper;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Infrastructure.Persistence;

public class EmployeeNoteRepository : IEmployeeNoteRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public EmployeeNoteRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<EmployeeNote>> GetByEmployeeIdAsync(int employeeId)
    {
        const string sql = @"
            SELECT * FROM EmployeeNotes
            WHERE EmployeeId = @EmployeeId
            ORDER BY CreatedAt DESC";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<EmployeeNote>(sql, new { EmployeeId = employeeId });
    }

    public async Task<int> AddAsync(EmployeeNote note)
    {
        // CreatedAt DB default'undan gelir (SYSUTCDATETIME) — diğer repo'larla aynı desen.
        const string sql = @"
            INSERT INTO EmployeeNotes (EmployeeId, AuthorUserId, Content)
            VALUES (@EmployeeId, @AuthorUserId, @Content);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, note);
    }
}
