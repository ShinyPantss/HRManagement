using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.DeleteLeaveRequest;

/// <summary>
/// İzin talebini iptal/sil. RequesterUserId imzalı JWT claim'inden gelir.
/// Yetki: talep sahibi YALNIZCA henüz onaylanmamış (Pending) kendi talebini
/// silebilir; onaylı/İK aşamasındaki talepleri yalnızca Admin silebilir —
/// aksi hâlde biri onaylı iznini silip bakiyesini geri kazanabilir.
/// </summary>
public sealed record DeleteLeaveRequestCommand(int Id, int RequesterUserId) : IRequest<Unit>;
