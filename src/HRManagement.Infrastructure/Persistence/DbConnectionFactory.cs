using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HRManagement.Infrastructure.Persistence;

public class DbConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;

        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException(
                                "'ConnectionStrings:DefaultConnection' yapılandırması bulunamadı.");
    }


    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    /// <summary>
    /// Yapay zekâ asistanının ürettiği sorgular için SALT OKUMA bağlantı.
    ///
    /// Ayrı bir veritabanı kullanıcısı (yalnızca db_datareader) kullanılır:
    /// koddaki metin denetimi (SqlReadOnlyGuard) atlatılsa bile veritabanı
    /// yazma girişimini reddeder. Güvenliğin tek bir regex'e bağlı kalmaması için.
    ///
    /// Bağlantı dizesi TEMBEL çözülür — uygulamanın geri kalanı asistan
    /// yapılandırılmamış olsa da çalışmaya devam etsin diye. Eksikse yalnızca
    /// asistan ucu hata verir.
    /// </summary>
    public IDbConnection CreateReadOnlyConnection()
    {
        var readOnlyConnectionString = _configuration.GetConnectionString("ReadOnlyConnection")
            ?? throw new InvalidOperationException(
                "'ConnectionStrings:ReadOnlyConnection' yapılandırması bulunamadı. " +
                "Asistan için salt okuma bir SQL kullanıcısı tanımlayıp bağlantı dizesini " +
                "user-secrets ile verin: dotnet user-secrets set " +
                "\"ConnectionStrings:ReadOnlyConnection\" \"<baglanti-dizesi>\" --project src/HRManagement.API");

        return new SqlConnection(readOnlyConnectionString);
    }
}
