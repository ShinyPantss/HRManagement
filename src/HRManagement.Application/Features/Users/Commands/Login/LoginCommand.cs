using HRManagement.Application.dto.Auth;
using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.Login;

/// <summary>
/// "Giriş yap" isteği. IRequest&lt;AuthResultDto&gt;: başarılı girişte token,
/// kullanıcı adı ve rol bilgisini taşıyan sonuç döner.
/// </summary>
public sealed class LoginCommand : IRequest<AuthResultDto>
{
    public LoginCommand(string usernameOrEmail, string password)
    {
        UsernameOrEmail = usernameOrEmail;
        Password = password;
    }

    public string UsernameOrEmail { get; }
    public string Password { get; }
}
