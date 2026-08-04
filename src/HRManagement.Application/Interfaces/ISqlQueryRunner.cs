namespace HRManagement.Application.Interfaces;

/// <summary>Salt okuma sorgusunun sonucu. Satırlar modele JSON olarak verilir.</summary>
/// <param name="RowCount">Döndürülen satır sayısı (kırpma sonrası).</param>
/// <param name="Truncated">Sonuç <see cref="ISqlQueryRunner.MaxRows"/> sınırına takıldı mı.</param>
/// <param name="RowsJson">Satırların JSON dizisi.</param>
public sealed record SqlQueryResult(int RowCount, bool Truncated, string RowsJson);

/// <summary>
/// Asistanın ürettiği SELECT sorgusunu çalıştırır. Adı bilinçli olarak
/// "ReadOnly" içerir: bu arayüzden yazma işlemi YAPILAMAZ.
///
/// İki katmanlı savunmanın ikinci katmanıdır:
///   1) SqlReadOnlyGuard  → metni denetler (Application, test edilebilir)
///   2) bu arayüz         → SALT OKUMA veritabanı kullanıcısıyla bağlanır
/// Guard'da bir açık kalsa bile veritabanı yazma girişimini reddeder.
/// </summary>
public interface ISqlQueryRunner
{
    /// <summary>Tek sorgudan dönebilecek en fazla satır. Aşılırsa sonuç kırpılır.</summary>
    const int MaxRows = 200;

    Task<SqlQueryResult> RunReadOnlyAsync(string sql, CancellationToken cancellationToken);
}
