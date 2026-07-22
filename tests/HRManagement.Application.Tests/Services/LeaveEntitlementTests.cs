using HRManagement.Application.Services;

namespace HRManagement.Application.Tests.Services;

/// <summary>
/// İzin hakkı hesabının birim testleri. LeaveEntitlement SAF fonksiyonlardan
/// oluştuğu için ne veritabanı ne mock gerekir — girdiler verilir, çıktı doğrulanır.
/// Kaynak kurallar: İş Kanunu md. 53 + avans izin kararı (2026-07-22).
/// </summary>
public class LeaveEntitlementTests
{
    // ── Kıdem hesabı: yıldönümü esaslı, takvim yılı değil ────────────────────

    [Fact]
    public void FullYearsOfService_yildonumunden_bir_gun_once_yil_dolmamis_sayilir()
    {
        var hireDate = new DateTime(2025, 7, 22);
        var asOf = new DateTime(2026, 7, 21); // yıldönümüne 1 gün var

        Assert.Equal(0, LeaveEntitlement.FullYearsOfService(hireDate, asOf));
    }

    [Fact]
    public void FullYearsOfService_yildonumu_gununde_yil_dolmus_sayilir()
    {
        var hireDate = new DateTime(2025, 7, 22);
        var asOf = new DateTime(2026, 7, 22); // tam yıldönümü

        Assert.Equal(1, LeaveEntitlement.FullYearsOfService(hireDate, asOf));
    }

    [Fact]
    public void FullYearsOfService_ileri_tarihli_ise_giris_sifira_kirpilir()
    {
        // Bozuk veri (gelecekte işe giriş) hesabı eksiye düşürmemeli.
        var hireDate = new DateTime(2027, 1, 1);
        var asOf = new DateTime(2026, 7, 22);

        Assert.Equal(0, LeaveEntitlement.FullYearsOfService(hireDate, asOf));
    }

    // ── Kanuni kademeler (md. 53) ────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]    // 1 yıldan az → hak yok
    [InlineData(1, 14)]   // 1-5 yıl
    [InlineData(5, 14)]   // 5 DAHİL 14 (sık yapılan hata: 5'i üst kademeye koymak)
    [InlineData(6, 20)]   // 5'ten fazla 15'ten az
    [InlineData(14, 20)]
    [InlineData(15, 26)]  // 15 DAHİL 26
    [InlineData(30, 26)]
    public void EntitledDaysForYears_kanuni_kademeleri_uygular(int years, int expectedDays)
    {
        Assert.Equal(expectedDays, LeaveEntitlement.EntitledDaysForYears(years));
    }

    // ── Elle geçersiz kılma (Employees.AnnualLeaveDays) ─────────────────────

    [Fact]
    public void EntitledDays_elle_deger_verilmisse_hesabi_ezer()
    {
        var hireDate = new DateTime(2020, 1, 1); // kıdem ~6 yıl → normalde 20
        var asOf = new DateTime(2026, 7, 22);

        Assert.Equal(30, LeaveEntitlement.EntitledDays(hireDate, asOf, manualOverride: 30));
        Assert.Equal(20, LeaveEntitlement.EntitledDays(hireDate, asOf, manualOverride: null));
    }

    // ── Avans sınırı: bir sonraki yıldönümünde kazanılacak hak ──────────────

    [Fact]
    public void AdvanceLimit_alti_aylik_calisan_icin_14_gundur()
    {
        // Kullanıcının sorduğu senaryo: 1 yılı dolmadan izin.
        // Kazanılmış hak 0, ama gelecek yıldönümünde 14 kazanacak → 14 güne
        // kadar borçlanabilir; 15. gün reddedilir.
        var hireDate = new DateTime(2026, 1, 22);
        var asOf = new DateTime(2026, 7, 22);

        Assert.Equal(0, LeaveEntitlement.EntitledDays(hireDate, asOf, null));
        Assert.Equal(14, LeaveEntitlement.AdvanceLimit(hireDate, asOf));
    }

    [Fact]
    public void AdvanceLimit_besinci_yilini_dolduran_icin_20_gundur()
    {
        // 5 yıl kıdem → hak 14; bir sonraki yıl (6.) → 20.
        var hireDate = new DateTime(2021, 7, 22);
        var asOf = new DateTime(2026, 7, 22);

        Assert.Equal(14, LeaveEntitlement.EntitledDays(hireDate, asOf, null));
        Assert.Equal(20, LeaveEntitlement.AdvanceLimit(hireDate, asOf));
    }

    // ── Hak dönemi: [son yıldönümü, sonraki yıldönümü) ──────────────────────

    [Fact]
    public void CurrentPeriod_ise_giris_yildonumune_gore_hesaplanir()
    {
        var hireDate = new DateTime(2024, 3, 15);
        var asOf = new DateTime(2026, 7, 22); // 2 tam yıl dolmuş

        var (start, endExclusive) = LeaveEntitlement.CurrentPeriod(hireDate, asOf);

        Assert.Equal(new DateTime(2026, 3, 15), start);
        Assert.Equal(new DateTime(2027, 3, 15), endExclusive);
    }

    [Fact]
    public void CurrentPeriod_ilk_yilinda_ise_giristen_baslar()
    {
        var hireDate = new DateTime(2026, 2, 1);
        var asOf = new DateTime(2026, 7, 22);

        var (start, endExclusive) = LeaveEntitlement.CurrentPeriod(hireDate, asOf);

        Assert.Equal(hireDate, start);
        Assert.Equal(new DateTime(2027, 2, 1), endExclusive);
    }

    // ── Gün sayımı: başlangıç ve bitiş DAHİL ────────────────────────────────

    [Theory]
    [InlineData("2026-07-22", "2026-07-22", 1)] // tek gün = 1 (off-by-one tuzağı)
    [InlineData("2026-07-20", "2026-07-24", 5)]
    public void TotalDays_iki_ucu_da_dahil_sayar(string start, string end, int expected)
    {
        Assert.Equal(expected,
            LeaveEntitlement.TotalDays(DateTime.Parse(start), DateTime.Parse(end)));
    }
}
