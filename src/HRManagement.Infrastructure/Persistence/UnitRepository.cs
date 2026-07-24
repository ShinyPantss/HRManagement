using Dapper;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Infrastructure.Persistence;

public class UnitRepository : IUnitRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public UnitRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Unit>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Units ORDER BY DepartmentId, Name";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Unit>(sql);
    }

    public async Task<Unit?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Units WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Unit>(sql, new { Id = id });
    }
}
