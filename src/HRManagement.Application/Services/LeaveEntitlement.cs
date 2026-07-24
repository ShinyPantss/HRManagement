namespace HRManagement.Application.Services;

/// <summary>
/// Yıllık izin hakkı hesabı — SAF fonksiyonlar: veritabanına dokunmaz, durum
/// tutmaz. Handler'lardan ayrıdır ki kıdem kademeleri tek yerde dursun ve birim
/// testi DB'siz yazılabilsin.
///
/// KÜMÜLATİF BAKİYE MODELİ:
///   Hak edilen (accrued) = tamamlanan her yıl için o yılın kademe günü toplamı
///   Kullanılan (used)    = şimdiye kadarki tüm yıllık izin günleri (handler'da hesaplanır)
///   Bakiye               = accrued − used            → avans izinde eksiye düşebilir
///   Talep kabul şartı    = used + talep ≤ accrued + (bir sonraki yılın hakkı)
///
/// Örnek (1 yılı dolmamış çalışan 10 gün kullanır):
///   accrued=0, used=10 → bakiye −10 (avans, sonraki yıl 14 kazanacağı için sınır içinde)
///   1 yıl dolunca accrued=14 → bakiye 14−10 = 4     ← borç yeni yıla devreder
///
/// Kademeler (İş Kanunu md. 53): 1–5. yıl 14 gün, 6–15. yıl 20 gün, 15+ yıl 26 gün.
/// Hak dönemi TAKVİM YILI DEĞİL, işe giriş YILDÖNÜMÜ esaslıdır.
/// </summary>
public static class LeaveEntitlement
{
    /// <summary>İşe girişten bu yana dolmuş TAM yıl sayısı (yıldönümü esaslı).</summary>
    public static int FullYearsOfService(DateTime hireDate, DateTime asOf)
    {
        var years = asOf.Year - hireDate.Year;

        // Yıldönümü henüz gelmediyse son yıl dolmamıştır. AddYears, 29 Şubat gibi
        // kenarları .NET kurallarıyla ele alır (28 Şubat'a çeker).
        if (hireDate.Date.AddYears(years) > asOf.Date)
            years--;

        return Math.Max(0, years); // ileri tarihli işe giriş verisi eksiye düşürmesin
    }

    /// <summary>
    /// n'inci yılın tamamlanmasıyla kazanılan gün. n 1-tabanlıdır: 1. yıl → 14.
    /// </summary>
    public static int GrantForYear(int yearNumber) => yearNumber switch
    {
        <= 0 => 0,
        <= 5 => 14,   // 1–5. yıl
        < 15 => 20,   // 6–14. yıl
        _ => 26       // 15. yıl ve sonrası
    };

    /// <summary>
    /// Şimdiye kadar HAK EDİLEN toplam gün: tamamlanan her yılın grant'ının toplamı.
    /// annualOverride (Employees.AnnualLeaveDays) doluysa yıllık kademe yerine o
    /// değer geçer — şirket bu kişiye yılda kaç gün tanıdığını elle belirlemiştir.
    /// </summary>
    public static int AccruedEntitlement(DateTime hireDate, DateTime asOf, int? annualOverride)
    {
        var fullYears = FullYearsOfService(hireDate, asOf);

        if (annualOverride is int perYear)
            return perYear * fullYears;

        var total = 0;
        for (var n = 1; n <= fullYears; n++)
            total += GrantForYear(n);
        return total;
    }

    /// <summary>
    /// Bir sonraki yıldönümünde kazanılacak gün = avans borçlanma sınırı.
    /// Bakiye bu kadar eksiye düşebilir (henüz kazanılmamış hakkın peşin kullanımı).
    /// </summary>
    public static int NextGrant(DateTime hireDate, DateTime asOf, int? annualOverride)
    {
        if (annualOverride is int perYear)
            return perYear;

        return GrantForYear(FullYearsOfService(hireDate, asOf) + 1);
    }

    /// <summary>
    /// Başlangıç ve bitiş DAHİL İŞ GÜNÜ sayısı (Cumartesi/Pazar hariç).
    /// Resmi tatiller şimdilik sayılır (tatil tablosu eklendiğinde buraya girer).
    /// İzin "gün"ü iş günüdür: Pzt→bir sonraki Pzt = 6 iş günü (2 hafta sonu düşer).
    /// </summary>
    public static int WorkingDays(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date;
        if (end < start) return 0;

        var days = 0;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                days++;
        }
        return days;
    }
}
