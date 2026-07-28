using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Dashboard.Queries.GetHrDashboard;

/// <summary>
/// İK/Admin ana sayfa panosu için özet metrikler. Şirket geneli veri olduğundan
/// yalnızca HR/Admin çağırır (API'de rol kapısı); handler ayrıca aktörün rolünü
/// doğrular — otorite Application'dır. ActorUserId imzalı JWT claim'inden gelir.
/// </summary>
public sealed record GetHrDashboardQuery(int ActorUserId) : IRequest<HrDashboardDto>;
