using HRManagement.WebUI.Models.Api.LeaveRequests;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models.LeaveRequests;

/// <summary>
/// İzin talepleri liste ekranının modeli. API'de "tüm talepler" ucu olmadığı için
/// liste her zaman bir çalışana bağlıdır: ekran hem çalışan seçiciyi hem de
/// seçilen çalışanın taleplerini taşır.
/// </summary>
public class LeaveRequestListViewModel
{
    /// <summary>Seçili çalışan; null ise henüz seçim yapılmamıştır ve liste boş gösterilir.</summary>
    public int? SelectedEmployeeId { get; set; }

    /// <summary>
    /// Giriş yapan kişinin KENDİ çalışan Id'si (varsa). Buton görünürlüğü buna bakar:
    /// kendi talebinde Onayla/Reddet gizlenir, Sil ise yalnızca kendi talebinde çıkar.
    /// Çalışan kaydı olmayan hesapta (ör. Admin/İK) null kalır.
    /// </summary>
    public int? CurrentEmployeeId { get; set; }

    /// <summary>
    /// Çalışan seçici gösterilsin mi? Yalnızca HR/Admin herkesi tarayabilir; diğer
    /// roller kendi izinlerine sabitlenir (seçici gizli).
    /// </summary>
    public bool ShowEmployeePicker { get; set; }

    public IEnumerable<SelectListItem> EmployeeOptions { get; set; } = [];

    public List<LeaveRequestResponse> Requests { get; set; } = [];

    /// <summary>
    /// HR/Admin "İzin Geçmişi" görünümü: TÜM izinler (her durumda) tek listede,
    /// çalışan seçmeden. true ise <see cref="AllRows"/> gösterilir (salt görüntü);
    /// false ise kişiye bağlı <see cref="Requests"/>.
    /// </summary>
    public bool IsAllView { get; set; }

    /// <summary>Tüm izin geçmişi — yalnızca <see cref="IsAllView"/> true iken doludur.</summary>
    public List<LeaveHistoryResponse> AllRows { get; set; } = [];

    /// <summary>
    /// HR'ın "İzinlerim" kapsamı (?mine=true): şirket geneli yerine kişinin KENDİ
    /// talepleri. Filtre formu ve rapor linkleri bu bayrağı taşımak zorunda —
    /// yoksa tarih filtresi uygulayan HR kendini yeniden şirket geneli görünümde bulur.
    /// </summary>
    public bool Mine { get; set; }

    // ── Zaman filtresi ───────────────────────────────────────────────────────
    // Filtre SUNUCUDA uygulanır, listenin JS süzgeci gibi değil: ekran her satırı
    // HTML'e basıyor, dolayısıyla gizlemek yükü azaltmaz. Sunucuda eleyince hem
    // sayfa küçülür hem rapor/CSV çıktısı aynı kapsamı paylaşır.

    /// <summary>Hazır aralık anahtarı: all | month | 3m | year | 12m | custom.</summary>
    public string Range { get; set; } = LeaveRangeOptions.All;

    /// <summary>Özel aralık — yalnızca <see cref="Range"/> = custom iken anlamlı.</summary>
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    /// <summary>Seçimden çözülen gerçek sınırlar; null = sınırsız. Ekranda ve raporda gösterilir.</summary>
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Filtre uygulanmadan ÖNCEKİ kayıt sayısı ("124 kayıttan 30'u").</summary>
    public int TotalBeforeFilter { get; set; }

    public bool HasDateFilter => EffectiveFrom is not null || EffectiveTo is not null;

    /// <summary>Filtrelenmiş kayıt sayısı — hangi görünümde olduğumuzdan bağımsız.</summary>
    public int RowCount => IsAllView ? AllRows.Count : Requests.Count;

    /// <summary>Rapor başlığındaki aralık metni ("01.01.2026 – 29.07.2026").</summary>
    public string RangeText => (EffectiveFrom, EffectiveTo) switch
    {
        (null, null) => "Tüm zamanlar",
        ({ } f, null) => $"{f:dd.MM.yyyy} ve sonrası",
        (null, { } t) => $"{t:dd.MM.yyyy} ve öncesi",
        ({ } f, { } t) => $"{f:dd.MM.yyyy} – {t:dd.MM.yyyy}"
    };
}

/// <summary>
/// Hazır zaman aralıkları. Anahtarlar URL'de görünür (?range=3m), bu yüzden
/// sabit tutulur; etiketler tek yerde durur ki ekran, rapor ve CSV aynı adı kullansın.
/// </summary>
public static class LeaveRangeOptions
{
    public const string All = "all";
    public const string Today = "today";
    public const string ThisWeek = "week";
    public const string ThisMonth = "month";
    public const string LastThreeMonths = "3m";
    public const string ThisYear = "year";
    public const string Custom = "custom";

    // Sıra dardan genişe; "Tümü" başta çünkü varsayılan o.
    public static readonly (string Key, string Label)[] Presets =
    [
        (All, "Tümü"),
        (Today, "Bugün"),
        (ThisWeek, "Bu hafta"),
        (ThisMonth, "Bu ay"),
        (LastThreeMonths, "Son 3 ay"),
        (ThisYear, "Bu yıl")
    ];

    public static string Label(string? key) =>
        Presets.FirstOrDefault(p => p.Key == key).Label ?? "Özel aralık";
}
