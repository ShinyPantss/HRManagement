using System.Text;
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
    ///
    /// İkinci grup (DBCC ve sonrası) bir AÇIĞA yanıttır: T-SQL ifadeler arasında
    /// ";" ZORUNLU TUTMAZ, "SELECT 1 SELECT 2" iki ifadedir. Aşağıdaki
    /// "birden fazla ifade" denetimi ise yalnızca ";" arar — yani noktalı
    /// virgülsüz zincirleme oradan geçer. Bu liste, geçtiğinde zarar verebilecek
    /// ifade başlatıcılarını kapatır.
    ///
    /// DÜRÜST OLMAK GEREKİRSE bu bir kara listedir ve kara listeler eksiktir:
    /// burada olmayan bir ifade başlatıcısı yine zincirlenebilir. Yapısal çözüm,
    /// metnin gerçekten TEK ifade olduğunu doğrulamaktır; o da tam bir T-SQL
    /// ayrıştırıcısı ister. Asıl garanti bu listede değil, sorguyu salt okuma
    /// yetkisi olan veritabanı kullanıcısıyla çalıştırmakta (bkz. sınıf başlığı).
    /// </summary>
    private static readonly string[] ForbiddenKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE",
        "MERGE", "EXEC", "EXECUTE", "GRANT", "REVOKE", "DENY", "BACKUP",
        "RESTORE", "SHUTDOWN", "RECONFIGURE", "OPENROWSET", "OPENQUERY",
        "OPENDATASOURCE", "BULK", "WAITFOR", "INTO",

        // ";" olmadan zincirlenebilen ifade başlatıcıları.
        "DBCC", "KILL", "CHECKPOINT", "USE", "GO",
        "DECLARE", "SET", "PRINT", "RAISERROR", "THROW", "GOTO", "WHILE", "BEGIN"
    ];

    /// <summary>
    /// Sorguyu denetler; geçerse ÇALIŞTIRILACAK metni <paramref name="safeSql"/> ile
    /// geri verir. Çağıran ham metni DEĞİL bu metni çalıştırmalıdır.
    ///
    /// Neden: denetlenen metinle çalıştırılan metin farklı olursa denetim
    /// atlatılabilir. Örnek: "SELECT '--' AS x; DROP TABLE EmployeeNotes" —
    /// naif bir yorum ayıklayıcı '--' işaretini yorum sanıp gerisini siler,
    /// geriye tek ve masum bir SELECT kalır; veritabanına giden ham metin ise
    /// iki ifadelik bir zincirdir.
    ///
    /// Değilse <paramref name="reason"/> kullanıcıya/modele gösterilebilecek bir
    /// gerekçe taşır.
    /// </summary>
    public static bool TryNormalize(string? sql, out string safeSql, out string reason)
    {
        safeSql = string.Empty;

        if (string.IsNullOrWhiteSpace(sql))
        {
            reason = "Sorgu boş.";
            return false;
        }

        // Tek geçişte iki metin üretilir:
        //   executable → yorumları ayıklanmış, metin sabitleri KORUNMUŞ (çalışacak olan)
        //   inspected  → aynısı, ama metin sabiti/tanımlayıcı içerikleri boşaltılmış
        // İkincisi denetlenir: bir string'in İÇİNDEKİ ";" ya da "DROP" veri olduğu
        // için denetimi yanıltmamalı. İkisi yalnızca veri kısmında ayrışır, yapıda
        // birebir aynıdır — bu yüzden birini denetleyip diğerini çalıştırmak güvenli.
        if (!TryScan(sql, out var executable, out var inspected, out var scanError))
        {
            reason = scanError;
            return false;
        }

        if (string.IsNullOrWhiteSpace(inspected))
        {
            reason = "Sorgu yalnızca yorum içeriyor.";
            return false;
        }

        // Noktalı virgül ifade zincirlemesine izin verir:
        // "SELECT 1; DROP TABLE Employees". Sondaki tek bir tanesi zararsız,
        // ortadaki değil — bu yüzden sondakini atıp kalanda hiç aramıyoruz.
        var trimmed = inspected.Trim().TrimEnd(';').Trim();

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

        // Çalıştırılacak metne de aynı kırpma uygulanır: denetlenen ile çalışan
        // metnin yapısı birebir aynı kalsın diye. Sondaki ";" güvenle atılabilir —
        // metin sabitinin içindeki ";" olamaz, çünkü sabit ancak "'" ile biter.
        safeSql = executable.Trim().TrimEnd(';').Trim();
        reason = string.Empty;
        return true;
    }

    /// <summary>
    /// Tırnak farkındalıklı tek geçişli tarayıcı. Regex yerine elle tarama, çünkü
    /// regex "'--'" ifadesindeki tireleri yorum sanar — SQL Server ise veri sayar.
    ///
    /// Yorumlar SİLİNMEZ, boşlukla değiştirilir: "SEL/**/ECT" silinseydi "SELECT"e
    /// dönüşür ve denetimi geçerdi; boşlukla "SEL ECT" olur ve reddedilir.
    ///
    /// Kapanmamış tırnak/köşeli parantez/blok yorumu doğrudan REDDEDİLİR: metnin
    /// nerede bittiği belirsizse denetim de belirsizdir.
    /// </summary>
    private static bool TryScan(string sql, out string executable, out string inspected, out string error)
    {
        var exec = new StringBuilder(sql.Length);
        var check = new StringBuilder(sql.Length);

        executable = string.Empty;
        inspected = string.Empty;
        error = string.Empty;

        var i = 0;
        while (i < sql.Length)
        {
            var c = sql[i];

            // Satır yorumu: "--" ile satır sonuna kadar.
            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                while (i < sql.Length && sql[i] != '\n' && sql[i] != '\r')
                    i++;

                exec.Append(' ');
                check.Append(' ');
                continue;
            }

            // Blok yorumu: T-SQL'de İÇ İÇE geçebilir, bu yüzden derinlik sayılır.
            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                var depth = 0;
                while (i < sql.Length)
                {
                    if (sql[i] == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
                    {
                        depth++;
                        i += 2;
                    }
                    else if (sql[i] == '*' && i + 1 < sql.Length && sql[i + 1] == '/')
                    {
                        depth--;
                        i += 2;
                        if (depth == 0) break;
                    }
                    else
                    {
                        i++;
                    }
                }

                if (depth != 0)
                {
                    error = "Kapanmamış blok yorumu var.";
                    return false;
                }

                exec.Append(' ');
                check.Append(' ');
                continue;
            }

            // Metin sabiti: '...' — içinde "''" kaçış dizisidir, sabiti BİTİRMEZ.
            if (c == '\'')
            {
                if (!TryReadQuoted(sql, ref i, '\'', exec, out error))
                    return false;

                check.Append("''");   // içerik veri; denetime boş girer
                continue;
            }

            // "Çift tırnaklı" ve [köşeli] tanımlayıcılar: içleri ad'dır, kod değil —
            // içindeki "--" yorum, ";" ifade ayıracı sayılmamalı.
            if (c == '"')
            {
                if (!TryReadQuoted(sql, ref i, '"', exec, out error))
                    return false;

                check.Append("\"\"");
                continue;
            }

            if (c == '[')
            {
                if (!TryReadBracketed(sql, ref i, exec, out error))
                    return false;

                check.Append("[]");
                continue;
            }

            exec.Append(c);
            check.Append(c);
            i++;
        }

        executable = exec.ToString();
        inspected = check.ToString();
        return true;
    }

    /// <summary>
    /// Açılış tırnağından kapanışına kadar okur; "''" / "\"\"" kaçışlarını atlar.
    /// Okunan her karakter <paramref name="exec"/>'e olduğu gibi yazılır.
    /// </summary>
    private static bool TryReadQuoted(
        string sql, ref int i, char quote, StringBuilder exec, out string error)
    {
        exec.Append(quote);
        i++;

        while (i < sql.Length)
        {
            if (sql[i] == quote)
            {
                // Çift tırnak = kaçış: sabit devam ediyor.
                if (i + 1 < sql.Length && sql[i + 1] == quote)
                {
                    exec.Append(quote).Append(quote);
                    i += 2;
                    continue;
                }

                exec.Append(quote);
                i++;
                error = string.Empty;
                return true;
            }

            exec.Append(sql[i]);
            i++;
        }

        error = quote == '\''
            ? "Kapanmamış metin sabiti (tırnak) var."
            : "Kapanmamış tanımlayıcı (çift tırnak) var.";
        return false;
    }

    /// <summary>[Köşeli] tanımlayıcı; "]]" kaçıştır.</summary>
    private static bool TryReadBracketed(string sql, ref int i, StringBuilder exec, out string error)
    {
        exec.Append('[');
        i++;

        while (i < sql.Length)
        {
            if (sql[i] == ']')
            {
                if (i + 1 < sql.Length && sql[i + 1] == ']')
                {
                    exec.Append("]]");
                    i += 2;
                    continue;
                }

                exec.Append(']');
                i++;
                error = string.Empty;
                return true;
            }

            exec.Append(sql[i]);
            i++;
        }

        error = "Kapanmamış tanımlayıcı (köşeli parantez) var.";
        return false;
    }
}
