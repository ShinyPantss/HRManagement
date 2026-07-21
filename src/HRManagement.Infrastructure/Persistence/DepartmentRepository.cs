using Dapper;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Infrastructure.Persistence;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DepartmentRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Department?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Departments WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Department>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Department>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Departments";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Department>(sql);
    }

    public async Task<int> AddAsync(Department department)
    {
        const string sql = @"
            INSERT INTO Departments (Name, Description)
            VALUES (@Name, @Description);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, department);
    }

    public async Task UpdateAsync(Department department)
    {
        const string sql = @"
            UPDATE Departments SET
                Name = @Name,
                Description = @Description
            WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, department);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Departments WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}