using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;

/// <summary>
/// "İzin talebini reddet" isteği. Reason opsiyoneldir; verilirse kayda
/// RejectionReason olarak yazılır. IRequest&lt;Unit&gt;: geriye değer dönmez.
/// </summary>
public sealed class RejectLeaveRequestCommand : IRequest<Unit>
{
    public RejectLeaveRequestCommand(int id, string? reason)
    {
        Id = id;
        Reason = reason;
    }

    public int Id { get; }
    public string? Reason { get; }
}
