using System.Security.Claims;
using HRManagement.API.Models;
using HRManagement.API.Models.Auth;
using HRManagement.Application.Features.Users.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Giriş yapar ve JWT döner. Hatalı giriş, LoginCommandHandler'daki
    /// ValidationException üzerinden 400 + BaseResponse.Fail olarak döner.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand(request.UsernameOrEmail, request.Password));

        var data = new LoginResponse(result.Token, result.Username, result.Role);
        return Ok(BaseResponse<LoginResponse>.Success(data, "Giriş başarılı."));
    }

    /// <summary>
    /// Token'ın gerçekten doğrulandığını gösteren uç: gelen Bearer token çözülüp
    /// User.Claims'e yazıldığı için buradaki bilgiler istemciden değil, imzalı
    /// token'dan gelir. WebUI de "oturum kimin?" sorusunu buradan soracak.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var username = User.FindFirstValue(ClaimTypes.Name)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        return Ok(BaseResponse<CurrentUserResponse>.Success(new CurrentUserResponse(id, username, role)));
    }
}
