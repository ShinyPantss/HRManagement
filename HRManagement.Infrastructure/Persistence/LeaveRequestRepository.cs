using Dapper;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Infrastructure.Persistence;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public LeaveRequestRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LeaveRequest?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM LeaveRequests WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<LeaveRequest>(sql, new { Id = id });
    }

    public async Task<IEnumerable<LeaveRequest>> GetAllAsync()
    {
        const string sql = "SELECT * FROM LeaveRequests";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LeaveRequest>(sql);
    }

    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId)
    {
        const string sql = "SELECT * FROM LeaveRequests WHERE EmployeeId = @EmployeeId";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LeaveRequest>(sql, new { EmployeeId = employeeId });
    }

    public async Task<int> AddAsync(LeaveRequest leaveRequest)
    {
        const string sql = @"
            INSERT INTO LeaveRequests (EmployeeId, Type, StartDate, EndDate, Description, Status, RejectionReason)
            VALUES (@EmployeeId, @Type, @StartDate, @EndDate, @Description, @Status, @RejectionReason);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, leaveRequest);
    }

    public async Task UpdateAsync(LeaveRequest leaveRequest)
    {
        const string sql = @"
            UPDATE LeaveRequests SET
                EmployeeId = @EmployeeId,
                Type = @Type,
                StartDate = @StartDate,
                EndDate = @EndDate,
                Description = @Description,
                Status = @Status,
                RejectionReason = @RejectionReason
            WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, leaveRequest);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM LeaveRequests WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}