using System.ComponentModel.DataAnnotations;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.Employees.Shared;

/// <summary>
/// Yönetici atama kuralı — Create ve Update handler'larının ortak kalbi.
/// Kapı SENIORITY'ye bakar (şirket org kademesi), Role'e DEĞİL: yönetici olabilmek
/// hesap yetkisi değil, org pozisyonudur. (Hesabı olmayan bir Müdür de yönetici olabilir.)
///
/// Üç koşul birden:
///   1) Yönetici, YÖNETİCİ KADEMESİNDE olmalı (GM, GMY, Müdür). Müdür Yrd., Kıdemli
///      Uzman ve Uzman kıdemce yüksek olsa bile kimseye yönetici olamaz.
///   2) Yönetici, çalışandan kıdemce KESİN yüksek olmalı (eşit de olmaz;
///      Müdür, Müdür'e yönetici olamaz).
///   3) Yönetici, çalışanla AYNI DEPARTMANDA olmalı — TEK istisna Genel Müdür.
///      Kıdem tek başına yetmez: alakasız departmanların yöneticileri kıdemce
///      uysa bile birbirine bağlanmamalı (ör. bir alanın GMY'si başka alanın
///      müdürüne yönetici olmamalı). GM "departman üstü"dür (SeniorityLevel yorumu),
///      şirket genelini yönettiği için departman eşleşmesi aranmaz; GMY dahil diğer
///      tüm kademeler bir departmana bağlıdır ve o departmanla sınırlıdır.
/// </summary>
public static class ManagerAssignment
{
    public static void EnsureManagerEligible(
        SeniorityLevel? managerSeniority, int managerDepartmentId,
        SeniorityLevel? employeeSeniority, int employeeDepartmentId)
    {
        // 1) Yönetici kademesi zorunlu. Kıdemi bilinmiyorsa (null) doğrulanamaz → reddet.
        if (managerSeniority is not SeniorityLevel manager || !manager.IsManagerial())
            throw new ValidationException(
                "Yönetici yalnızca yönetici kademesinden (Müdür, GMY, GM) seçilebilir.");

        // 2) Kıdem karşılaştırması yalnızca çalışanın kıdemi de belliyse yapılır.
        if (employeeSeniority is SeniorityLevel employee && (int)manager >= (int)employee)
            throw new ValidationException(
                "Yönetici, çalışandan daha kıdemli olmalıdır. Seçilen kişi eşit veya daha düşük kıdemde.");

        // 3) Departman eşleşmesi — GM hariç. GM departman üstü olduğu için serbest;
        //    GMY ve Müdür ise kendi departmanlarıyla sınırlıdır.
        if (manager != SeniorityLevel.GenelMudur && managerDepartmentId != employeeDepartmentId)
            throw new ValidationException(
                "Yönetici, çalışanla aynı departmanda olmalıdır. " +
                "Farklı departmandan yalnızca Genel Müdür yönetici atanabilir.");
    }
}
