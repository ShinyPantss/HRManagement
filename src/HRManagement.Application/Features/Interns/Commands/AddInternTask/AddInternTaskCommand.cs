using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.AddInternTask;

/// <summary>
/// Mentorun stajyere görev ataması (§5.4). RequesterUserId imzalı token'dan
/// gelir; görev Pending durumuyla doğar.
/// </summary>
public sealed record AddInternTaskCommand(
    int InternId,
    int RequesterUserId,
    string Title,
    string? Description,
    DateTime? DueDate) : IRequest<int>;
