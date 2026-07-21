using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// "Yeni kullanıcı ekle" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner.
/// </summary>
public sealed class CreateUserCommand : IRequest<int>
{
    public CreateUserCommand(string username, string email, string password, Role role)
    {
        Username = username;
        Email = email;
        Password = password;
        Role = role;
    }

    public string Username { get; }
    public string Email { get; }
    public string Password { get; }
    public Role Role { get; }
}
