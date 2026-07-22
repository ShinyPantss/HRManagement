using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;

/// <summary>
/// "İzin talebini reddet" isteği. Reddetme HER İKİ aşamada da mümkündür;
/// yetki kuralı o aşamada onaylayabilecek kişiyle aynıdır (LeaveApprovalGuard).
/// Reason opsiyoneldir; verilirse kayda RejectionReason olarak yazılır.
/// </summary>
public sealed record RejectLeaveRequestCommand(int Id, int ApproverUserId, string? Reason) : IRequest<Unit>;
