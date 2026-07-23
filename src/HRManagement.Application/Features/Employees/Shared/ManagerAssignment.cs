using System.ComponentModel.DataAnnotations;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.Employees.Shared;

/// <summary>
/// Yönetici atama kıdem kuralı — Create ve Update handler'larının ortak kalbi.
///
/// Yönetici, çalışandan kıdemce YÜKSEK olmalıdır (SeniorityLevel'da sayı küçüldükçe
/// kıdem yükselir, 1=GM). Bir Uzman (6), bir Müdür'e (3) yönetici olamaz.
///
/// Karşılaştırma yalnızca İKİSİNİN de kıdemi belli olduğunda yapılır; biri null
/// ise (kıdem henüz girilmemiş) engellenmez — kıyaslanamayan durumu yanlışlıkla
/// reddetmemek için.
/// </summary>
public static class ManagerAssignment
{
    public static void EnsureManagerOutranks(SeniorityLevel? managerSeniority, SeniorityLevel? employeeSeniority)
    {
        if (managerSeniority is SeniorityLevel m && employeeSeniority is SeniorityLevel e
            && (int)m >= (int)e)
        {
            throw new ValidationException(
                "Yönetici, çalışandan daha kıdemli olmalıdır. Seçilen kişi eşit veya daha düşük kıdemde.");
        }
    }
}
