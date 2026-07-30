using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Tests.Features.Employees;

/// <summary>
/// Yönetici atama kuralının birim testleri. ManagerAssignment saf statik bir
/// fonksiyon olduğu için DB/mock gerekmez. Üç koşul: yönetici kademesi + kıdem +
/// (GM hariç) aynı departman. Departman boyutu bu testlerin odağı.
/// Departmanları sabit sayılarla temsil ediyoruz: 10=Oto, 20=Mal, 99=Yönetim.
/// </summary>
public class ManagerAssignmentTests
{
    private const int Oto = 10;
    private const int Mal = 20;
    private const int Yonetim = 99;

    [Fact]
    public void ayni_departmandaki_ust_kidem_gecerli()
    {
        // GMY (Oto) → Müdür (Oto): kademe uygun, kıdem yüksek, aynı departman.
        var ex = Record.Exception(() => ManagerAssignment.EnsureManagerEligible(
            SeniorityLevel.GenelMudurYardimcisi, Oto, SeniorityLevel.Mudur, Oto));

        Assert.Null(ex);
    }

    [Fact]
    public void farkli_departmandaki_gmy_reddedilir()
    {
        // Kullanıcının şikayeti: Mal'ın GMY'si, Oto'nun müdürüne yönetici olamaz.
        // Kıdem tutsa da departman tutmuyor → red.
        Assert.Throws<ValidationException>(() => ManagerAssignment.EnsureManagerEligible(
            SeniorityLevel.GenelMudurYardimcisi, Mal, SeniorityLevel.Mudur, Oto));
    }

    [Fact]
    public void genel_mudur_farkli_departmandan_da_yonetici_olabilir()
    {
        // GM "departman üstü": Yönetim'de otursa bile başka departmanın GMY'sine
        // yönetici olabilir (istisna yalnızca GM'e tanınır).
        var ex = Record.Exception(() => ManagerAssignment.EnsureManagerEligible(
            SeniorityLevel.GenelMudur, Yonetim, SeniorityLevel.GenelMudurYardimcisi, Oto));

        Assert.Null(ex);
    }

    [Fact]
    public void yonetici_kademesinde_olmayan_reddedilir()
    {
        // Uzman kıdemce yüksek olsa bile yönetici olamaz; departman aynı olsa da.
        Assert.Throws<ValidationException>(() => ManagerAssignment.EnsureManagerEligible(
            SeniorityLevel.Uzman, Oto, SeniorityLevel.Uzman, Oto));
    }

    [Fact]
    public void esit_kidem_ayni_departmanda_da_reddedilir()
    {
        // Müdür, Müdür'e yönetici olamaz (kıdem eşit) — departman aynı olsa bile.
        Assert.Throws<ValidationException>(() => ManagerAssignment.EnsureManagerEligible(
            SeniorityLevel.Mudur, Oto, SeniorityLevel.Mudur, Oto));
    }

    // ── Fırlatmayan hâl: yöneticinin kendisi düzenlenirken astları toplu denetlenir ──

    [Fact]
    public void uygun_baglantida_gerekce_null_doner()
    {
        Assert.Null(ManagerAssignment.GetIneligibilityReason(
            SeniorityLevel.GenelMudur, Yonetim, SeniorityLevel.GenelMudurYardimcisi, Mal));
    }

    [Fact]
    public void gm_dusurulunce_bagli_gmy_gecersiz_kalir()
    {
        // Yaşanan hata: GM'lik başkasına verilip eskisi Müdür'e düşürüldü.
        // Ona bağlı GMY'ler "Müdür'e raporlayan GMY" hâlinde kaldı — hem kıdem
        // hem departman kuralı bozuluyor.
        var reason = ManagerAssignment.GetIneligibilityReason(
            SeniorityLevel.Mudur, Oto,                       // eski GM'in YENİ hâli
            SeniorityLevel.GenelMudurYardimcisi, Mal);       // ona bağlı GMY

        Assert.NotNull(reason);
    }

    [Fact]
    public void gm_departman_disina_cikabilir_ast_gecerli_kalir()
    {
        // GM departman üstüdür: departmanı değişse bile astları geçerli kalır.
        Assert.Null(ManagerAssignment.GetIneligibilityReason(
            SeniorityLevel.GenelMudur, Oto, SeniorityLevel.Mudur, Mal));
    }

    [Fact]
    public void mudur_departman_degistirince_eski_astlari_gecersiz_kalir()
    {
        // Müdür başka departmana taşınırsa eski departmanındaki astları
        // "farklı departmandan yönetici" durumuna düşer.
        Assert.NotNull(ManagerAssignment.GetIneligibilityReason(
            SeniorityLevel.Mudur, Mal, SeniorityLevel.Uzman, Oto));
    }
}
