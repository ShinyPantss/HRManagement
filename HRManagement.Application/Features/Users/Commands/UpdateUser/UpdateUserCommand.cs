using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// "Kullanıcıyı güncelle" isteği. IRequest&lt;Unit&gt;: geriye veri dönmez,
/// Unit MediatR'ın "değer yok" karşılığıdır.
/// </summary>
public sealed record UpdateUserCommand(
    int Id,
    string Email,
    Role Role,
    bool IsActive) : IRequest<Unit>;
