using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;

/// <summary>
/// "İzin talebini onayla" isteği. Geriye anlamlı bir değer dönmediği için
/// IRequest&lt;Unit&gt; kullanılır; Unit, MediatR'ın "değer yok" karşılığıdır.
/// </summary>
public sealed class ApproveLeaveRequestCommand : IRequest<Unit>
{
    public ApproveLeaveRequestCommand(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
