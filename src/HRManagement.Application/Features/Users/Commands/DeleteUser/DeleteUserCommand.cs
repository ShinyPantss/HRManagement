using MediatR;

namespace HRManagement.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// "Kullanıcıyı sil" isteği. IRequest&lt;Unit&gt;: geriye veri dönmez.
/// </summary>
public sealed class DeleteUserCommand : IRequest<Unit>
{
    public DeleteUserCommand(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
