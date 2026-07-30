using System.Security.Claims;
using HRManagement.API.Models;
using HRManagement.API.Models.Users;
using HRManagement.Application.Features.Users.Commands.CreateUserForPerson;
using HRManagement.Application.Features.Users.Commands.UpdateUser;
using HRManagement.Application.Features.Users.Queries.GetAllUsers;
using HRManagement.Application.Features.Users.Queries.GetUserById;
using HRManagement.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

/// <summary>
/// Hesap yönetimi — baştan sona Admin'e kilitli. Rol atama gücünün tek durduğu
/// yer burasıdır: HR'a açılsaydı HR kendini Admin yapabilirdi.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _mediator.Send(new GetAllUsersQuery());
        var data = users.Select(ToResponse).ToList();
        return Ok(BaseResponse<List<UserResponse>>.Success(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _mediator.Send(new GetUserByIdQuery(id));

        if (user is null)
            return NotFound(BaseResponse<UserResponse>.Fail("Kullanıcı bulunamadı."));

        return Ok(BaseResponse<UserResponse>.Success(ToResponse(user)));
    }

    /// <summary>
    /// E-posta, rol ve aktiflik güncellenir. "Kendi rolünü değiştirememe" ve
    /// "son yöneticiyi koruma" kuralları handler'da; işlemi yapan kişi gövdeden
    /// DEĞİL token'dan okunur, aksi hâlde kural istemciden atlatılabilirdi.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request)
    {
        await _mediator.Send(new UpdateUserCommand(
            id, request.Email, (Role)request.Role, request.IsActive, CurrentUserId()));

        return Ok(BaseResponse<int>.Success(id, "Hesap güncellendi."));
    }

    /// <summary>
    /// Var olan bir çalışana/stajyere giriş hesabı açar.
    /// </summary>
    [HttpPost("for-person")]
    public async Task<IActionResult> CreateForPerson(CreateUserForPersonRequest request)
    {
        var id = await _mediator.Send(new CreateUserForPersonCommand(
            request.Username, request.Email, request.Password, (Role)request.Role,
            request.EmployeeId, request.InternId));

        return Ok(BaseResponse<int>.Success(id, "Giriş hesabı oluşturuldu."));
    }

    /// <summary>Kimlik daima imzalı token'dan okunur, istek gövdesinden asla.</summary>
    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static UserResponse ToResponse(HRManagement.Application.DTOs.UserDto u) => new(
        u.Id, u.Username, u.Email, (int)u.Role, u.IsActive);
}
