using System.ComponentModel.DataAnnotations;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.Employees.Shared;

/// <summary>
/// Yönetici atama kuralı — Create ve Update handler'larının ortak kalbi.
/// Kapı SENIORITY'ye bakar (şirket org kademesi), Role'e DEĞİL: yönetici olabilmek
/// hesap yetkisi değil, org pozisyonudur. (Hesabı olmayan bir Müdür de yönetici olabilir.)
///
/// İki koşul birden:
///   1) Yönetici, YÖNETİCİ KADEMESİNDE olmalı (GM, GMY, Müdür). Müdür Yrd., Kıdemli
///      Uzman ve Uzman kıdemce yüksek olsa bile kimseye yönetici olamaz.
///   2) Yönetici, çalışandan kıdemce KESİN yüksek olmalı (eşit de olmaz;
///      Müdür, Müdür'e yönetici olamaz).
/// </summary>
public static class ManagerAssignment
{
    public static void EnsureManagerEligible(SeniorityLevel? managerSeniority, SeniorityLevel? employeeSeniority)
    {
        // 1) Yönetici kademesi zorunlu. Kıdemi bilinmiyorsa (null) doğrulanamaz → reddet.
        if (managerSeniority is not SeniorityLevel manager || !manager.IsManagerial())
            throw new ValidationException(
                "Yönetici yalnızca yönetici kademesinden (Müdür, GMY, GM) seçilebilir.");

        // 2) Kıdem karşılaştırması yalnızca çalışanın kıdemi de belliyse yapılır.
        if (employeeSeniority is SeniorityLevel employee && (int)manager >= (int)employee)
            throw new ValidationException(
                "Yönetici, çalışandan daha kıdemli olmalıdır. Seçilen kişi eşit veya daha düşük kıdemde.");
    }
}
