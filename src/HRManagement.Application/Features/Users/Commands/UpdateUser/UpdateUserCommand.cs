using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// "Kullanıcıyı güncelle" isteği. IRequest&lt;Unit&gt;: geriye veri dönmez,
/// Unit MediatR'ın "değer yok" karşılığıdır.
///
/// CurrentUserId isteği YAPAN Admin'dir; istemciden değil, imzalı token'dan gelir.
/// Kişinin kendi rolünü/erişimini değiştirmesini engelleyen kural buna dayanır.
/// </summary>
public sealed record UpdateUserCommand(
    int Id,
    string Email,
    Role Role,
    bool IsActive,
    int CurrentUserId) : IRequest<Unit>;
