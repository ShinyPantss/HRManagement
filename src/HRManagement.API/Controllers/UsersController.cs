using HRManagement.API.Models;
using HRManagement.API.Models.Users;
using HRManagement.Application.Features.Users.Commands.CreateUserForPerson;
using HRManagement.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Var olan bir çalışana/stajyere giriş hesabı açar. Rol atama gücü yalnızca
    /// Admin'de: HR'a verilseydi HR kendini Admin yapabilirdi (privilege escalation).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("for-person")]
    public async Task<IActionResult> CreateForPerson(CreateUserForPersonRequest request)
    {
        var id = await _mediator.Send(new CreateUserForPersonCommand(
            request.Username, request.Email, request.Password, (Role)request.Role,
            request.EmployeeId, request.InternId));

        return Ok(BaseResponse<int>.Success(id, "Giriş hesabı oluşturuldu."));
    }
}
