namespace HRManagement.WebUI.Models;

/// <summary>
/// İzin türü + durumunun gösterim eşlemesi — liste ve detay ekranları ORTAK kullanır
/// (tek doğruluk kaynağı). API enum ADINI metin olarak döndürür ("Annual", "PendingHr");
/// Türkçeleştirme ve rozet sınıfı burada durur.
///
/// Sınıf adları site.css'in .badge-* ailesinden gelir (Bootstrap değil).
/// </summary>
public static class LeaveDisplay
{
    public static string TypeText(string type) => type switch
    {
        "Annual" => "Yıllık İzin",
        "Unpaid" => "Ücretsiz İzin",
        "Sick" => "Hastalık İzni",
        _ => type
    };

    /// <summary>İzin türünün rozet rengi — yıllık mor, hastalık kırmızı, ücretsiz nötr.</summary>
    public static string TypeBadge(string type) => type switch
    {
        "Annual" => "badge-brand",
        "Unpaid" => "badge-neutral",
        "Sick" => "badge-danger",
        _ => "badge-neutral"
    };

    /// <summary>
    /// "Beklemede" yerine NEYİN beklendiğini yazar: yönetici onayı mı, İK onayı mı.
    /// İki aşamalı akış: Pending (yönetici) → PendingHr (İK) → Approved.
    /// </summary>
    public static (string Badge, string Text) StatusBadge(string status) => status switch
    {
        "Pending" => ("badge-warn", "Yönetici Onayı Bekliyor"),
        "PendingHr" => ("badge-info", "İK Onayı Bekliyor"),
        "Approved" => ("badge-ok", "Onaylandı"),
        "Rejected" => ("badge-danger", "Reddedildi"),
        _ => ("badge-neutral", status)
    };

    /// <summary>Dar sütunlarda kullanılan kısa durum metni.</summary>
    public static (string Badge, string Text) StatusBadgeShort(string status) => status switch
    {
        "Pending" => ("badge-warn", "Yönetici onayı"),
        "PendingHr" => ("badge-info", "İK onayı"),
        "Approved" => ("badge-ok", "Onaylandı"),
        "Rejected" => ("badge-danger", "Reddedildi"),
        _ => ("badge-neutral", status)
    };

    public static string StatusText(string status) => StatusBadge(status).Text;
}
