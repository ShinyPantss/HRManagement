using HRManagement.Application.Features.Assistant.Shared;

namespace HRManagement.Application.Tests.Features.Assistant;

/// <summary>
/// Asistanın ürettiği SQL için metin denetimi (birinci savunma katmanı).
///
/// Buradaki asıl sınav, "denetlenen metin ile çalıştırılan metin aynı mı"
/// sorusudur: guard yorumları ayıklarken bir metin sabitinin İÇİNDEKİ "--" ya
/// da "/*" işaretini yorum sanarsa, denetimden geçen sorgu ile veritabanına
/// giden sorgu ayrışır ve zincirlenmiş ifadeler (";" ile) gizlenebilir.
/// </summary>
public class SqlReadOnlyGuardTests
{
    // ── Reddedilmesi gerekenler ──────────────────────────────────────────────

    [Theory]
    // Tırnak içindeki "--" yorum DEĞİLDİR: SQL Server bunu veri sayar ve
    // ardındaki DROP'u ikinci ifade olarak çalıştırır.
    [InlineData("SELECT '--' AS x; DROP TABLE EmployeeNotes")]
    [InlineData("SELECT '/*' AS x; DROP TABLE EmployeeNotes; SELECT '*/'")]
    [InlineData("SELECT '--' AS x; SELECT Username, PasswordHash FROM Users")]
    // Düz ifade zincirleme.
    [InlineData("SELECT 1; DROP TABLE Employees")]
    // Blok yorumuyla anahtar kelime bölme: yorum boşluğa döner, "SEL ECT" kalır.
    [InlineData("SEL/**/ECT 1")]
    public void Zincirlenmis_veya_gizlenmis_ifadeler_reddedilir(string sql)
    {
        var ok = SqlReadOnlyGuard.TryNormalize(sql, out var safeSql, out var reason);

        Assert.False(ok);
        Assert.Equal(string.Empty, safeSql);
        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    [Theory]
    [InlineData("DELETE FROM Employees")]
    [InlineData("UPDATE Employees SET FirstName = 'x'")]
    [InlineData("EXEC xp_cmdshell 'dir'")]
    [InlineData("SELECT * INTO Yedek FROM Employees")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-- yalnızca yorum")]
    public void Yazma_ve_sistem_islemleri_reddedilir(string sql)
    {
        Assert.False(SqlReadOnlyGuard.TryNormalize(sql, out _, out _));
    }

    [Theory]
    // Kapanmamış tırnak: metnin nerede bittiği belirsizse denetim de belirsizdir.
    [InlineData("SELECT 'acik FROM Employees")]
    [InlineData("SELECT * FROM Employees /* kapanmamis")]
    public void Kapanmamis_tirnak_veya_yorum_reddedilir(string sql)
    {
        Assert.False(SqlReadOnlyGuard.TryNormalize(sql, out _, out _));
    }

    // ── Kabul edilmesi gerekenler (yanlış pozitif üretmemeli) ────────────────

    [Theory]
    [InlineData("SELECT TOP 200 * FROM Employees")]
    // "CreatedAt" içinde "CREATE" geçer ama kolon adıdır.
    [InlineData("SELECT TOP 10 CreatedAt FROM Users")]
    [InlineData("WITH t AS (SELECT 1 AS a) SELECT * FROM t")]
    // Metin sabitindeki tire yorum sanılmamalı.
    [InlineData("SELECT TOP 5 Name FROM Departments WHERE Name = 'Ar-Ge'")]
    public void Zararsiz_select_sorgulari_kabul_edilir(string sql)
    {
        var ok = SqlReadOnlyGuard.TryNormalize(sql, out var safeSql, out var reason);

        Assert.True(ok, reason);
        Assert.Equal(sql, safeSql);
    }

    [Fact]
    public void Sondaki_tek_noktali_virgul_zararsizdir_ve_kirpilir()
    {
        var ok = SqlReadOnlyGuard.TryNormalize(
            "SELECT TOP 5 * FROM Employees;", out var safeSql, out var reason);

        Assert.True(ok, reason);
        Assert.Equal("SELECT TOP 5 * FROM Employees", safeSql);
    }

    // ── Çalıştırılacak metin = denetlenen metin ──────────────────────────────

    [Fact]
    public void Calistirilacak_metinde_yorumlar_ayiklanir_metin_sabiti_korunur()
    {
        var ok = SqlReadOnlyGuard.TryNormalize(
            "SELECT Name FROM Departments WHERE Name = 'Ar-Ge' -- notum",
            out var safeSql, out var reason);

        Assert.True(ok, reason);
        // Yorum gitti, veri aynen duruyor: aksi hâlde sorgu anlamını değiştirirdi.
        Assert.Equal("SELECT Name FROM Departments WHERE Name = 'Ar-Ge'", safeSql);
    }

    [Fact]
    public void Metin_sabitindeki_kacis_tirnagi_sabiti_bitirmez()
    {
        // 'O''Brien' tek bir metin sabitidir; ortadaki '' kaçıştır. Tarayıcı bunu
        // yanlış okusaydı sabitin bittiğini sanıp gerisini kod sayardı.
        var ok = SqlReadOnlyGuard.TryNormalize(
            "SELECT TOP 5 * FROM Employees WHERE LastName = 'O''Brien'",
            out var safeSql, out var reason);

        Assert.True(ok, reason);
        Assert.Equal("SELECT TOP 5 * FROM Employees WHERE LastName = 'O''Brien'", safeSql);
    }

    [Fact]
    public void Metin_sabitindeki_yasak_kelime_yanlis_pozitif_uretmez()
    {
        // Veri olarak "DROP" geçmesi sorguyu tehlikeli yapmaz.
        var ok = SqlReadOnlyGuard.TryNormalize(
            "SELECT TOP 5 * FROM Employees WHERE FirstName = 'DROP'",
            out _, out var reason);

        Assert.True(ok, reason);
    }

    [Fact]
    public void Koseli_tanimlayici_icindeki_noktali_virgul_ifade_ayiraci_sayilmaz()
    {
        var ok = SqlReadOnlyGuard.TryNormalize(
            "SELECT [a;b] FROM Employees", out var safeSql, out var reason);

        Assert.True(ok, reason);
        Assert.Equal("SELECT [a;b] FROM Employees", safeSql);
    }
}
