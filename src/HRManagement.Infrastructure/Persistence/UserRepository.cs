using Dapper;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;

namespace HRManagement.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public UserRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Users WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = "SELECT * FROM Users WHERE Username = @Username";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM Users WHERE Email = @Email";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Users";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<User>(sql);
    }

    public async Task<int> AddAsync(User user)
    {
        const string sql = @"
            INSERT INTO Users (Username, Email, PasswordHash, Role, IsActive)
            VALUES (@Username, @Email, @PasswordHash, @Role, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, user);
    }

    public async Task UpdateAsync(User user)
    {
        const string sql = @"
            UPDATE Users SET
                Email = @Email,
                Role = @Role,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, user);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Users WHERE Id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<int> CreateForPersonAsync(
        User user, int? employeeId, int? internId, int? accountRequestId, int? reviewerUserId)
    {
        const string insertUser = @"
            INSERT INTO Users (Username, Email, PasswordHash, Role, IsActive)
            VALUES (@Username, @Email, @PasswordHash, @Role, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        const string linkEmployee =
            "UPDATE Employees SET UserId = @UserId, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id";
        const string linkIntern =
            "UPDATE Interns SET UserId = @UserId, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id";
        const string closeRequest = @"
            UPDATE AccountRequests SET
                Status = @Approved, ReviewedByUserId = @ReviewerId,
                ReviewedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @RequestId";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        // Açık transaction: birden çok tabloya (Users + Employees/Interns [+ AccountRequests])
        // yazıp aralarında atomiklik gerektiğimiz için zorunlu. Handler'lar bağlantı/
        // transaction görmez — bu ayrıntı Infrastructure'da kalır.
        using var transaction = connection.BeginTransaction();
        try
        {
            var newUserId = await connection.QuerySingleAsync<int>(insertUser, user, transaction);

            if (employeeId is int eid)
                await connection.ExecuteAsync(linkEmployee, new { UserId = newUserId, Id = eid }, transaction);
            else if (internId is int iid)
                await connection.ExecuteAsync(linkIntern, new { UserId = newUserId, Id = iid }, transaction);

            if (accountRequestId is int reqId)
                await connection.ExecuteAsync(closeRequest, new
                {
                    Approved = (int)Domain.Enums.AccountRequestStatus.Approved,
                    ReviewerId = reviewerUserId,
                    RequestId = reqId
                }, transaction);

            transaction.Commit();
            return newUserId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}