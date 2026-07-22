using HRManagement.Application.Services;

namespace HRManagement.Application.Tests.Services;

/// <summary>
/// İzin hakkı hesabının birim testleri. LeaveEntitlement SAF fonksiyonlardan
/// oluştuğu için ne veritabanı ne mock gerekir — girdiler verilir, çıktı doğrulanır.
/// Model: kümülatif bakiye (İş Kanunu md. 53 kademeleri + avans borcu devreder).
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
        var hireDate = new DateTime(2027, 1, 1);
        var asOf = new DateTime(2026, 7, 22);

        Assert.Equal(0, LeaveEntitlement.FullYearsOfService(hireDate, asOf));
    }

    // ── Kademeler (md. 53): n'inci yılın grant'ı ─────────────────────────────

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 14)]
    [InlineData(5, 14)]   // 5. yıl DAHİL 14 (sık yapılan hata: 5'i üst kademeye koymak)
    [InlineData(6, 20)]
    [InlineData(14, 20)]
    [InlineData(15, 26)]  // 15. yıl DAHİL 26
    [InlineData(30, 26)]
    public void GrantForYear_kanuni_kademeleri_uygular(int year, int expected)
    {
        Assert.Equal(expected, LeaveEntitlement.GrantForYear(year));
    }

    // ── Hak edilen toplam: tamamlanan yılların grant toplamı ─────────────────

    [Fact]
    public void AccruedEntitlement_yil_dolmadan_sifirdir()
    {
        var hireDate = new DateTime(2026, 1, 22);
        var asOf = new DateTime(2026, 7, 22); // ~6 ay

        Assert.Equal(0, LeaveEntitlement.AccruedEntitlement(hireDate, asOf, null));
    }

    [Fact]
    public void AccruedEntitlement_uc_yil_sonunda_uc_kademe_toplanir()
    {
        var hireDate = new DateTime(2023, 7, 22);
        var asOf = new DateTime(2026, 7, 22); // 3 tam yıl

        // 14 + 14 + 14 = 42
        Assert.Equal(42, LeaveEntitlement.AccruedEntitlement(hireDate, asOf, null));
    }

    [Fact]
    public void AccruedEntitlement_kademe_gecisini_dogru_toplar()
    {
        var hireDate = new DateTime(2020, 7, 22);
        var asOf = new DateTime(2026, 7, 22); // 6 tam yıl

        // İlk 5 yıl 14'er (70), 6. yıl 20 → 90
        Assert.Equal(90, LeaveEntitlement.AccruedEntitlement(hireDate, asOf, null));
    }

    [Fact]
    public void AccruedEntitlement_elle_deger_verilmisse_yillik_kademeyi_ezer()
    {
        var hireDate = new DateTime(2023, 7, 22);
        var asOf = new DateTime(2026, 7, 22); // 3 tam yıl

        // Elle 20 → yılda 20'den 3 yıl = 60
        Assert.Equal(60, LeaveEntitlement.AccruedEntitlement(hireDate, asOf, annualOverride: 20));
    }

    // ── Avans sınırı: bir sonraki yılın hakkı ────────────────────────────────

    [Fact]
    public void NextGrant_alti_aylik_calisan_icin_14tur()
    {
        var hireDate = new DateTime(2026, 1, 22);
        var asOf = new DateTime(2026, 7, 22);

        Assert.Equal(14, LeaveEntitlement.NextGrant(hireDate, asOf, null));
    }

    [Fact]
    public void NextGrant_besinci_yilini_dolduran_icin_20dir()
    {
        var hireDate = new DateTime(2021, 7, 22);
        var asOf = new DateTime(2026, 7, 22); // 5 tam yıl

        Assert.Equal(20, LeaveEntitlement.NextGrant(hireDate, asOf, null));
    }

    // ── Kullanıcının senaryosu: avans borcu yeni yıla DEVREDER ───────────────

    [Fact]
    public void avans_borcu_yeni_hak_yilina_devreder()
    {
        // 6 aylık çalışan 10 gün yıllık izin kullanır.
        var hireDate = new DateTime(2026, 1, 22);
        const int used = 10;

        // İlk yıl dolmadan: hak 0, bakiye -10 (avans 14 sınırında geçerli).
        var beforeAsOf = new DateTime(2026, 7, 22);
        var accruedBefore = LeaveEntitlement.AccruedEntitlement(hireDate, beforeAsOf, null);
        var nextGrant = LeaveEntitlement.NextGrant(hireDate, beforeAsOf, null);
        Assert.Equal(0, accruedBefore);
        Assert.Equal(-10, accruedBefore - used);        // bakiye -10
        Assert.True(used <= accruedBefore + nextGrant);  // 10 ≤ 0 + 14 → talep geçer

        // 1 yıl dolunca: +14 eklenir, borç devreder, bakiye 4.
        var afterAsOf = new DateTime(2027, 1, 22);
        var accruedAfter = LeaveEntitlement.AccruedEntitlement(hireDate, afterAsOf, null);
        Assert.Equal(14, accruedAfter);
        Assert.Equal(4, accruedAfter - used);            // bakiye 14 - 10 = 4
    }

    [Fact]
    public void avans_siniri_asilirsa_talep_gecmez()
    {
        // 6 aylık çalışan 15 gün ister: 15 > 0 + 14 → reddedilmeli.
        var hireDate = new DateTime(2026, 1, 22);
        var asOf = new DateTime(2026, 7, 22);
        const int requested = 15;

        var accrued = LeaveEntitlement.AccruedEntitlement(hireDate, asOf, null);
        var nextGrant = LeaveEntitlement.NextGrant(hireDate, asOf, null);

        Assert.False(0 + requested <= accrued + nextGrant); // 15 ≤ 14 değil
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
