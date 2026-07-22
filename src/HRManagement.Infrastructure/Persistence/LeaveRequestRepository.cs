using Dapper;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

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
            INSERT INTO LeaveRequests (EmployeeId, InternId, Type, StartDate, EndDate, Description, Status, RejectionReason)
            VALUES (@EmployeeId, @InternId, @Type, @StartDate, @EndDate, @Description, @Status, @RejectionReason);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, leaveRequest);
    }

    public async Task UpdateAsync(LeaveRequest leaveRequest)
    {
        // Talep sahibi (EmployeeId/InternId) bilinçli olarak güncellenmez:
        // bir talep açıldıktan sonra sahibi değişmez, yalnızca akışı ilerler.
        const string sql = @"
            UPDATE LeaveRequests SET
                Type = @Type,
                StartDate = @StartDate,
                EndDate = @EndDate,
                Description = @Description,
                Status = @Status,
                RejectionReason = @RejectionReason,
                ManagerApprovedByUserId = @ManagerApprovedByUserId,
                ManagerApprovedAt = @ManagerApprovedAt,
                HrApprovedByUserId = @HrApprovedByUserId,
                HrApprovedAt = @HrApprovedAt,
                RejectedByUserId = @RejectedByUserId,
                RejectedAt = @RejectedAt,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, leaveRequest);
    }

    public async Task<IEnumerable<LeaveRequest>> GetByInternIdAsync(int internId)
    {
        const string sql = "SELECT * FROM LeaveRequests WHERE InternId = @InternId";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LeaveRequest>(sql, new { InternId = internId });
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM LeaveRequests WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsByEmployeeIdAsync(int employeeId)
    {
        const string sql = @"
            SELECT CASE WHEN EXISTS
                (SELECT 1 FROM LeaveRequests WHERE EmployeeId = @EmployeeId)
            THEN 1 ELSE 0 END";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { EmployeeId = employeeId });
    }

    public async Task<bool> HasOverlapAsync(int? employeeId, int? internId, DateTime startDate, DateTime endDate)
    {
        // Klasik aralık kesişimi: A.Start <= B.End VE A.End >= B.Start.
        // Statü listesi parametre olarak geçilir; SQL içinde sihirli sayı durmaz.
        const string sql = @"
            SELECT CASE WHEN EXISTS
            (
                SELECT 1 FROM LeaveRequests
                WHERE Status IN @ActiveStatuses
                  AND ( (@EmployeeId IS NOT NULL AND EmployeeId = @EmployeeId)
                     OR (@InternId  IS NOT NULL AND InternId  = @InternId) )
                  AND StartDate <= @EndDate
                  AND EndDate   >= @StartDate
            )
            THEN 1 ELSE 0 END";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new
        {
            EmployeeId = employeeId,
            InternId = internId,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            ActiveStatuses = new[]
            {
                (int)LeaveStatus.Pending,
                (int)LeaveStatus.PendingHr,
                (int)LeaveStatus.Approved
            }
        });
    }

    public async Task<int> GetUsedAnnualDaysAsync(int employeeId, DateTime periodStart, DateTime periodEndExclusive)
    {
        // Gün sayısı = DATEDIFF + 1 (başlangıç ve bitiş günü dahil).
        // Talep, başlangıç tarihinin düştüğü hak dönemine BÜTÜNÜYLE sayılır
        // (dönem sınırını aşan talepler gün gün bölüştürülmez — bilinçli sadelik).
        const string sql = @"
            SELECT COALESCE(SUM(DATEDIFF(DAY, StartDate, EndDate) + 1), 0)
            FROM LeaveRequests
            WHERE EmployeeId = @EmployeeId
              AND Type = @AnnualType
              AND Status IN @ActiveStatuses
              AND StartDate >= @PeriodStart
              AND StartDate <  @PeriodEnd";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            EmployeeId = employeeId,
            AnnualType = (int)LeaveType.Annual,
            PeriodStart = periodStart.Date,
            PeriodEnd = periodEndExclusive.Date,
            ActiveStatuses = new[]
            {
                (int)LeaveStatus.Pending,
                (int)LeaveStatus.PendingHr,
                (int)LeaveStatus.Approved
            }
        });
    }
}