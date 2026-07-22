using MediatR;

namespace HRManagement.Application.Features.AccountRequests.Commands.RejectAccountRequest;

/// <summary>
/// Bekleyen hesap talebini reddeder. Yalnızca Admin. ApproverUserId claim'den gelir.
/// Reason opsiyonel (neden reddedildiği kayda geçer).
/// </summary>
public sealed record RejectAccountRequestCommand(int Id, int ApproverUserId, string? Reason) : IRequest<Unit>;
