using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models;

/// <summary>
/// Kıdem seviyesinin (SeniorityLevel enum'ının sayısal karşılığı) Türkçe etiketi
/// ve "pozisyon" gösterimi. Pozisyon AYRI bir alan değildir; Departman + Kıdem'den
/// türetilir: "IT" + "Uzman" → "IT Uzmanı".
/// </summary>
public static class SeniorityDisplay
{
    // Sıra, dropdown'da da bu düzende görünsün diye kıdem yüksekten düşüğe.
    private static readonly (int Value, string Label)[] Levels =
    [
        (1, "Genel Müdür"),
        (2, "Genel Müdür Yardımcısı"),
        (3, "Müdür"),
        (4, "Müdür Yardımcısı"),
        (5, "Kıdemli Uzman"),
        (6, "Uzman"),
    ];

    public static string Label(int? seniority) =>
        seniority is int s
            ? Levels.FirstOrDefault(l => l.Value == s).Label ?? "—"
            : "—";

    /// <summary>Departman + kıdem → "IT Uzmanı". Kıdem yoksa yalnız departman adı.</summary>
    public static string Position(string? departmentName, int? seniority)
    {
        var dept = departmentName?.Trim() ?? "";
        var label = seniority is int ? Label(seniority) : "";
        return string.Join(" ", new[] { dept, label }.Where(p => !string.IsNullOrEmpty(p)));
    }

    public static IEnumerable<SelectListItem> Options() =>
        Levels.Select(l => new SelectListItem(l.Label, l.Value.ToString()));

    /// <summary>
    /// Yönetici kademesi mi? (GM=1, GMY=2, Müdür=3). Yalnızca bunlar birine
    /// yönetici olabilir — API'deki SeniorityLevel.IsManagerial ile aynı kural.
    /// </summary>
    public static bool IsManagerial(int? seniority) => seniority is 1 or 2 or 3;
}
