using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// "Kullanıcıyı güncelle" isteği. IRequest&lt;Unit&gt;: geriye veri dönmez,
/// Unit MediatR'ın "değer yok" karşılığıdır.
/// </summary>
public sealed class UpdateUserCommand : IRequest<Unit>
{
    public UpdateUserCommand(int id, string email, Role role, bool isActive)
    {
        Id = id;
        Email = email;
        Role = role;
        IsActive = isActive;
    }

    public int Id { get; }
    public string Email { get; }
    public Role Role { get; }
    public bool IsActive { get; }
}
