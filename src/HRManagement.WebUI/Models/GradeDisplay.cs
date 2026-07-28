using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models;

/// <summary>
/// Stajyer sınıfının gösterim/seçim yardımcısı. Sayı sözleşmesi: 0 = Hazırlık,
/// 1-4 = lisans sınıfı (Application validator'ıyla aynı aralık).
/// SeniorityDisplay ile aynı desen: sayı ↔ etiket eşlemesi tek yerde durur.
/// </summary>
public static class GradeDisplay
{
    private static readonly (int Value, string Label)[] Grades =
    [
        (0, "Hazırlık"),
        (1, "1. Sınıf"),
        (2, "2. Sınıf"),
        (3, "3. Sınıf"),
        (4, "4. Sınıf"),
    ];

    public static string Label(int grade) =>
        Grades.FirstOrDefault(g => g.Value == grade).Label ?? grade.ToString();

    public static IEnumerable<SelectListItem> Options() =>
        Grades.Select(g => new SelectListItem(g.Label, g.Value.ToString()));
}
