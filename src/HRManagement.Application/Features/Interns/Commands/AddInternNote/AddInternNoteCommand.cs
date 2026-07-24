using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.AddInternNote;

/// <summary>Mentor notu (§5.4 — haftalık kısa geri bildirim gibi).</summary>
public sealed record AddInternNoteCommand(int InternId, int RequesterUserId, string Content)
    : IRequest<int>;
