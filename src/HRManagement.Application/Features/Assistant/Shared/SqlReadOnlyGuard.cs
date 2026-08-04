using System.Text.RegularExpressions;

namespace HRManagement.Application.Features.Assistant.Shared;

/// <summary>
/// Asistanın ürettiği SQL'i çalıştırmadan ÖNCE denetler.
///
/// Neden Application'da: bu bir GÜVENLİK KURALIDIR ("yapay zekâ yalnızca okuyabilir"),
/// SQL sözdizimi ayrıntısı değil. Burada durduğu için birim testi yazılabilir —
/// Infrastructure'a konsaydı testlerin ulaşamayacağı bir yerde yaşardı.
///
/// Bu tek başına YETERLİ DEĞİLDİR. İkinci katman, sorguyu salt okuma yetkisi olan
/// bir veritabanı kullanıcısıyla çalıştırmaktır (bkz. ISqlQueryRunner). Bir metin
/// denetimi her zaman atlatılabilir; veritabanı izni atlatılamaz.
/// </summary>
public static class SqlReadOnlyGuard
{
    /// <summary>
    /// Yasaklı anahtar kelimeler. Tam kelime olarak aranır ki "CreatedAt" kolonu
    /// "CREATE" yüzünden reddedilmesin.
    /// </summary>
    private static readonly string[] ForbiddenKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE",
        "MERGE", "EXEC", "EXECUTE", "GRANT", "REVOKE", "DENY", "BACKUP",
        "RESTORE", "SHUTDOWN", "RECONFIGURE", "OPENROWSET", "OPENQUERY",
        "OPENDATASOURCE", "BULK", "WAITFOR", "INTO"
    ];

    /// <summary>
    /// Sorgu güvenli mi? Değilse <paramref name="reason"/> kullanıcıya/modele
    /// gösterilebilecek bir gerekçe taşır.
    /// </summary>
    public static bool IsReadOnly(string? sql, out string reason)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            reason = "Sorgu boş.";
            return false;
        }

        // Yorumlar denetimi atlatmak için kullanılabilir (ör. "SEL/**/ECT" ya da
        // "-- " ile kalanı gizlemek). Denetimi yorumsuz metin üzerinde yapıyoruz.
        var stripped = StripComments(sql);

        if (string.IsNullOrWhiteSpace(stripped))
        {
            reason = "Sorgu yalnızca yorum içeriyor.";
            return false;
        }

        // Noktalı virgül ifade zincirlemesine izin verir:
        // "SELECT 1; DROP TABLE Employees". Sondaki tek bir tanesi zararsız,
        // ortadaki değil — bu yüzden sondakini atıp kalanda hiç aramıyoruz.
        var trimmed = stripped.Trim().TrimEnd(';').Trim();

        if (trimmed.Contains(';'))
        {
            reason = "Birden fazla ifade çalıştırılamaz.";
            return false;
        }

        // Yalnızca SELECT ya da WITH (CTE) ile başlayabilir.
        if (!Regex.IsMatch(trimmed, @"^\s*(SELECT|WITH)\b", RegexOptions.IgnoreCase))
        {
            reason = "Yalnızca SELECT sorguları çalıştırılabilir.";
            return false;
        }

        foreach (var keyword in ForbiddenKeywords)
        {
            // \b ... \b: tam kelime. "CreatedAt" içindeki "Create" eşleşmez.
            if (Regex.IsMatch(trimmed, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
            {
                reason = $"'{keyword}' içeren sorgular çalıştırılamaz.";
                return false;
            }
        }

        // xp_/sp_ ile başlayan sistem yordamları (ör. xp_cmdshell).
        if (Regex.IsMatch(trimmed, @"\b(xp_|sp_)\w+", RegexOptions.IgnoreCase))
        {
            reason = "Sistem yordamları çağrılamaz.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>
    /// Blok (/* */) ve satır (--) yorumlarını boşlukla değiştirir.
    /// Silmek yerine BOŞLUKLA değiştiriyoruz: "SEL/**/ECT" silinseydi "SELECT"e
    /// dönüşür ve denetimi geçerdi; boşlukla "SEL ECT" olur ve reddedilir.
    /// </summary>
    private static string StripComments(string sql)
    {
        var noBlockComments = Regex.Replace(sql, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(noBlockComments, @"--[^\r\n]*", " ");
    }
}
