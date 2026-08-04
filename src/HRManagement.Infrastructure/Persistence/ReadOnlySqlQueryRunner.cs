using System.Text.Json;
using Dapper;
using HRManagement.Application.Interfaces;

namespace HRManagement.Infrastructure.Persistence;

/// <summary>
/// Asistanın ürettiği SELECT sorgusunu SALT OKUMA bağlantıyla çalıştırır.
///
/// Sorgu metni modelden geldiği için parametreleştirilemez — bu, projenin
/// diğer her yerindeki "asla string birleştirme" kuralının bilinçli istisnasıdır.
/// Riski üç şey birlikte karşılar:
///   1. SqlReadOnlyGuard (Application) — SELECT dışını reddeder
///   2. salt okuma veritabanı kullanıcısı — yazma yetkisi hiç yok
///   3. yalnızca İK/Admin bu ucu çağırabilir
/// </summary>
public class ReadOnlySqlQueryRunner : ISqlQueryRunner
{
    /// <summary>Kaçak sorgu tüm veritabanını kilitlemesin diye kısa tutuluyor.</summary>
    private const int CommandTimeoutSeconds = 15;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // Türkçe karakterler modele okunur gitsin (ç yerine ç).
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    private readonly DbConnectionFactory _connectionFactory;

    public ReadOnlySqlQueryRunner(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SqlQueryResult> RunReadOnlyAsync(string sql, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            sql,
            commandTimeout: CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        using var connection = _connectionFactory.CreateReadOnlyConnection();

        // Dapper dynamic satırları IDictionary<string, object> olarak gelir;
        // bu şekilde JSON'a çevirmek sütun adlarını korur.
        var rows = (await connection.QueryAsync(command))
            .Cast<IDictionary<string, object>>()
            .ToList();

        // Model TOP koymayı unutsa bile bağlam penceresini patlatmasın diye
        // sunucu tarafında da kırpıyoruz.
        var truncated = rows.Count > ISqlQueryRunner.MaxRows;
        if (truncated)
            rows = rows.Take(ISqlQueryRunner.MaxRows).ToList();

        var rowsJson = JsonSerializer.Serialize(rows, JsonOptions);

        return new SqlQueryResult(rows.Count, truncated, rowsJson);
    }
}
