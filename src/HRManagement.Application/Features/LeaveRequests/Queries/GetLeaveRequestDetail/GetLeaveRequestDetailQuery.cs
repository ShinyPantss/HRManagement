using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Queries.GetLeaveRequestDetail;

/// <summary>
/// Tek bir izin talebinin detayı. Görüntüleme yetkisi handler'da denetlenir:
/// HR/Admin herkesi görür; aksi hâlde yalnızca talep sahibi ya da işleme yetkili
/// (yönetici zinciri/mentor) görebilir. ActorUserId imzalı JWT claim'inden gelir.
/// </summary>
public sealed record GetLeaveRequestDetailQuery(int Id, int ActorUserId) : IRequest<LeaveDetailDto>;
