using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Queries.GetAllLeaveRequests;

/// <summary>
/// TÜM izin talepleri (her durumda) — "İzin Geçmişi" ekranı için. Şirket geneli
/// hassas veri olduğundan yalnızca HR/Admin çağırır (API'de rol kapısı); handler
/// ayrıca aktörün rolünü doğrular — otorite Application'dır.
/// ActorUserId imzalı JWT claim'inden gelir.
/// </summary>
public sealed record GetAllLeaveRequestsQuery(int ActorUserId) : IRequest<IReadOnlyList<LeaveHistoryDto>>;
