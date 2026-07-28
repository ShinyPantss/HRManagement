using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.UpdateMyInternTaskStatus;

/// <summary>
/// Stajyerin KENDİ görevinin durumunu ilerletmesi (Başlat/Tamamla).
/// Mentorun UpdateInternTaskStatus'undan ayrı bir use-case: yetki kuralı
/// farklıdır (mentor ilişkisi değil, görev SAHİPLİĞİ denetlenir).
/// </summary>
public sealed record UpdateMyInternTaskStatusCommand(int TaskId, int RequesterUserId, int NewStatus)
    : IRequest<Unit>;
