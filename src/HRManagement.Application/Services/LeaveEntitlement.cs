namespace HRManagement.Application.Services;

/// <summary>
/// Yıllık izin hakkı hesabı — SAF fonksiyonlar: veritabanına dokunmaz, durum
/// tutmaz. Kıdem kademeleri tek yerde dursun ve birim testi DB'siz yazılabilsin
/// diye handler'lardan ayrılmıştır.
///
/// Kaynak: İş Kanunu md. 53 —
///   1 yıldan az kıdem      →  0 gün (yıllık ücretli izin hakkı henüz doğmamıştır)
///   1–5 yıl (5 dahil)      → 14 gün
///   5'ten fazla, 15'ten az → 20 gün
///   15 yıl ve fazlası      → 26 gün
///
/// Hak dönemi TAKVİM YILI DEĞİL, işe giriş YILDÖNÜMÜDÜR: 1 Temmuz'da işe giren,
/// 1 Ocak'ta değil ertesi 1 Temmuz'da hak kazanır.
///
/// Avans izin (kullanıcı kararı, 2026-07-22): çalışan bir SONRAKİ yıldönümünde
/// kazanacağı hak kadar borçlanabilir; bakiye o sınıra kadar eksiye düşebilir.
/// </summary>
public static class LeaveEntitlement
{
    /// <summary>İşe girişten bu yana dolmuş TAM yıl sayısı (yıldönümü esaslı).</summary>
    public static int FullYearsOfService(DateTime hireDate, DateTime asOf)
    {
        var years = asOf.Year - hireDate.Year;

        // Yıldönümü henüz gelmediyse son yıl dolmamıştır.
        // AddYears, 29 Şubat gibi kenarları .NET kurallarıyla ele alır (28 Şubat'a çeker).
        if (hireDate.Date.AddYears(years) > asOf.Date)
            years--;

        return Math.Max(0, years); // ileri tarihli işe giriş verisi hesabı eksiye düşürmesin
    }

    /// <summary>Kıdeme göre kanuni yıllık izin günü.</summary>
    public static int EntitledDaysForYears(int fullYears) => fullYears switch
    {
        < 1 => 0,
        <= 5 => 14,   // 1–5 yıl (5 dahil)
        < 15 => 20,   // 5'ten fazla 15'ten az
        _ => 26       // 15 ve üzeri
    };

    /// <summary>
    /// Geçerli hakkın gün sayısı. manualOverride (Employees.AnnualLeaveDays)
    /// doluysa hesaplamayı ezer: şirket bu kişiye özel gün tanımlamıştır.
    /// </summary>
    public static int EntitledDays(DateTime hireDate, DateTime asOf, int? manualOverride) =>
        manualOverride ?? EntitledDaysForYears(FullYearsOfService(hireDate, asOf));

    /// <summary>
    /// Avans sınırı: bir sonraki yıldönümünde kazanılacak hak. 6 aylık çalışan
    /// için 14 — yani 14 güne kadar borçlanabilir, 15. günde reddedilir.
    /// </summary>
    public static int AdvanceLimit(DateTime hireDate, DateTime asOf) =>
        EntitledDaysForYears(FullYearsOfService(hireDate, asOf) + 1);

    /// <summary>
    /// İçinde bulunulan hak dönemi: [son yıldönümü, sonraki yıldönümü).
    /// Kıdemi 1 yıldan az olanlar için [işe giriş, işe giriş + 1 yıl).
    /// </summary>
    public static (DateTime Start, DateTime EndExclusive) CurrentPeriod(DateTime hireDate, DateTime asOf)
    {
        var fullYears = FullYearsOfService(hireDate, asOf);
        var start = hireDate.Date.AddYears(fullYears);
        return (start, hireDate.Date.AddYears(fullYears + 1));
    }

    /// <summary>Başlangıç ve bitiş günü DAHİL takvim günü sayısı.</summary>
    public static int TotalDays(DateTime startDate, DateTime endDate) =>
        (endDate.Date - startDate.Date).Days + 1;
}
