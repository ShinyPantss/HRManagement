using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models;

/// <summary>
/// Cinsiyetin (Gender enum'ının sayısal karşılığı) etiketi ve dropdown seçenekleri.
/// SeniorityDisplay/GradeDisplay ile aynı desen: sayı ↔ etiket eşlemesi tek yerde.
/// </summary>
public static class GenderDisplay
{
    private static readonly (int Value, string Label)[] Genders =
    [
        (1, "Erkek"),
        (2, "Kadın"),
    ];

    public static string Label(int? gender) =>
        gender is int g
            ? Genders.FirstOrDefault(x => x.Value == g).Label ?? "—"
            : "—";

    public static IEnumerable<SelectListItem> Options() =>
        Genders.Select(x => new SelectListItem(x.Label, x.Value.ToString()));
}
