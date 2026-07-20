using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;

/// <summary>
/// "İzin talebini reddet" isteği. Reason opsiyoneldir; verilirse kayda
/// RejectionReason olarak yazılır. IRequest&lt;Unit&gt;: geriye değer dönmez.
/// </summary>
public sealed record RejectLeaveRequestCommand(int Id, string? Reason) : IRequest<Unit>;
