using HRManagement.Application.Features.Assistant.Shared;
using HRManagement.Application.Services;

namespace HRManagement.Application.Tests.Verification;

/// <summary>
/// BAĞIMSIZ DOĞRULAMA testleri (yazan: dogrulayici, düzelten değil).
///
/// Bu dosyanın amacı "kod güzel mi" değil: kapatıldığı söylenen açığın GERÇEKTEN
/// kapanıp kapanmadığını, verilen örnekleri değil KURALI test etmek.
///
/// İki tür test var:
///
///   1) YEŞİL testler — kapanışı kilitleyen regresyon testleri. Biri kırmızıya
///      dönerse kapanmış bir açık yeniden açılmış demektir.
///
///   2) KIRMIZI testler — `AtlatmaKanitlari` sınıfındakiler BİLEREK BAŞARISIZDIR.
///      Doğrulama sırasında bulunmuş, henüz kapatılmamış atlatma vektörleridir.
///      Kanıtın raporda değil KODDA durması için buradalar: düzeltme yapıldığında
///      kendiliğinden yeşile döner ve doğrulanmış olur.
///      Bu testler geçmeden "SqlReadOnlyGuard ifade zincirlemesini engelliyor"
///      denemez.
///
/// Ayrıntılar: docs/dogrulama-raporu.md
/// </summary>
public class SqlReadOnlyGuardBypassTests
{
    // ── K1'in ÖZÜ: denetlenen metin ile çalıştırılan metnin ayrışması ────────
    //
    // Guard yorumları ayıklayıp denetliyor, çağıran ise ham metni çalıştırıyordu.
    // T-SQL'in "veri" saydığı bir '--' veya '/*', naif bir ayıklayıcıya "yorum"
    // gibi görünür; arkasındaki ikinci ifade denetimden silinir ama veritabanına
    // gider. Aşağıdaki her satır bu ayrışmayı sömürmeye çalışır.
    //
    // Hepsi REDDEDİLMELİ. Biri kabul edilirse K1 yeniden açılmıştır.
    [Theory]
    [InlineData("SELECT '--' AS x, 1; DROP TABLE Employees")]
    [InlineData("SELECT '/*' AS x; DROP TABLE Employees; SELECT '*/' AS y")]
    [InlineData("SELECT 'it''s --' AS x, 1; DROP TABLE Employees")]
    [InlineData("SELECT N'--' AS x, 1; DROP TABLE Employees")]
    [InlineData("SELECT N'/*' AS x; TRUNCATE TABLE Employees; SELECT N'*/' AS y")]
    [InlineData("SELECT 1 AS [--]; DROP TABLE Employees")]
    [InlineData("SELECT 1 AS [a]]--]; DROP TABLE Employees")]
    [InlineData("SELECT 1 AS \"--\"; DROP TABLE Employees")]
    [InlineData("SELECT '--' AS x; UPDATE Employees SET Email='x'")]
    [InlineData("SELECT '-- ' AS a, '--' AS b; DELETE FROM Employees")]
    [InlineData("SELECT '/*' AS a; DR/**/OP TABLE Employees; SELECT '*/' AS b")]
    [InlineData("SELECT /* /* */ 1; DROP TABLE Employees")]
    [InlineData("SELECT 1 --\r; DROP TABLE Employees")]
    [InlineData("SELECT 1 /*; DROP TABLE Employees")]
    [InlineData("SELECT 'abc; DROP TABLE Employees")]
    [InlineData("\t\n  SELECT '--' AS x, 1; DROP TABLE Employees")]
    [InlineData("﻿SELECT 1; DROP TABLE Employees")]
    public void Metin_sabiti_ile_gizlenmis_ikinci_ifade_reddedilir(string sql)
    {
        var kabul = SqlReadOnlyGuard.TryNormalize(sql, out _, out _);

        Assert.False(kabul, $"Guard bu sorguyu kabul etti, oysa ikinci bir ifade taşıyor: {sql}");
    }

    // Guard'ın SÖZLEŞMESİ: kabul ettiği metnin ta kendisi çalıştırılabilmeli.
    // safeSql ile girdi ayrışırsa denetim yeniden atlatılabilir hâle gelir.
    [Theory]
    [InlineData("SELECT TOP 200 * FROM Employees WHERE LastName LIKE '%-%'")]
    [InlineData("SELECT TOP 200 * FROM Employees WHERE FirstName = '/* yorum degil */'")]
    [InlineData("SELECT TOP 200 * FROM Employees WHERE LastName = 'O''Brien'")]
    public void Kabul_edilen_sorguda_metin_sabitinin_icerigi_korunur(string sql)
    {
        var kabul = SqlReadOnlyGuard.TryNormalize(sql, out var safeSql, out var reason);

        Assert.True(kabul, $"Meşru sorgu reddedildi: {reason}");

        // Çalışacak metin, girdinin (sondaki ";" kırpması dışında) aynısı olmalı.
        Assert.Equal(sql.Trim().TrimEnd(';').Trim(), safeSql);
    }

    // ── Yanlış pozitif kilidi ────────────────────────────────────────────────
    //
    // Guard'ı aşırı sıkılaştırmak da bir kusurdur: asistan kullanılamaz hâle
    // gelirse özellik ölür. Bu meşru sorgular her zaman GEÇMELİDİR.
    [Theory]
    [InlineData("SELECT TOP 200 CreatedAt FROM Employees")]                 // "CREATE" alt dizgesi
    [InlineData("SELECT TOP 200 UpdatedAt FROM Employees")]                 // "UPDATE" alt dizgesi
    [InlineData("WITH x AS (SELECT TOP 200 Id FROM Employees) SELECT * FROM x")]
    [InlineData("SELECT TOP 200 Id FROM Employees;")]                       // sondaki tek ";"
    [InlineData("SELECT TOP 200 Id FROM Employees ;  ")]
    [InlineData("/* rapor */ SELECT TOP 200 Id FROM Employees")]            // gerçek blok yorum
    [InlineData("-- rapor\nSELECT TOP 200 Id FROM Employees")]              // gerçek satır yorumu
    [InlineData("SELECT TOP 200 [First Name] FROM Employees")]              // köşeli tanımlayıcı
    [InlineData("SELECT TOP 200 * FROM Employees WHERE FirstName = N'Mücahit'")]
    [InlineData("SELECT TOP 200 Grade - 1 FROM Interns")]                   // tek tire, yorum değil
    [InlineData("SELECT TOP 100 Id FROM Employees UNION ALL SELECT TOP 100 Id FROM Interns")]
    [InlineData("SELECT Id FROM Employees ORDER BY Id OFFSET 0 ROWS FETCH NEXT 200 ROWS ONLY")]
    [InlineData("SELECT TOP 200 CASE WHEN IsActive=1 THEN 'Aktif' ELSE 'Pasif' END FROM Employees")]
    [InlineData("SELECT TOP 200 * FROM Employees WHERE DepartmentId IN (SELECT Id FROM Departments)")]
    public void Mesru_salt_okuma_sorgulari_kabul_edilir(string sql)
    {
        var kabul = SqlReadOnlyGuard.TryNormalize(sql, out _, out var reason);

        Assert.True(kabul, $"Meşru sorgu YANLIŞ POZİTİF olarak reddedildi ({reason}): {sql}");
    }
}

/// <summary>
/// İş kuralı düzeltmelerinin kilidi. Yorumun koda uydurulması yerine KODUN
/// yoruma uydurulması klasik bir regresyondur — H-2'de düzeltilen yalnızca
/// yorumdu, kademe değerleri değişmemeliydi.
/// </summary>
public class LeaveEntitlementRegressionTests
{
    // İş Kanunu md. 53 kademeleri. Sınır yılları (5/6 ve 14/15) özellikle test
    // ediliyor: H-2'deki yorum 15. yıl için "20" diyordu, kod 26 veriyor —
    // doğru olan KOD. Yorum düzeltilirken kod bozulmuş olmamalı.
    [Theory]
    [InlineData(1, 14)]
    [InlineData(5, 14)]   // 5. yıl hâlâ 14
    [InlineData(6, 20)]   // 6. yılda 20'ye çıkar
    [InlineData(14, 20)]  // 14. yıl hâlâ 20
    [InlineData(15, 26)]  // 15. yıldan itibaren 26  ← yorumun yanlış olduğu sınır
    [InlineData(30, 26)]
    public void GrantForYear_kademeleri_degismedi(int yil, int beklenenGun)
    {
        Assert.Equal(beklenenGun, LeaveEntitlement.GrantForYear(yil));
    }
}

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
/// BİLEREK BAŞARISIZ — henüz kapatılmamış atlatma vektörlerinin kanıtı.
/// ══════════════════════════════════════════════════════════════════════════
///
/// Bulgu: SqlReadOnlyGuard'ın ifade zincirlemesine karşı TEK savunması
/// ";" aramasıdır (SqlReadOnlyGuard.cs:77, "Birden fazla ifade çalıştırılamaz").
/// Oysa T-SQL ifadeler arasında noktalı virgül ZORUNLU TUTMAZ; ardışık iki
/// ifade yalnızca boşlukla ayrılabilir. Dolayısıyla ";" içermeyen çok ifadeli
/// bir metin denetimden geçer.
///
/// Zarar, ikinci ifadenin anahtar kelimesinin ForbiddenKeywords listesinde
/// olmasına bağlıdır. Liste DDL/DML'i kapsıyor (DROP, DELETE, ... engelleniyor)
/// ama DBCC, KILL, CHECKPOINT gibi ifade başlatıcıları listede YOK.
///
/// ÖNEMLİ ÖLÇÜLÜLÜK NOTU (abartılmasın):
///   • Doğruladığım şey, guard'ın bu metinleri KABUL ETTİĞİDİR — bu kesin.
///   • "SQL Server bunları iki ayrı ifade olarak çalıştırır" T-SQL'in bilinen
///     bir özelliğidir; CANLI BİR SUNUCUDA ÇALIŞTIRARAK DOĞRULAMADIM.
///   • İkinci savunma katmanı (db_datareader salt okuma kullanıcısı,
///     DbConnectionFactory.CreateReadOnlyConnection) doğru yapılandırılmışsa
///     DBCC/KILL zaten yetki hatası alır — bunlar ALTER SERVER STATE ister.
///   Yani bu, doğrulanmış bir ayrıcalık yükseltmesi DEĞİL, guard'ın kendi
///   sözleşmesini tutmadığının kanıtı ve derinlemesine savunmada bir gediktir.
///
/// Kapatma yolu (öneri): ";" aramasının yanına "denetlenen metinde ilk ifadeden
/// sonra başka bir ifade başlatıcı anahtar kelime gelmesin" kuralı; ya da
/// ForbiddenKeywords listesine DBCC/KILL/CHECKPOINT/USE/GO eklenmesi.
/// </summary>
public class AtlatmaKanitlari
{
    // Premis kanıtı: ";" olmadan iki ifade. Tek başına zararsız, ama guard'ın
    // "Birden fazla ifade çalıştırılamaz" sözleşmesinin tutmadığını gösterir.
    [Fact]
    public void ATLATMA_noktali_virgulsuz_ikinci_ifade_engellenmiyor()
    {
        var kabul = SqlReadOnlyGuard.TryNormalize("SELECT 1 SELECT 2", out var safeSql, out _);

        Assert.False(kabul,
            "Guard ';' içermeyen çok ifadeli metni kabul etti — 'Birden fazla ifade " +
            $"çalıştırılamaz' kuralı yalnızca ';' varken işliyor. Çalışacak metin: {safeSql}");
    }

    // Etki kanıtı: ForbiddenKeywords listesinde olmayan ifade başlatıcılar.
    [Theory]
    [InlineData("SELECT 1 DBCC FREEPROCCACHE")]
    [InlineData("SELECT 1 AS a DBCC CHECKDB")]
    [InlineData("SELECT 1 KILL 55")]
    public void ATLATMA_yasak_listesinde_olmayan_ifade_basiticilari_geciyor(string sql)
    {
        var kabul = SqlReadOnlyGuard.TryNormalize(sql, out var safeSql, out _);

        Assert.False(kabul,
            "Guard, yasak kelime listesinde bulunmayan bir ifade başlatıcısıyla " +
            $"zincirlenmiş metni kabul etti. Çalışacak metin: {safeSql}");
    }
}
