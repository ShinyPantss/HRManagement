using Dapper;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Infrastructure.Persistence;

public class InternNoteRepository : IInternNoteRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public InternNoteRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<InternNote>> GetByInternIdAsync(int internId)
    {
        const string sql = @"
            SELECT * FROM InternNotes
            WHERE InternId = @InternId
            ORDER BY CreatedAt DESC";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<InternNote>(sql, new { InternId = internId });
    }

    public async Task<int> AddAsync(InternNote note)
    {
        const string sql = @"
            INSERT INTO InternNotes (InternId, AuthorUserId, Content)
            VALUES (@InternId, @AuthorUserId, @Content);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, note);
    }
}
