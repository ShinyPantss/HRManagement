using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Queries.GetPendingApprovals;

/// <summary>
/// Giriş yapan kişinin ONAYINI BEKLEYEN izin talepleri. Tek tek çalışan seçmeye
/// gerek yok: kişi yetkili olduğu talepleri tek listede görür.
/// ActorUserId imzalı JWT claim'inden gelir.
/// </summary>
public sealed record GetPendingApprovalsQuery(int ActorUserId) : IRequest<IReadOnlyList<PendingApprovalDto>>;
