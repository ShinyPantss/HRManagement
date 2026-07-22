using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;

/// <summary>
/// "İzin talebini onayla" isteği. Talep hangi aşamadaysa (yönetici / İK)
/// o aşamanın onayı işlenir — hangi aşama olduğuna handler karar verir.
///
/// ApproverUserId imzalı JWT claim'inden gelir, istek gövdesinden değil:
/// gövde istemcinin elindedir, kimlik ancak imzalı token'dan okunur.
/// </summary>
public sealed record ApproveLeaveRequestCommand(int Id, int ApproverUserId) : IRequest<Unit>;
