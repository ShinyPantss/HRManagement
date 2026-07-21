using HRManagement.Application.dto.Auth;
using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.Login;

/// <summary>
/// "Giriş yap" isteği. IRequest&lt;AuthResultDto&gt;: başarılı girişte token,
/// kullanıcı adı ve rol bilgisini taşıyan sonuç döner.
/// </summary>
public sealed record LoginCommand(string UsernameOrEmail, string Password) : IRequest<AuthResultDto>;
