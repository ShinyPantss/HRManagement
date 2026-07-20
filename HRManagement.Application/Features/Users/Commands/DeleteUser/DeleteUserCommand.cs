using MediatR;

namespace HRManagement.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// "Kullanıcıyı sil" isteği. IRequest&lt;Unit&gt;: geriye veri dönmez.
/// </summary>
public sealed record DeleteUserCommand(int Id) : IRequest<Unit>;
