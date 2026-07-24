using Dapper;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Infrastructure.Persistence;

public class InternTaskRepository : IInternTaskRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public InternTaskRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<InternTask?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM InternTasks WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<InternTask>(sql, new { Id = id });
    }

    public async Task<IEnumerable<InternTask>> GetByInternIdAsync(int internId)
    {
        const string sql = @"
            SELECT * FROM InternTasks
            WHERE InternId = @InternId
            ORDER BY CreatedAt DESC";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<InternTask>(sql, new { InternId = internId });
    }

    public async Task<int> AddAsync(InternTask task)
    {
        // CreatedAt ve Status varsayılanları DB'den gelir (SYSUTCDATETIME / 1);
        // Status'u yine de yazıyoruz — entity varsayılanıyla DB varsayılanı
        // birbirinden habersiz kaymasın.
        const string sql = @"
            INSERT INTO InternTasks (InternId, Title, Description, Status, DueDate, CreatedByUserId)
            VALUES (@InternId, @Title, @Description, @Status, @DueDate, @CreatedByUserId);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, task);
    }

    public async Task UpdateAsync(InternTask task)
    {
        const string sql = @"
            UPDATE InternTasks SET
                Title = @Title,
                Description = @Description,
                Status = @Status,
                DueDate = @DueDate,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, task);
    }
}
