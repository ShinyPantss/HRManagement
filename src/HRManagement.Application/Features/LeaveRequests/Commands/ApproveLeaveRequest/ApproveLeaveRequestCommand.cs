using MediatR;

namespace HRManagement.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;

/// <summary>
/// "İzin talebini onayla" isteği. Geriye anlamlı bir değer dönmediği için
/// IRequest&lt;Unit&gt; kullanılır; Unit, MediatR'ın "değer yok" karşılığıdır.
/// </summary>
public sealed record ApproveLeaveRequestCommand(int Id) : IRequest<Unit>;
