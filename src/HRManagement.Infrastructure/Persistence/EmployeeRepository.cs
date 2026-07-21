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
        const string sql = @"
            INSERT INTO Employees (FirstName, LastName, NationalId, DateOfBirth, DepartmentId, Position, HireDate, Email, Phone, IsActive, UserId)
            VALUES (@FirstName, @LastName, @NationalId, @DateOfBirth, @DepartmentId, @Position, @HireDate, @Email, @Phone, @IsActive, @UserId);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleAsync<int>(sql, employee);
    }

    public async Task UpdateAsync(Employee employee)
    {
        const string sql = @"
            UPDATE Employees SET
                FirstName = @FirstName,
                LastName = @LastName,
                NationalId = @NationalId,
                DateOfBirth = @DateOfBirth,
                DepartmentId = @DepartmentId,
                Position = @Position,
                HireDate = @HireDate,
                Email = @Email,
                Phone = @Phone,
                IsActive = @IsActive,
                UserId = @UserId
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
}