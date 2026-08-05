using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;

namespace HRManagement.Application.Features.Employees.Shared;

/// <summary>
/// Kaydın İÇİNDEKİ hassas alanların kırpma kuralları.
///
/// EmployeeVisibility "hangi KAYDI görebilir" sorusuna bakar; burası ise
/// "gördüğü kaydın hangi ALANLARINI görebilir" sorusuna. Ayrı bir sınıf olmasının
/// sebebi tek doğruluk kaynağı: aynı kural üç ayrı yoldan geçiyor (detay, liste,
/// tekil sorgu) ve kural tek yerde durmazsa —ki durmuyordu— detayda kırpılan
/// alan listede sızıyor.
/// </summary>
public static class EmployeeFieldVisibility
{
    /// <summary>
    /// T.C. Kimlik yalnızca İK'ya görünür (kullanıcı kararı, 2026-07-23):
    /// Admin dahil başka hiçbir rol göremez.
    /// </summary>
    public static bool CanSeeNationalId(User? requester) => requester?.Role == Role.HR;
}
