using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HRManagement.Infrastructure.Persistence;

public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException(
                                "'ConnectionStrings:DefaultConnection' yapılandırması bulunamadı.");
    }
    
    
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}