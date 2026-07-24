using Dapper;
using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Infrastructure.Persistence;

public class AccountRequestRepository : IAccountRequestRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public AccountRequestRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AccountRequest?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM AccountRequests WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<AccountRequest>(sql, new { Id = id });
    }

    public async Task<int> AddAsync(AccountRequest request)
    {
        const string sql = @"
            INSERT INTO AccountRequests (EmployeeId, InternId, RequestedByUserId, SuggestedRole, Note, Status)
            VALUES (@EmployeeId, @InternId, @RequestedByUserId, @SuggestedRole, @Note, @Status);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, request);
    }

    public async Task UpdateAsync(AccountRequest request)
    {
        // Onay/red sonucunu yazar. Onay hesabı açan transaction tarafından da
        // güncellenebildiği için burada yalnızca "durum ilerletme" alanları var.
        const string sql = @"
            UPDATE AccountRequests SET
                Status = @Status,
                RejectionReason = @RejectionReason,
                ReviewedByUserId = @ReviewedByUserId,
                ReviewedAt = @ReviewedAt,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, request);
    }

    public async Task<bool> HasPendingAsync(int? employeeId, int? internId)
    {
        const string sql = @"
            SELECT CASE WHEN EXISTS
            (
                SELECT 1 FROM AccountRequests
                WHERE Status = @Pending
                  AND ( (@EmployeeId IS NOT NULL AND EmployeeId = @EmployeeId)
                     OR (@InternId  IS NOT NULL AND InternId  = @InternId) )
            )
            THEN 1 ELSE 0 END";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new
        {
            EmployeeId = employeeId,
            InternId = internId,
            Pending = (int)AccountRequestStatus.Pending
        });
    }

    public async Task<bool> ExistsForEmployeeAsync(int employeeId)
    {
        const string sql = @"
            SELECT CASE WHEN EXISTS
                (SELECT 1 FROM AccountRequests WHERE EmployeeId = @EmployeeId)
            THEN 1 ELSE 0 END";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { EmployeeId = employeeId });
    }

    public async Task<bool> ExistsForInternAsync(int internId)
    {
        const string sql = @"
            SELECT CASE WHEN EXISTS
                (SELECT 1 FROM AccountRequests WHERE InternId = @InternId)
            THEN 1 ELSE 0 END";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { InternId = internId });
    }

    public async Task<IEnumerable<AccountRequestDto>> GetPendingWithNamesAsync()
    {
        // İlk JOIN'li sorgumuz: kişi adı (çalışan veya stajyerden) + talep eden
        // kullanıcı adı tek sorguda gelir. Rol/durum ham int okunur, C# tarafında
        // enum adına çevrilir (Türkçe etiketleri SQL'e gömmemek için).
        // Departman/Birim adları ve çalışanın kıdemi de gelir: bekleyen ekranı
        // "Tür + rol" yerine POZİSYON (Departman · Birim · Kıdem) gösterir.
        const string sql = @"
            SELECT ar.Id, ar.EmployeeId, ar.InternId,
                   COALESCE(e.FirstName + ' ' + e.LastName, i.FirstName + ' ' + i.LastName) AS SubjectName,
                   CASE WHEN ar.EmployeeId IS NOT NULL THEN N'Çalışan' ELSE N'Stajyer' END AS SubjectType,
                   ar.RequestedByUserId, ru.Username AS RequestedByUsername,
                   d.Name AS DepartmentName, u.Name AS UnitName, e.Seniority AS Seniority,
                   ar.SuggestedRole, ar.Status, ar.Note, ar.CreatedAt
            FROM AccountRequests ar
            LEFT JOIN Employees e ON e.Id = ar.EmployeeId
            LEFT JOIN Interns   i ON i.Id = ar.InternId
            LEFT JOIN Departments d ON d.Id = COALESCE(e.DepartmentId, i.DepartmentId)
            LEFT JOIN Units u ON u.Id = COALESCE(e.UnitId, i.UnitId)
            JOIN Users ru ON ru.Id = ar.RequestedByUserId
            WHERE ar.Status = @Pending
            ORDER BY ar.CreatedAt";

        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<PendingRow>(sql, new { Pending = (int)AccountRequestStatus.Pending });

        return rows.Select(r => new AccountRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            InternId = r.InternId,
            SubjectName = r.SubjectName,
            SubjectType = r.SubjectType,
            RequestedByUserId = r.RequestedByUserId,
            RequestedByUsername = r.RequestedByUsername,
            DepartmentName = r.DepartmentName,
            UnitName = r.UnitName,
            Seniority = r.Seniority,
            SuggestedRole = ((Role)r.SuggestedRole).ToString(),
            Note = r.Note,
            Status = ((AccountRequestStatus)r.Status).ToString(),
            CreatedAt = r.CreatedAt
        });
    }

    // Dapper'ın ham satırı map ettiği ara tip (SuggestedRole/Status int olarak okunur).
    private sealed class PendingRow
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public int? InternId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectType { get; set; } = string.Empty;
        public int RequestedByUserId { get; set; }
        public string RequestedByUsername { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? UnitName { get; set; }
        public int? Seniority { get; set; }
        public int SuggestedRole { get; set; }
        public int Status { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
