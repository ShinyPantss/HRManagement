using System.ComponentModel.DataAnnotations;
using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Dashboard.Queries.GetHrDashboard;

/// <summary>
/// İK panosu. Toplama/gruplama işi stored procedure'e taşındıktan sonra bu
/// handler'a iki sorumluluk kaldı: KİM sorabilir (yetki) ve HANGİ eşiklerle
/// (parametreler). Query tarafının incelmesi CQRS'in beklediği asimetridir —
/// karar kuralları command tarafında, raporlama sorguları veri katmanında.
/// </summary>
public sealed class GetHrDashboardQueryHandler
    : IRequestHandler<GetHrDashboardQuery, HrDashboardDto>
{
    // ── Pano eşikleri ────────────────────────────────────────────────────────
    // Hem SP'ye parametre olarak gider hem ekrandaki metinleri yazar
    // (DashboardController bunları yanıta koyar). Tek yerde durmaları bilinçli:
    // ekranda "5 günden uzun" yazarken SP'nin 7'ye göre süzmesi, panonun kendi
    // kendine yalan söylemesi olurdu.

    /// <summary>Bir talep bu kadar gündür bekliyorsa "gecikmiş" sayılır.</summary>
    public const int OverdueDays = 5;

    /// <summary>Stajı bu kadar gün içinde bitenler "yakında bitiyor" sayılır.</summary>
    public const int InternEndingWindowDays = 30;

    /// <summary>Yaklaşan izinler penceresi.</summary>
    public const int UpcomingWindowDays = 14;

    /// <summary>Trend grafiğinin kapsadığı ay sayısı.</summary>
    private const int TrendMonths = 6;

    private readonly IUserRepository _userRepository;
    private readonly IDashboardRepository _dashboardRepository;

    public GetHrDashboardQueryHandler(
        IUserRepository userRepository,
        IDashboardRepository dashboardRepository)
    {
        _userRepository = userRepository;
        _dashboardRepository = dashboardRepository;
    }

    public async Task<HrDashboardDto> Handle(GetHrDashboardQuery request, CancellationToken cancellationToken)
    {
        var actor = await _userRepository.GetByIdAsync(request.ActorUserId);

        if (actor is null || !actor.IsActive)
            throw new ValidationException("İşlemi yapan hesap bulunamadı veya pasif.");

        if (actor.Role is not (Role.HR or Role.Admin))
            throw new ValidationException("Bu panoyu görüntüleme yetkiniz yok.");

        return await _dashboardRepository.GetHrDashboardAsync(new HrDashboardParameters(
            Today: DateTime.UtcNow.Date,
            OverdueDays: OverdueDays,
            UpcomingWindowDays: UpcomingWindowDays,
            InternEndingWindowDays: InternEndingWindowDays,
            TrendMonths: TrendMonths));
    }
}
