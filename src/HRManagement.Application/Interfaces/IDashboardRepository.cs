using HRManagement.Application.DTOs;

namespace HRManagement.Application.Interfaces;

/// <summary>
/// İK panosunun veri kaynağı. Arkasında tek bir stored procedure çalışır ama
/// bu bir INFRASTRUCTURE detayıdır: Application yalnızca bu sözleşmeyi görür,
/// verinin SP'den mi düz SQL'den mi geldiğini bilmez.
///
/// Panonun tamamı TEK çağrıda gelir. Bölmek round-trip'i artırmanın yanı sıra
/// tutarsız anlık görüntü riski doğururdu: KPI "5 kişi izinde" derken listede
/// 6 satır çıkması gibi.
/// </summary>
public interface IDashboardRepository
{
    Task<HrDashboardDto> GetHrDashboardAsync(HrDashboardParameters parameters);
}

/// <summary>
/// Panonun hesaplama girdileri. Hepsi Application'dan gelir çünkü hem sorguyu
/// hem ekrandaki metinleri ("5 günden uzun", "14 gün içinde") aynı değerler
/// besliyor — tek doğruluk kaynağı burada durmalı.
///
/// Enum karşılıkları (LeaveStatus, Gender) bu kayıtta YOK: onları repository
/// kendi içinde Domain enum'ından çevirip SP'ye geçirir. Arayüze koymak
/// imzayı kirletir ve çağıranı ilgilendirmeyen bir ayrıntıyı sızdırırdı.
/// </summary>
/// <param name="Today">Uygulamanın "bugün" tanımı (UTC tarih) — SP kendi saatine bakmaz.</param>
/// <param name="OverdueDays">Bir talep bu kadar gündür bekliyorsa gecikmiş sayılır.</param>
/// <param name="UpcomingWindowDays">Yaklaşan izinler penceresi.</param>
/// <param name="InternEndingWindowDays">Stajı bu kadar gün içinde bitenler uyarı üretir.</param>
/// <param name="TrendMonths">Trend grafiğinin kapsadığı ay sayısı.</param>
public sealed record HrDashboardParameters(
    DateTime Today,
    int OverdueDays,
    int UpcomingWindowDays,
    int InternEndingWindowDays,
    int TrendMonths);
