using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models;

/// <summary>
/// Rol sayısının gösterim/seçim yardımcısı. Sayı sözleşmesi Domain'deki Role
/// enum'ıyla aynıdır: 1=Admin 2=HR 3=Manager 4=Employee 5=Intern.
/// GradeDisplay/SeniorityDisplay ile aynı desen — eşleme tek yerde durur.
///
/// WebUI Domain'e referans VEREMEDİĞİ için sayılar burada elle tutulur;
/// enum değeri değişirse bu dosya da güncellenmelidir.
/// </summary>
public static class RoleDisplay
{
    public const int Admin = 1;
    public const int Intern = 5;

    private static readonly (int Value, string Label)[] Roles =
    [
        (Admin, "Sistem Yöneticisi"),
        (2, "İnsan Kaynakları"),
        (3, "Yönetici"),
        (4, "Çalışan"),
        (Intern, "Stajyer"),
    ];

    public static string Label(int role) =>
        Roles.FirstOrDefault(r => r.Value == role).Label ?? role.ToString();

    public static IEnumerable<SelectListItem> Options() =>
        Roles.Select(r => new SelectListItem(r.Label, r.Value.ToString()));
}
