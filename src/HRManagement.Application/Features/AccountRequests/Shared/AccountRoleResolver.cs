using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.AccountRequests.Shared;

/// <summary>
/// Bir kişiye açılacak hesabın rolünü belirler. Artık "önerilen rol" SEÇİMİ yok —
/// rol kişinin KENDİSİNDEN türetilir; tek kural, hem otomatik (kayıt açılınca) hem
/// manuel talep akışında kullanılır.
///   Çalışan: yönetici kademesi (GM/GMY/Müdür — Direktör de Müdür sayılır) → Yönetici;
///            diğerleri (Müdür Yrd. / Kıd. Uzman / Uzman / kıdemsiz) → Çalışan.
///   Stajyer: her zaman Stajyer.
/// İK/Admin gibi kıdemden türetilemeyen roller gerekiyorsa Admin onay ekranında verir.
/// </summary>
public static class AccountRoleResolver
{
    public static Role ForEmployee(SeniorityLevel? seniority) =>
        seniority is SeniorityLevel s && s.IsManagerial() ? Role.Manager : Role.Employee;

    public static Role ForIntern() => Role.Intern;
}
