using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.UpdateInternTaskStatus;

/// <summary>
/// Görev durumunu ilerletir (Pending → InProgress → Done). NewStatus,
/// InternTaskStatus'un sayısal karşılığıdır; aralık kontrolü Validator'da.
/// </summary>
public sealed record UpdateInternTaskStatusCommand(int TaskId, int RequesterUserId, int NewStatus)
    : IRequest<Unit>;
