using Dapper;
using HRManagement.Application.DTOs;
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
            INSERT INTO LeaveRequests (EmployeeId, InternId, Type, StartDate, EndDate, WorkingDays, Description, MedicalReport, Status, RejectionReason)
            VALUES (@EmployeeId, @InternId, @Type, @StartDate, @EndDate, @WorkingDays, @Description, @MedicalReport, @Status, @RejectionReason);
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

    public async Task<bool> ExistsByInternIdAsync(int internId)
    {
        const string sql = @"
            SELECT CASE WHEN EXISTS
                (SELECT 1 FROM LeaveRequests WHERE InternId = @InternId)
            THEN 1 ELSE 0 END";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { InternId = internId });
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

    public async Task<IEnumerable<PendingApprovalDto>> GetActionableWithNamesAsync()
    {
        // İşlem bekleyen (Pending + PendingHr) talepler + kişi adı, tip, sahip hesabı,
        // stajyer mentoru ve 1. aşama onaylayanı. Yetki süzmesi handler'da yapılır.
        const string sql = @"
            SELECT lr.Id,
                   COALESCE(e.FirstName + ' ' + e.LastName, i.FirstName + ' ' + i.LastName) AS SubjectName,
                   CASE WHEN lr.EmployeeId IS NOT NULL THEN N'Çalışan' ELSE N'Stajyer' END AS SubjectType,
                   lr.Type, lr.StartDate, lr.EndDate, lr.WorkingDays, lr.Status,
                   lr.EmployeeId, lr.InternId,
                   COALESCE(e.UserId, i.UserId) AS OwnerUserId,
                   i.MentorId AS MentorId,
                   lr.ManagerApprovedByUserId
            FROM LeaveRequests lr
            LEFT JOIN Employees e ON e.Id = lr.EmployeeId
            LEFT JOIN Interns   i ON i.Id = lr.InternId
            WHERE lr.Status IN @Statuses
            ORDER BY lr.StartDate";

        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ActionableRow>(sql, new
        {
            Statuses = new[] { (int)LeaveStatus.Pending, (int)LeaveStatus.PendingHr }
        });

        return rows.Select(r => new PendingApprovalDto
        {
            Id = r.Id,
            SubjectName = r.SubjectName,
            SubjectType = r.SubjectType,
            TypeName = ((LeaveType)r.Type).ToString(),
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            WorkingDays = r.WorkingDays,
            Status = (LeaveStatus)r.Status,
            EmployeeId = r.EmployeeId,
            InternId = r.InternId,
            OwnerUserId = r.OwnerUserId,
            MentorId = r.MentorId,
            ManagerApprovedByUserId = r.ManagerApprovedByUserId
        }).ToList();
    }

    // Dapper'ın ham satırı map ettiği ara tip (Type/Status int okunur, C#'ta enum'a çevrilir).
    private sealed class ActionableRow
    {
        public int Id { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectType { get; set; } = string.Empty;
        public int Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WorkingDays { get; set; }
        public int Status { get; set; }
        public int? EmployeeId { get; set; }
        public int? InternId { get; set; }
        public int? OwnerUserId { get; set; }
        public int? MentorId { get; set; }
        public int? ManagerApprovedByUserId { get; set; }
    }

    public async Task<int> GetTotalUsedAnnualDaysAsync(int employeeId)
    {
        // İŞ GÜNÜ toplamı: oluşturulurken hesaplanıp saklanan WorkingDays sütunu
        // SUM'lanır — hafta sonu matematiği SQL'de tekrarlanmaz (C# ile tek kaynak).
        // Kümülatif model: dönem filtresi YOK; reddedilenler sayılmaz.
        const string sql = @"
            SELECT COALESCE(SUM(WorkingDays), 0)
            FROM LeaveRequests
            WHERE EmployeeId = @EmployeeId
              AND Type = @AnnualType
              AND Status IN @ActiveStatuses";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            EmployeeId = employeeId,
            AnnualType = (int)LeaveType.Annual,
            ActiveStatuses = new[]
            {
                (int)LeaveStatus.Pending,
                (int)LeaveStatus.PendingHr,
                (int)LeaveStatus.Approved
            }
        });
    }
}